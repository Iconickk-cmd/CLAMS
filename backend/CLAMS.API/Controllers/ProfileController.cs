using CLAMS.Domain.Enums;
using CLAMS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CLAMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly CLAMSHRDbContext _context;

    public ProfileController(CLAMSHRDbContext context)
    {
        _context = context;
    }

    // GET: api/Profile/me
    // Read-only view of the signed-in employee's own record.
    // Any signed-in user can read their own profile; the employee id
    // always comes from the login token, never from the request.
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile(
        CancellationToken cancellationToken)
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

        var e = await _context.Employees
            .AsNoTracking()
            .Where(x => x.Id == employeeId)
            .Select(x => new
            {
                x.Id,
                x.EmployeeNumber,
                x.FirstName,
                x.MiddleName,
                x.LastName,
                x.NationalId,
                x.Gender,
                x.DateOfBirth,
                x.Email,
                x.PhoneNumber,
                x.ResidentialAddress,
                x.EmergencyContactName,
                x.EmergencyContactPhone,
                x.EmergencyContactRelationship,
                x.DateOfEmployment,
                x.EmploymentType,
                x.EmploymentStatus,
                DepartmentName = x.Department.Name,
                SupervisorName = x.Supervisor == null
                    ? null
                    : x.Supervisor.FirstName + " " + x.Supervisor.LastName,
                CurrentPositionTitle = x.PositionHistories
                    .Where(p => p.Status == PositionAssignmentStatus.Current)
                    .OrderByDescending(p => p.StartDate)
                    .Select(p => p.Position.Title)
                    .FirstOrDefault(),
                PositionStartDate = x.PositionHistories
                    .Where(p => p.Status == PositionAssignmentStatus.Current)
                    .OrderByDescending(p => p.StartDate)
                    .Select(p => (DateOnly?)p.StartDate)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (e == null)
        {
            return NotFound(new
            {
                message = "Your employee record was not found."
            });
        }

        var fullName = string.Join(
            " ",
            new[] { e.FirstName, e.MiddleName, e.LastName }
                .Where(part => !string.IsNullOrWhiteSpace(part)));

        return Ok(new
        {
            e.Id,
            e.EmployeeNumber,
            e.FirstName,
            e.MiddleName,
            e.LastName,
            FullName = fullName,
            e.NationalId,
            Gender = e.Gender?.ToString(),
            e.DateOfBirth,
            e.Email,
            e.PhoneNumber,
            e.ResidentialAddress,
            e.EmergencyContactName,
            e.EmergencyContactPhone,
            e.EmergencyContactRelationship,
            e.DateOfEmployment,
            EmploymentType = e.EmploymentType.ToString(),
            EmploymentStatus = e.EmploymentStatus.ToString(),
            e.DepartmentName,
            e.CurrentPositionTitle,
            e.SupervisorName,
            e.PositionStartDate
        });
    }
}
