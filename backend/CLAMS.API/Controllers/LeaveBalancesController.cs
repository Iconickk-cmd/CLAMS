using CLAMS.API.Services;
using CLAMS.Domain.Entities;
using CLAMS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CLAMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class LeaveBalancesController : ControllerBase
{
    private readonly CLAMSHRDbContext _context;

    public LeaveBalancesController(CLAMSHRDbContext context)
    {
        _context = context;
    }

    // GET: api/LeaveBalances/me
    // The authenticated employee's own balances for a leave year.
    [HttpGet("me")]
    public async Task<IActionResult> GetMyBalances(
        [FromQuery] int? year = null,
        CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(
                User.FindFirst("employeeId")?.Value,
                out var employeeId))
        {
            return Unauthorized(new
            {
                message = "Employee identity was not found in the login token."
            });
        }

        return Ok(await BuildBalancesAsync(
            employeeId,
            year,
            cancellationToken));
    }

    // GET: api/LeaveBalances/employee/5
    // HR can view any employee's balances (used when reviewing requests).
    [HttpGet("employee/{employeeId:int}")]
    [Authorize(Roles = "HRManager,HROfficer,SystemAdministrator")]
    public async Task<IActionResult> GetEmployeeBalances(
        int employeeId,
        [FromQuery] int? year = null,
        CancellationToken cancellationToken = default)
    {
        var exists = await _context.Employees
            .AnyAsync(e => e.Id == employeeId, cancellationToken);

        if (!exists)
            return NotFound(new { message = "Employee record was not found." });

        return Ok(await BuildBalancesAsync(
            employeeId,
            year,
            cancellationToken));
    }

    private async Task<object> BuildBalancesAsync(
        int employeeId,
        int? year,
        CancellationToken cancellationToken)
    {
        var today = BusinessClock.Today;
        var leaveYear = year ?? today.Year;

        // Current-year balances. "Available" matches the rule used when
        // a request is approved: accrued + adjustments - taken.
        // Enum values are converted to text in memory (after the query)
        // so the database query stays simple.
        var currentYearRows = await _context.Set<EmployeeLeaveBalance>()
            .AsNoTracking()
            .Where(b =>
                b.EmployeeId == employeeId &&
                b.LeaveYear == leaveYear)
            .OrderBy(b => b.LeaveType.Name)
            .ThenBy(b => b.PayCategory)
            .Select(b => new
            {
                b.LeaveTypeId,
                LeaveTypeName = b.LeaveType.Name,
                b.PayCategory,
                b.AnnualEntitlementDays,
                b.AccruedDays,
                b.TakenDays,
                b.AdjustmentDays
            })
            .ToListAsync(cancellationToken);

        // Carry-forward lots that still have days and have not expired.
        var carryForwardRows = await _context.Set<EmployeeLeaveCarryForward>()
            .AsNoTracking()
            .Where(c =>
                c.EmployeeId == employeeId &&
                c.RemainingDays > 0 &&
                c.ExpiresOn >= today)
            .OrderBy(c => c.ExpiresOn)
            .Select(c => new
            {
                c.LeaveTypeId,
                LeaveTypeName = c.LeaveType.Name,
                c.PayCategory,
                c.OriginalDays,
                c.RemainingDays,
                c.ExpiresOn
            })
            .ToListAsync(cancellationToken);

        var currentYear = currentYearRows.Select(b => new
        {
            b.LeaveTypeId,
            LeaveType = b.LeaveTypeName,
            Source = "CurrentYear",
            PayCategory = b.PayCategory.ToString(),
            b.AnnualEntitlementDays,
            b.AccruedDays,
            b.TakenDays,
            b.AdjustmentDays,
            AvailableDays = b.AccruedDays + b.AdjustmentDays - b.TakenDays,
            ExpiresOn = (DateOnly?)null
        });

        var carryForwards = carryForwardRows.Select(c => new
        {
            c.LeaveTypeId,
            LeaveType = c.LeaveTypeName,
            Source = "CarryForward",
            PayCategory = c.PayCategory.ToString(),
            AnnualEntitlementDays = 0m,
            AccruedDays = c.OriginalDays,
            TakenDays = c.OriginalDays - c.RemainingDays,
            AdjustmentDays = 0m,
            AvailableDays = c.RemainingDays,
            ExpiresOn = (DateOnly?)c.ExpiresOn
        });

        return new
        {
            leaveYear,
            asOf = today,
            balances = currentYear.Concat(carryForwards).ToList()
        };
    }
}
