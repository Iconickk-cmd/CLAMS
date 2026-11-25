using CLAMS.Domain.Enums;
using CLAMS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CLAMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "SystemAdministrator,HRManager")]
public class DashboardController : ControllerBase
{
    private readonly CLAMSHRDbContext _context;

    public DashboardController(CLAMSHRDbContext context)
    {
        _context = context;
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var totalEmployees = await _context.Employees
            .CountAsync();

        var activeEmployees = await _context.Employees
            .CountAsync(e => e.EmploymentStatus == EmploymentStatus.Active);

        var departments = await _context.Departments
            .CountAsync();

        var positions = await _context.Positions
            .CountAsync();

        var currentAssignments = await _context.EmployeePositionHistories
            .CountAsync(a => a.Status == PositionAssignmentStatus.Current);

        var pendingLeaveRequests = await _context.LeaveRequests
            .CountAsync(l => l.Status == LeaveRequestStatus.Pending);

        var approvedLeaveRequests = await _context.LeaveRequests
            .CountAsync(l => l.Status == LeaveRequestStatus.Approved);

        var rejectedLeaveRequests = await _context.LeaveRequests
            .CountAsync(l => l.Status == LeaveRequestStatus.Rejected);

        var employeesCurrentlyOnLeave = await _context.LeaveRequests
            .Where(l =>
                l.Status == LeaveRequestStatus.Approved &&
                l.StartDate <= today &&
                l.EndDate >= today)
            .Select(l => l.EmployeeId)
            .Distinct()
            .CountAsync();

        return Ok(new
        {
            totalEmployees,
            activeEmployees,
            departments,
            positions,
            currentAssignments,
            pendingLeaveRequests,
            approvedLeaveRequests,
            rejectedLeaveRequests,
            employeesCurrentlyOnLeave
        });
    }
}
