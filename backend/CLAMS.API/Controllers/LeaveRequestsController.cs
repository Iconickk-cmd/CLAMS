using System.ComponentModel.DataAnnotations;
using System.Data;
using CLAMS.API.Services;
using CLAMS.Domain.Entities;
using CLAMS.Domain.Enums;
using CLAMS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CLAMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class LeaveRequestsController : ControllerBase
{
    private readonly CLAMSHRDbContext _context;
    private readonly WorkingDayCalculator _workingDayCalculator;
    private readonly LeaveAllocationService _allocationService;

    public LeaveRequestsController(
        CLAMSHRDbContext context,
        WorkingDayCalculator workingDayCalculator,
        LeaveAllocationService allocationService)
    {
        _context = context;
        _workingDayCalculator = workingDayCalculator;
        _allocationService = allocationService;
    }

    private bool IsHrUser =>
        User.IsInRole("HRManager") ||
        User.IsInRole("HROfficer") ||
        User.IsInRole("SystemAdministrator");

    private bool TryGetEmployeeId(out int employeeId)
    {
        return int.TryParse(
            User.FindFirst("employeeId")?.Value,
            out employeeId);
    }

    // GET: api/LeaveRequests
    // Employees see their own requests. HR sees all requests.
    [HttpGet]
    public async Task<IActionResult> GetLeaveRequests(
        [FromQuery] LeaveRequestStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<LeaveRequest>()
            .AsNoTracking()
            .AsQueryable();

        if (!IsHrUser)
        {
            if (!TryGetEmployeeId(out var employeeId))
                return Unauthorized(new { message = "Employee identity was not found." });

            query = query.Where(r => r.EmployeeId == employeeId);
        }

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        var requests = await query
            .OrderByDescending(r => r.SubmittedAt)
            .Select(r => new
            {
                r.Id,
                r.EmployeeId,
                EmployeeName = r.Employee.FirstName + " " + r.Employee.LastName,
                r.LeaveTypeId,
                LeaveType = r.LeaveType.Name,
                LeaveTypeCode = r.LeaveType.Code,
                r.StartDate,
                r.EndDate,
                r.RequestedDays,
                r.ApprovedDays,
                r.Reason,
                r.Status,
                r.SubmittedAt,
                r.DecisionAt,
                r.DecisionBy,
                r.DecisionRemarks
            })
            .ToListAsync(cancellationToken);

        // Return date = next working day after the last day of leave.
        var returnDates = await _workingDayCalculator.GetNextWorkingDatesAsync(
            requests.Select(r => r.EndDate),
            cancellationToken);

        var response = requests
            .Select(r => new
            {
                r.Id,
                r.EmployeeId,
                r.EmployeeName,
                r.LeaveTypeId,
                r.LeaveType,
                r.LeaveTypeCode,
                r.StartDate,
                r.EndDate,
                ReturnDate = returnDates[r.EndDate],
                r.RequestedDays,
                r.ApprovedDays,
                r.Reason,
                r.Status,
                r.SubmittedAt,
                r.DecisionAt,
                r.DecisionBy,
                r.DecisionRemarks
            })
            .ToList();

        return Ok(new
        {
            totalCount = response.Count,
            requests = response
        });
    }

    // GET: api/LeaveRequests/5
    // Employees can view only their own requests.
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetLeaveRequest(
        int id,
        CancellationToken cancellationToken)
    {
        var query = _context.Set<LeaveRequest>()
            .AsNoTracking()
            .Where(r => r.Id == id);

        if (!IsHrUser)
        {
            if (!TryGetEmployeeId(out var employeeId))
                return Unauthorized(new { message = "Employee identity was not found." });

            query = query.Where(r => r.EmployeeId == employeeId);
        }

        var request = await query
            .Select(r => new
            {
                r.Id,
                r.EmployeeId,
                EmployeeName = r.Employee.FirstName + " " + r.Employee.LastName,
                r.LeaveTypeId,
                LeaveType = r.LeaveType.Name,
                r.StartDate,
                r.EndDate,
                r.RequestedDays,
                r.ApprovedDays,
                r.Reason,
                r.Status,
                r.SubmittedAt,
                r.DecisionAt,
                r.DecisionBy,
                r.DecisionRemarks,

                Allocations = r.Allocations.Select(a => new
                {
                    a.Id,
                    a.DaysAllocated,
                    Source = a.EmployeeLeaveBalanceId.HasValue
                        ? "CurrentYear"
                        : "CarryForward",
                    LeaveYear = a.EmployeeLeaveBalance != null
                        ? a.EmployeeLeaveBalance.LeaveYear
                        : (int?)null,
                    PayCategory = a.EmployeeLeaveBalance != null
                        ? a.EmployeeLeaveBalance.PayCategory.ToString()
                        : a.EmployeeLeaveCarryForward!.PayCategory.ToString(),
                    CarryForwardExpiry = a.EmployeeLeaveCarryForward != null
                        ? a.EmployeeLeaveCarryForward.ExpiresOn
                        : (DateOnly?)null
                }).ToList(),

                ApprovalHistory = r.ApprovalHistory
                    .OrderBy(h => h.ActionAt)
                    .Select(h => new
                    {
                        h.Id,
                        Action = h.Action.ToString(),
                        h.ActorName,
                        h.Comments,
                        h.ActionAt
                    }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (request is null)
            return NotFound(new { message = "Leave request was not found." });

        return Ok(request);
    }

    // POST: api/LeaveRequests
    // Submit a leave request for the authenticated employee.
    [HttpPost]
    public async Task<IActionResult> CreateLeaveRequest(
        [FromBody] CreateLeaveRequestDto dto,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId))
            return Unauthorized(new
            {
                message = "Employee identity was not found in the login token."
            });

        if (dto.StartDate > dto.EndDate)
            return BadRequest(new
            {
                message = "The start date cannot be after the end date."
            });

        if (dto.StartDate < BusinessClock.Today)
            return BadRequest(new
            {
                message = "Leave dates cannot be in the past."
            });

        if (string.IsNullOrWhiteSpace(dto.Reason))
            return BadRequest(new
            {
                message = "A reason for leave is required."
            });

        var employeeExists = await _context.Employees
            .AnyAsync(e => e.Id == employeeId, cancellationToken);

        if (!employeeExists)
            return NotFound(new { message = "Employee record was not found." });

        var leaveTypeExists = await _context.LeaveTypes
            .AnyAsync(
                l => l.Id == dto.LeaveTypeId && l.IsActive,
                cancellationToken);

        if (!leaveTypeExists)
            return BadRequest(new
            {
                message = "The selected leave type is invalid or inactive."
            });

        var workingDates = await _workingDayCalculator.GetWorkingDatesAsync(
            dto.StartDate,
            dto.EndDate,
            cancellationToken);

        if (workingDates.Count == 0)
            return BadRequest(new
            {
                message = "The selected period contains no working days."
            });

        var overlaps = await _context.Set<LeaveRequest>()
            .AnyAsync(r =>
                r.EmployeeId == employeeId &&
                (r.Status == LeaveRequestStatus.Pending ||
                 r.Status == LeaveRequestStatus.Approved) &&
                r.StartDate <= dto.EndDate &&
                r.EndDate >= dto.StartDate,
                cancellationToken);

        if (overlaps)
            return Conflict(new
            {
                message = "An existing pending or approved leave request overlaps these dates."
            });

        var request = new LeaveRequest
        {
            EmployeeId = employeeId,
            LeaveTypeId = dto.LeaveTypeId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            RequestedDays = workingDates.Count,
            ApprovedDays = 0m,
            Reason = dto.Reason.Trim(),
            Status = LeaveRequestStatus.Pending,
            SubmittedAt = DateTime.UtcNow
        };

        _context.Set<LeaveRequest>().Add(request);

        _context.Set<LeaveApprovalHistory>().Add(
            new LeaveApprovalHistory
            {
                LeaveRequest = request,
                Action = LeaveApprovalAction.Submitted,
                ActorName = User.Identity?.Name ?? employeeId.ToString(),
                Comments = "Leave request submitted."
            });

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = "Leave request submitted successfully and is awaiting approval.",
            request.Id,
            request.LeaveTypeId,
            request.StartDate,
            request.EndDate,
            request.RequestedDays,
            request.Status
        });
    }

    // POST: api/LeaveRequests/5/approve
    // HR approval automatically allocates leave balances.
    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = "HRManager,HROfficer,SystemAdministrator")]
    public async Task<IActionResult> ApproveLeaveRequest(
        int id,
        [FromBody] LeaveDecisionDto? dto,
        CancellationToken cancellationToken)
    {
        var actorName = User.Identity?.Name ?? "HR Administrator";

        int? actorEmployeeId = TryGetEmployeeId(out var actingEmployeeId)
            ? actingEmployeeId
            : null;

        var result = await _allocationService.ApproveAsync(
            id,
            actorName,
            dto?.Remarks,
            actorEmployeeId,
            cancellationToken);

        if (!result.Success)
        {
            if (result.Message == "Leave request was not found.")
                return NotFound(new { message = result.Message });

            if (result.Message == "Only pending leave requests can be approved.")
                return Conflict(new { message = result.Message });

            if (result.Message == LeaveAllocationService.SelfApprovalMessage)
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new { message = result.Message });

            return BadRequest(new { message = result.Message });
        }

        return Ok(new
        {
            message = result.Message,
            approvedDays = result.ApprovedDays
        });
    }

    // POST: api/LeaveRequests/5/reject
    // Rejection does not deduct or reserve leave balances.
    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = "HRManager,HROfficer,SystemAdministrator")]
    public async Task<IActionResult> RejectLeaveRequest(
        int id,
        [FromBody] LeaveDecisionDto? dto,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        var request = await _context.Set<LeaveRequest>()
            .FirstOrDefaultAsync(
                r => r.Id == id,
                cancellationToken);

        if (request is null)
            return NotFound(new { message = "Leave request was not found." });

        if (request.Status != LeaveRequestStatus.Pending)
            return Conflict(new
            {
                message = "Only pending leave requests can be rejected."
            });

        if (TryGetEmployeeId(out var rejectingEmployeeId) &&
            request.EmployeeId == rejectingEmployeeId)
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { message = "You cannot reject your own leave request." });

        request.Status = LeaveRequestStatus.Rejected;
        request.DecisionAt = DateTime.UtcNow;
        request.DecisionBy = User.Identity?.Name ?? "HR Administrator";
        request.DecisionRemarks = dto?.Remarks;

        _context.Set<LeaveApprovalHistory>().Add(
            new LeaveApprovalHistory
            {
                LeaveRequestId = request.Id,
                Action = LeaveApprovalAction.Rejected,
                ActorName = request.DecisionBy,
                Comments = dto?.Remarks
            });

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(new
        {
            message = "Leave request rejected successfully.",
            request.Id,
            request.Status,
            request.DecisionAt,
            request.DecisionBy,
            request.DecisionRemarks
        });
    }
}

public sealed class CreateLeaveRequestDto
{
    [Required]
    public int LeaveTypeId { get; set; }

    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }

    [Required]
    [StringLength(2000, MinimumLength = 3)]
    public string Reason { get; set; } = string.Empty;
}

public sealed class LeaveDecisionDto
{
    [StringLength(1000)]
    public string? Remarks { get; set; }
}
