
using System.Data;
using CLAMS.API.DTOs.PositionAssignments;
using CLAMS.Domain.Entities;
using CLAMS.Domain.Enums;
using CLAMS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CLAMS.API.Controllers;

[Route("api/position-assignments")]
[ApiController]
public class PositionAssignmentsController : ControllerBase
{
    private readonly CLAMSHRDbContext _context;

    public PositionAssignmentsController(CLAMSHRDbContext context)
    {
        _context = context;
    }

    // GET: api/position-assignments
    [Authorize(Roles = "SystemAdministrator,HRManager,HROfficer,Management,ReadOnly")]
    [HttpGet]
    public async Task<IActionResult> GetAssignments(
        int? employeeId,
        int? positionId,
        int? departmentId,
        bool? currentOnly,
        int page = 1,
        int pageSize = 10)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new
            {
                message = "Page must be at least 1 and pageSize must be between 1 and 100."
            });
        }

        var query = _context.EmployeePositionHistories
            .Include(h => h.Employee)
            .Include(h => h.Position)
                .ThenInclude(p => p.Department)
            .AsQueryable();

        if (employeeId.HasValue)
            query = query.Where(h => h.EmployeeId == employeeId.Value);

        if (positionId.HasValue)
            query = query.Where(h => h.PositionId == positionId.Value);

        if (departmentId.HasValue)
        {
            query = query.Where(h =>
                h.Position.DepartmentId == departmentId.Value);
        }

        if (currentOnly == true)
        {
            query = query.Where(h =>
                h.Status == PositionAssignmentStatus.Current);
        }

        var totalCount = await query.CountAsync();

        var assignmentEntities = await query
            .OrderByDescending(h => h.StartDate)
            .ThenByDescending(h => h.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var assignments = assignmentEntities
            .Select(ToResponse)
            .ToList();

        return Ok(new
        {
            page,
            pageSize,
            totalCount,
            totalPages = (int)Math.Ceiling(
                totalCount / (double)pageSize),
            assignments
        });
    }

    // GET: api/position-assignments/1
    [Authorize(Roles = "SystemAdministrator,HRManager,HROfficer,Management,ReadOnly")]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAssignment(int id)
    {
        var assignment = await _context.EmployeePositionHistories
            .Include(h => h.Employee)
            .Include(h => h.Position)
                .ThenInclude(p => p.Department)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (assignment == null)
        {
            return NotFound(new
            {
                message = "Position assignment not found."
            });
        }

        return Ok(ToResponse(assignment));
    }

    // GET: api/position-assignments/employee/1
    [Authorize(Roles = "SystemAdministrator,HRManager,HROfficer,Management,ReadOnly")]
    [HttpGet("employee/{employeeId:int}")]
    public async Task<IActionResult> GetEmployeeAssignments(
        int employeeId)
    {
        var employeeExists = await _context.Employees
            .AnyAsync(e => e.Id == employeeId);

        if (!employeeExists)
        {
            return NotFound(new
            {
                message = "Employee not found."
            });
        }

        var assignmentEntities =
            await _context.EmployeePositionHistories
                .Include(h => h.Employee)
                .Include(h => h.Position)
                    .ThenInclude(p => p.Department)
                .Where(h => h.EmployeeId == employeeId)
                .OrderByDescending(h => h.StartDate)
                .ThenByDescending(h => h.Id)
                .ToListAsync();

        return Ok(assignmentEntities.Select(ToResponse).ToList());
    }

    // POST: api/position-assignments
    [Authorize(Roles = "SystemAdministrator,HRManager,HROfficer")]
    [HttpPost]
    public async Task<IActionResult> CreateAssignment(
        [FromBody] CreatePositionAssignmentRequest request)
    {
        if (request.EmployeeId <= 0 || request.PositionId <= 0)
        {
            return BadRequest(new
            {
                message = "Valid employee and position IDs are required."
            });
        }

        if (request.StartDate == default)
        {
            return BadRequest(new
            {
                message = "A valid assignment start date is required."
            });
        }

        var today = GetBusinessDate();

        var assignmentStatus = request.StartDate > today
            ? PositionAssignmentStatus.Scheduled
            : PositionAssignmentStatus.Current;

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted);

        try
        {
            if (!await LockEmployeeAsync(request.EmployeeId))
            {
                return NotFound(new
                {
                    message = "Employee not found."
                });
            }

            var employee = await _context.Employees
                .FirstAsync(e => e.Id == request.EmployeeId);

            if (employee.EmploymentStatus != EmploymentStatus.Active)
            {
                return BadRequest(new
                {
                    message = "Only active employees can be assigned to positions."
                });
            }

            if (!await LockPositionAsync(request.PositionId))
            {
                return NotFound(new
                {
                    message = "Position not found."
                });
            }

            var position = await _context.Positions
                .Include(p => p.Department)
                .FirstAsync(p => p.Id == request.PositionId);

            if (!position.IsActive || !position.Department.IsActive)
            {
                return BadRequest(new
                {
                    message = "The position and its department must be active."
                });
            }

            if (employee.DepartmentId != position.DepartmentId)
            {
                return BadRequest(new
                {
                    message = "The employee and position must belong to the same department. Use the transfer workflow to move an employee between departments."
                });
            }

            var existingAssignment =
                await _context.EmployeePositionHistories
                    .AnyAsync(h =>
                        h.EmployeeId == employee.Id &&
                        (h.Status == PositionAssignmentStatus.Current ||
                         h.Status == PositionAssignmentStatus.Scheduled));

            if (existingAssignment)
            {
                return Conflict(new
                {
                    message = "This employee already has a current or scheduled position assignment. Use the transfer workflow if the employee already has a position."
                });
            }

            var hasHistoricalOverlap =
                await HasOverlappingAssignmentAsync(
                    employee.Id,
                    request.StartDate,
                    null);

            if (hasHistoricalOverlap)
            {
                return Conflict(new
                {
                    message = "The proposed assignment overlaps an existing or historical assignment. Check the employee's assignment history."
                });
            }

            var reservedHeadcount =
                await _context.EmployeePositionHistories
                    .CountAsync(h =>
                        h.PositionId == position.Id &&
                        (h.Status == PositionAssignmentStatus.Current ||
                         h.Status == PositionAssignmentStatus.Scheduled));

            if (reservedHeadcount >= position.ApprovedHeadcount)
            {
                return Conflict(new
                {
                    message = "The position has reached its approved headcount, including scheduled assignments."
                });
            }

            var assignment = new EmployeePositionHistory
            {
                EmployeeId = employee.Id,
                PositionId = position.Id,
                StartDate = request.StartDate,
                Status = assignmentStatus,
                ChangeReason = request.ChangeReason?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.EmployeePositionHistories.Add(assignment);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var createdAssignment =
                await _context.EmployeePositionHistories
                    .Include(h => h.Employee)
                    .Include(h => h.Position)
                        .ThenInclude(p => p.Department)
                    .FirstAsync(h => h.Id == assignment.Id);

            return CreatedAtAction(
                nameof(GetAssignment),
                new { id = assignment.Id },
                ToResponse(createdAssignment));
        }
        catch (Exception ex) when (
            IsUniqueViolation(ex) || IsExclusionViolation(ex))
        {
            await transaction.RollbackAsync();

            return Conflict(new
            {
                message = "The assignment conflicts with an existing employee assignment or date range."
            });
        }
        catch (Exception ex) when (IsConcurrencyFailure(ex))
        {
            await transaction.RollbackAsync();

            return Conflict(new
            {
                message = "A concurrent assignment operation occurred. Please retry the request."
            });
        }
    }

    // POST: api/position-assignments/1/transfer
    [Authorize(Roles = "SystemAdministrator,HRManager,HROfficer")]
    [HttpPost("{id:int}/transfer")]
    public async Task<IActionResult> TransferAssignment(
        int id,
        [FromBody] TransferPositionAssignmentRequest request)
    {
        if (request.NewPositionId <= 0)
        {
            return BadRequest(new
            {
                message = "A valid destination position is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.ChangeReason))
        {
            return BadRequest(new
            {
                message = "A transfer reason is required."
            });
        }

        if (request.StartDate == default)
        {
            return BadRequest(new
            {
                message = "A valid transfer start date is required."
            });
        }

        var today = GetBusinessDate();
        var isScheduledTransfer = request.StartDate > today;

        var employeeId = await _context.EmployeePositionHistories
            .Where(h =>
                h.Id == id &&
                h.Status == PositionAssignmentStatus.Current)
            .Select(h => (int?)h.EmployeeId)
            .FirstOrDefaultAsync();

        if (!employeeId.HasValue)
        {
            return NotFound(new
            {
                message = "Current position assignment not found."
            });
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted);

        try
        {
            if (!await LockEmployeeAsync(employeeId.Value))
            {
                return NotFound(new
                {
                    message = "Employee not found."
                });
            }

            var currentAssignment =
                await _context.EmployeePositionHistories
                    .Include(h => h.Employee)
                    .Include(h => h.Position)
                    .FirstOrDefaultAsync(h =>
                        h.Id == id &&
                        h.Status == PositionAssignmentStatus.Current);

            if (currentAssignment == null)
            {
                return Conflict(new
                {
                    message = "The assignment is no longer current. Refresh and try again."
                });
            }

            if (request.StartDate <= currentAssignment.StartDate)
            {
                return BadRequest(new
                {
                    message = "The transfer date must be after the current assignment start date."
                });
            }

            if (currentAssignment.Employee.EmploymentStatus
                != EmploymentStatus.Active)
            {
                return BadRequest(new
                {
                    message = "Only active employees can be transferred."
                });
            }

            if (request.NewPositionId == currentAssignment.PositionId)
            {
                return BadRequest(new
                {
                    message = "The employee is already assigned to this position."
                });
            }

            var existingScheduledAssignment =
                await _context.EmployeePositionHistories
                    .AnyAsync(h =>
                        h.EmployeeId == employeeId.Value &&
                        h.Status == PositionAssignmentStatus.Scheduled);

            if (existingScheduledAssignment)
            {
                return Conflict(new
                {
                    message = "This employee already has a scheduled assignment. Complete or resolve that assignment before scheduling another transfer."
                });
            }

            var hasHistoricalOverlap =
                await HasOverlappingAssignmentAsync(
                    employeeId.Value,
                    request.StartDate,
                    null,
                    currentAssignment.Id);

            if (hasHistoricalOverlap)
            {
                return Conflict(new
                {
                    message = "The proposed transfer overlaps another assignment in the employee's history."
                });
            }

            if (!await LockPositionAsync(request.NewPositionId))
            {
                return NotFound(new
                {
                    message = "Destination position not found."
                });
            }

            var newPosition = await _context.Positions
                .Include(p => p.Department)
                .FirstAsync(p => p.Id == request.NewPositionId);

            if (!newPosition.IsActive || !newPosition.Department.IsActive)
            {
                return BadRequest(new
                {
                    message = "The destination position and its department must be active."
                });
            }

            var reservedHeadcount =
                await _context.EmployeePositionHistories
                    .CountAsync(h =>
                        h.PositionId == newPosition.Id &&
                        (h.Status == PositionAssignmentStatus.Current ||
                         h.Status == PositionAssignmentStatus.Scheduled));

            if (reservedHeadcount >= newPosition.ApprovedHeadcount)
            {
                return Conflict(new
                {
                    message = "The destination position has reached its approved headcount, including scheduled assignments."
                });
            }

            var newAssignment = new EmployeePositionHistory
            {
                EmployeeId = currentAssignment.EmployeeId,
                PositionId = newPosition.Id,
                StartDate = request.StartDate,
                Status = isScheduledTransfer
                    ? PositionAssignmentStatus.Scheduled
                    : PositionAssignmentStatus.Current,
                ChangeReason = request.ChangeReason.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            // Record the previous assignment's planned end date.
            // This prevents date-range overlap with the new assignment.
            currentAssignment.EndDate =
                request.StartDate.AddDays(-1);

            if (!isScheduledTransfer)
            {
                // Immediate transfer: complete the previous assignment.
                currentAssignment.Status =
                    PositionAssignmentStatus.Completed;
            }

            // Save the previous assignment before inserting the new one.
            await _context.SaveChangesAsync();

            if (!isScheduledTransfer)
            {
                currentAssignment.Employee.DepartmentId =
                    newPosition.DepartmentId;

                currentAssignment.Employee.UpdatedAt =
                    DateTime.UtcNow;
            }

            _context.EmployeePositionHistories.Add(newAssignment);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var transferredAssignment =
                await _context.EmployeePositionHistories
                    .Include(h => h.Employee)
                    .Include(h => h.Position)
                        .ThenInclude(p => p.Department)
                    .FirstAsync(h => h.Id == newAssignment.Id);

            return Ok(new
            {
                message = isScheduledTransfer
                    ? "Future transfer scheduled successfully. The employee's current assignment remains in effect until the scheduled transfer date."
                    : "Employee transferred successfully.",
                previousAssignmentId = currentAssignment.Id,
                newAssignment = ToResponse(transferredAssignment)
            });
        }
        catch (Exception ex) when (
            IsUniqueViolation(ex) || IsExclusionViolation(ex))
        {
            await transaction.RollbackAsync();

            return Conflict(new
            {
                message = "The transfer conflicts with an existing employee assignment or date range."
            });
        }
        catch (Exception ex) when (IsConcurrencyFailure(ex))
        {
            await transaction.RollbackAsync();

            return Conflict(new
            {
                message = "A concurrent transfer or assignment operation occurred. Please retry the request."
            });
        }
    }

    // PATCH: api/position-assignments/1/end
    [Authorize(Roles = "SystemAdministrator,HRManager,HROfficer")]
    [HttpPatch("{id:int}/end")]
    public async Task<IActionResult> EndAssignment(
        int id,
        [FromBody] EndPositionAssignmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(new
            {
                message = "An assignment ending reason is required."
            });
        }

        var today = GetBusinessDate();

        if (request.EndDate == default || request.EndDate > today)
        {
            return BadRequest(new
            {
                message = "The assignment end date must be valid and cannot be in the future."
            });
        }

        var employeeId = await _context.EmployeePositionHistories
            .Where(h =>
                h.Id == id &&
                h.Status == PositionAssignmentStatus.Current)
            .Select(h => (int?)h.EmployeeId)
            .FirstOrDefaultAsync();

        if (!employeeId.HasValue)
        {
            return NotFound(new
            {
                message = "Current position assignment not found."
            });
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted);

        try
        {
            if (!await LockEmployeeAsync(employeeId.Value))
            {
                return NotFound(new
                {
                    message = "Employee not found."
                });
            }

            var assignment = await _context.EmployeePositionHistories
                .FirstOrDefaultAsync(h =>
                    h.Id == id &&
                    h.Status == PositionAssignmentStatus.Current);

            if (assignment == null)
            {
                return Conflict(new
                {
                    message = "The assignment is no longer current. Refresh and try again."
                });
            }

            if (request.EndDate < assignment.StartDate)
            {
                return BadRequest(new
                {
                    message = "The end date cannot be earlier than the assignment start date."
                });
            }

            var hasHistoricalOverlap =
                await HasOverlappingAssignmentAsync(
                    employeeId.Value,
                    assignment.StartDate,
                    request.EndDate,
                    assignment.Id);

            if (hasHistoricalOverlap)
            {
                return Conflict(new
                {
                    message = "The requested end date would overlap another assignment in the employee's history."
                });
            }

            var scheduledAssignment =
                await _context.EmployeePositionHistories
                    .Where(h =>
                        h.EmployeeId == employeeId.Value &&
                        h.Status == PositionAssignmentStatus.Scheduled)
                    .OrderBy(h => h.StartDate)
                    .FirstOrDefaultAsync();

            if (scheduledAssignment != null &&
                request.EndDate >= scheduledAssignment.StartDate)
            {
                return Conflict(new
                {
                    message = "The end date must be before the employee's scheduled assignment start date."
                });
            }

            assignment.EndDate = request.EndDate;
            assignment.Status = PositionAssignmentStatus.Completed;

            assignment.ChangeReason = string.IsNullOrWhiteSpace(
                assignment.ChangeReason)
                ? request.Reason.Trim()
                : $"{assignment.ChangeReason}; Ended: {request.Reason.Trim()}";

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                message = "Position assignment ended successfully.",
                assignmentId = assignment.Id,
                employeeId = assignment.EmployeeId,
                positionId = assignment.PositionId,
                endDate = assignment.EndDate,
                status = assignment.Status.ToString(),
                isCurrent = false
            });
        }
        catch (Exception ex) when (
            IsExclusionViolation(ex))
        {
            await transaction.RollbackAsync();

            return Conflict(new
            {
                message = "The requested end date conflicts with another employee assignment."
            });
        }
        catch (Exception ex) when (IsConcurrencyFailure(ex))
        {
            await transaction.RollbackAsync();

            return Conflict(new
            {
                message = "A concurrent assignment operation occurred. Please retry the request."
            });
        }
    }

    // Checks whether a proposed assignment date range overlaps
    // any existing assignment belonging to the employee.
    private async Task<bool> HasOverlappingAssignmentAsync(
        int employeeId,
        DateOnly proposedStartDate,
        DateOnly? proposedEndDate,
        int? excludedAssignmentId = null)
    {
        var effectiveEndDate =
            proposedEndDate ?? DateOnly.MaxValue;

        var query = _context.EmployeePositionHistories
            .Where(h =>
                h.EmployeeId == employeeId &&
                h.StartDate <= effectiveEndDate &&
                (h.EndDate == null ||
                 h.EndDate >= proposedStartDate));

        if (excludedAssignmentId.HasValue)
        {
            query = query.Where(h =>
                h.Id != excludedAssignmentId.Value);
        }

        return await query.AnyAsync();
    }

    // PostgreSQL row-level employee lock.
    private async Task<bool> LockEmployeeAsync(int employeeId)
    {
        var lockedRows = await _context.Database
            .SqlQuery<int>($"""
                SELECT "Id" AS "Value"
                FROM "Employees"
                WHERE "Id" = {employeeId}
                FOR UPDATE
                """)
            .ToListAsync();

        return lockedRows.Count > 0;
    }

    // PostgreSQL row-level position lock.
    private async Task<bool> LockPositionAsync(int positionId)
    {
        var lockedRows = await _context.Database
            .SqlQuery<int>($"""
                SELECT "Id" AS "Value"
                FROM "Positions"
                WHERE "Id" = {positionId}
                FOR UPDATE
                """)
            .ToListAsync();

        return lockedRows.Count > 0;
    }

    // Use the Nairobi business date on both Windows and Linux.
    private static DateOnly GetBusinessDate()
    {
        TimeZoneInfo timeZone;

        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(
                "Africa/Nairobi");
        }
        catch (TimeZoneNotFoundException)
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(
                "E. Africa Standard Time");
        }

        var localTime = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            timeZone);

        return DateOnly.FromDateTime(localTime);
    }

    private static bool IsUniqueViolation(Exception exception)
    {
        var postgresException = FindPostgresException(exception);

        return postgresException?.SqlState ==
            PostgresErrorCodes.UniqueViolation;
    }

    // PostgreSQL exclusion constraint violation: SQLSTATE 23P01.
    private static bool IsExclusionViolation(Exception exception)
    {
        var postgresException = FindPostgresException(exception);

        return postgresException?.SqlState ==
            PostgresErrorCodes.ExclusionViolation;
    }

    private static bool IsConcurrencyFailure(Exception exception)
    {
        var postgresException = FindPostgresException(exception);

        return postgresException?.SqlState is
            PostgresErrorCodes.SerializationFailure or
            PostgresErrorCodes.DeadlockDetected;
    }

    private static PostgresException? FindPostgresException(
        Exception exception)
    {
        Exception? current = exception;

        while (current != null)
        {
            if (current is PostgresException postgresException)
                return postgresException;

            current = current.InnerException;
        }

        return null;
    }

    private static PositionAssignmentResponse ToResponse(
        EmployeePositionHistory history)
    {
        return new PositionAssignmentResponse
        {
            Id = history.Id,

            EmployeeId = history.EmployeeId,
            EmployeeNumber = history.Employee.EmployeeNumber,
            EmployeeName =
                $"{history.Employee.FirstName} " +
                $"{history.Employee.LastName}",

            PositionId = history.PositionId,
            PositionCode = history.Position.Code,
            PositionTitle = history.Position.Title,

            DepartmentId = history.Position.DepartmentId,
            DepartmentName = history.Position.Department.Name,

            StartDate = history.StartDate,
            EndDate = history.EndDate,

            Status = history.Status.ToString(),

            ChangeReason = history.ChangeReason,

            IsCurrent =
                history.Status == PositionAssignmentStatus.Current,

            CreatedAt = history.CreatedAt
        };
    }
}

