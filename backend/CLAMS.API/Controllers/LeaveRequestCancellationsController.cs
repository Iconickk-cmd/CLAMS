using CLAMS.API.Services;
using CLAMS.Domain.Entities;
using CLAMS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CLAMS.API.Controllers;

[ApiController]
[Route("api/LeaveRequests")]
[Authorize]
public class LeaveRequestCancellationsController : ControllerBase
{
    private readonly CLAMSHRDbContext _context;
    private readonly LeaveCancellationService _cancellationService;

    public LeaveRequestCancellationsController(
        CLAMSHRDbContext context,
        LeaveCancellationService cancellationService)
    {
        _context = context;
        _cancellationService = cancellationService;
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(
        int id,
        [FromBody] CancelLeaveRequestDto? dto,
        CancellationToken cancellationToken)
    {
        var isHrUser =
            User.IsInRole("HRManager") ||
            User.IsInRole("HROfficer") ||
            User.IsInRole("SystemAdministrator");

        if (!isHrUser)
        {
            if (!int.TryParse(
                User.FindFirst("employeeId")?.Value,
                out var employeeId))
            {
                return Unauthorized(new
                {
                    message = "Employee identity could not be verified."
                });
            }

            var belongsToEmployee = await _context.Set<LeaveRequest>()
                .AnyAsync(
                    r => r.Id == id && r.EmployeeId == employeeId,
                    cancellationToken);

            if (!belongsToEmployee)
            {
                return NotFound(new
                {
                    message = "Leave request was not found."
                });
            }
        }

        var result = await _cancellationService.CancelAsync(
            id,
            User.Identity?.Name ?? "User",
            dto?.Remarks,
            cancellationToken);

        if (!result.Success)
        {
            if (result.Message == "Leave request was not found.")
                return NotFound(new { message = result.Message });

            return Conflict(new { message = result.Message });
        }

        return Ok(new
        {
            message = result.Message
        });
    }
}

public class CancelLeaveRequestDto
{
    public string? Remarks { get; set; }
}
