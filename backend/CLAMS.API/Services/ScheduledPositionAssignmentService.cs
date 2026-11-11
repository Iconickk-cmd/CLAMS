
using System.Data;
using CLAMS.Domain.Enums;
using CLAMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CLAMS.API.Services;

public class ScheduledPositionAssignmentService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScheduledPositionAssignmentService> _logger;

    public ScheduledPositionAssignmentService(
        IServiceScopeFactory scopeFactory,
        ILogger<ScheduledPositionAssignmentService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Scheduled position assignment service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var dueAssignmentIds =
                    await GetDueAssignmentIdsAsync(stoppingToken);

                foreach (var assignmentId in dueAssignmentIds)
                {
                    if (stoppingToken.IsCancellationRequested)
                        break;

                    await ActivateAssignmentAsync(
                        assignmentId,
                        stoppingToken);
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An error occurred while processing scheduled assignments.");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromMinutes(1),
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation(
            "Scheduled position assignment service stopped.");
    }

    private async Task<List<int>> GetDueAssignmentIdsAsync(
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<CLAMSHRDbContext>();

        var today = GetBusinessDate();

        return await context.EmployeePositionHistories
            .AsNoTracking()
            .Where(h =>
                h.Status == PositionAssignmentStatus.Scheduled &&
                h.StartDate <= today)
            .OrderBy(h => h.StartDate)
            .ThenBy(h => h.Id)
            .Select(h => h.Id)
            .ToListAsync(cancellationToken);
    }

    private async Task ActivateAssignmentAsync(
        int assignmentId,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<CLAMSHRDbContext>();

        await using var transaction =
            await context.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        try
        {
            // Find the employee associated with the scheduled assignment.
            var employeeId = await context.EmployeePositionHistories
                .Where(h =>
                    h.Id == assignmentId &&
                    h.Status == PositionAssignmentStatus.Scheduled)
                .Select(h => (int?)h.EmployeeId)
                .FirstOrDefaultAsync(cancellationToken);

            if (!employeeId.HasValue)
                return;

            // Lock the employee first to serialize assignment changes.
            if (!await LockEmployeeAsync(
                context,
                employeeId.Value,
                cancellationToken))
            {
                _logger.LogWarning(
                    "Employee {EmployeeId} was not found while activating assignment {AssignmentId}.",
                    employeeId.Value,
                    assignmentId);

                return;
            }

            var assignment = await context.EmployeePositionHistories
                .Include(h => h.Employee)
                .Include(h => h.Position)
                    .ThenInclude(p => p.Department)
                .FirstOrDefaultAsync(h =>
                    h.Id == assignmentId &&
                    h.Status == PositionAssignmentStatus.Scheduled,
                    cancellationToken);

            if (assignment == null)
                return;

            if (assignment.StartDate > GetBusinessDate())
                return;

            var employee = assignment.Employee;
            var position = assignment.Position;

            if (employee.EmploymentStatus != EmploymentStatus.Active)
            {
                _logger.LogWarning(
                    "Scheduled assignment {AssignmentId} was not activated because employee {EmployeeId} is not active.",
                    assignmentId,
                    employee.Id);

                return;
            }

            // Lock the destination position before checking capacity.
            if (!await LockPositionAsync(
                context,
                position.Id,
                cancellationToken))
            {
                _logger.LogWarning(
                    "Destination position {PositionId} was not found for assignment {AssignmentId}.",
                    position.Id,
                    assignmentId);

                return;
            }

            // Reload the position after acquiring its lock.
            await context.Entry(position).ReloadAsync(cancellationToken);

            await context.Entry(position)
                .Reference(p => p.Department)
                .LoadAsync(cancellationToken);

            if (!position.IsActive || !position.Department.IsActive)
            {
                _logger.LogWarning(
                    "Scheduled assignment {AssignmentId} was not activated because its position or department is inactive.",
                    assignmentId);

                return;
            }

            // Check capacity against other employees currently occupying
            // the destination position.
            var occupiedHeadcount =
                await context.EmployeePositionHistories
                    .CountAsync(h =>
                        h.PositionId == position.Id &&
                        h.EmployeeId != employee.Id &&
                        h.Status == PositionAssignmentStatus.Current,
                        cancellationToken);

            if (occupiedHeadcount >= position.ApprovedHeadcount)
            {
                _logger.LogWarning(
                    "Scheduled assignment {AssignmentId} could not be activated because position {PositionId} has reached its approved headcount.",
                    assignmentId,
                    position.Id);

                return;
            }

            // Find the employee's existing current assignment.
            var previousAssignment =
                await context.EmployeePositionHistories
                    .FirstOrDefaultAsync(h =>
                        h.EmployeeId == employee.Id &&
                        h.Status == PositionAssignmentStatus.Current,
                        cancellationToken);

            if (previousAssignment != null)
            {
                if (previousAssignment.StartDate >= assignment.StartDate)
                {
                    _logger.LogError(
                        "Scheduled assignment {AssignmentId} has a conflicting start date with current assignment {CurrentAssignmentId}.",
                        assignmentId,
                        previousAssignment.Id);

                    return;
                }

                // Close the previous assignment one day before
                // the scheduled assignment becomes effective.
                previousAssignment.EndDate =
                    assignment.StartDate.AddDays(-1);

                previousAssignment.Status =
                    PositionAssignmentStatus.Completed;
            }

            // Activate the scheduled assignment.
            assignment.Status = PositionAssignmentStatus.Current;

            // Synchronize the employee's department.
            employee.DepartmentId = position.DepartmentId;
            employee.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Scheduled assignment {AssignmentId} was successfully activated for employee {EmployeeId}.",
                assignmentId,
                employee.Id);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(CancellationToken.None);

            _logger.LogError(
                ex,
                "Failed to activate scheduled assignment {AssignmentId}.",
                assignmentId);
        }
    }

    private static async Task<bool> LockEmployeeAsync(
        CLAMSHRDbContext context,
        int employeeId,
        CancellationToken cancellationToken)
    {
        var lockedRows = await context.Database
            .SqlQuery<int>($"""
                SELECT "Id" AS "Value"
                FROM "Employees"
                WHERE "Id" = {employeeId}
                FOR UPDATE
                """)
            .ToListAsync(cancellationToken);

        return lockedRows.Count > 0;
    }

    private static async Task<bool> LockPositionAsync(
        CLAMSHRDbContext context,
        int positionId,
        CancellationToken cancellationToken)
    {
        var lockedRows = await context.Database
            .SqlQuery<int>($"""
                SELECT "Id" AS "Value"
                FROM "Positions"
                WHERE "Id" = {positionId}
                FOR UPDATE
                """)
            .ToListAsync(cancellationToken);

        return lockedRows.Count > 0;
    }

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
}
