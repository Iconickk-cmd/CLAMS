using System.Data;
using CLAMS.API.DTOs.NextOfKin;
using CLAMS.Domain.Enums;
using CLAMS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CLAMS.API.Controllers;

[Route("api/employee-profile")]
[ApiController]
[Authorize]
public class EmployeeProfileController : ControllerBase
{
    private readonly CLAMSHRDbContext _context;

    public EmployeeProfileController(CLAMSHRDbContext context)
    {
        _context = context;
    }

    private bool TryGetEmployeeId(out int employeeId)
    {
        return int.TryParse(
            User.FindFirst("employeeId")?.Value,
            out employeeId);
    }

    // GET: api/employee-profile/me
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile(
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId))
            return Unauthorized(new { message = "Employee identity was not found." });

        var profile = await _context.Employees
            .AsNoTracking()
            .Where(e => e.Id == employeeId)
            .Select(e => new
            {
                e.Id,
                e.EmployeeNumber,
                e.FirstName,
                e.MiddleName,
                e.LastName,
                FullName = e.FirstName +
                    (e.MiddleName == null ? " " : " " + e.MiddleName + " ") +
                    e.LastName,

                e.NationalId,
                Gender = e.Gender.HasValue
                    ? e.Gender.Value.ToString()
                    : null,
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

                e.DepartmentId,
                DepartmentName = e.Department.Name,
                SupervisorName = e.Supervisor == null
                    ? null
                    : e.Supervisor.FirstName + " " + e.Supervisor.LastName,

                CurrentPosition = e.PositionHistories
                    .Where(h =>
                        h.Status == PositionAssignmentStatus.Current &&
                        h.EndDate == null)
                    .OrderByDescending(h => h.StartDate)
                    .Select(h => new
                    {
                        PositionId = (int?)h.PositionId,
                        h.Position.Code,
                        h.Position.Title,
                        h.Position.Grade,
                        PositionDepartment = h.Position.Department.Name
                    })
                    .FirstOrDefault(),

                e.CreatedAt,
                e.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (profile == null)
            return NotFound(new { message = "Employee profile was not found." });

        return Ok(profile);
    }

    // GET: api/employee-profile/me/next-of-kin
    [HttpGet("me/next-of-kin")]
    public async Task<IActionResult> GetNextOfKin(
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId))
            return Unauthorized(new { message = "Employee identity was not found." });

        var employeeExists = await _context.Employees
            .AnyAsync(e => e.Id == employeeId, cancellationToken);

        if (!employeeExists)
            return NotFound(new { message = "Employee record was not found." });

        var contacts = await _context.NextOfKinContacts
            .AsNoTracking()
            .Where(n => n.EmployeeId == employeeId)
            .OrderBy(n => n.Id)
            .Select(n => new NextOfKinResponse
            {
                Id = n.Id,
                FullName = n.FullName,
                Relationship = n.Relationship,
                PhoneNumber = n.PhoneNumber,
                AlternativePhoneNumber = n.AlternativePhoneNumber,
                Email = n.Email,
                PhysicalAddress = n.PhysicalAddress,
                CreatedAt = n.CreatedAt,
                UpdatedAt = n.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(contacts);
    }

    // POST: api/employee-profile/me/next-of-kin
    [HttpPost("me/next-of-kin")]
    public async Task<IActionResult> AddNextOfKin(
        [FromBody] NextOfKinRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId))
            return Unauthorized(new { message = "Employee identity was not found." });

        var employeeExists = await _context.Employees
            .AnyAsync(e => e.Id == employeeId, cancellationToken);

        if (!employeeExists)
            return NotFound(new { message = "Employee record was not found." });

        var contact = new CLAMS.Domain.Entities.NextOfKin
        {
            EmployeeId = employeeId,
            FullName = request.FullName.Trim(),
            Relationship = request.Relationship.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            AlternativePhoneNumber = NormalizeOptional(request.AlternativePhoneNumber),
            Email = NormalizeOptional(request.Email),
            PhysicalAddress = NormalizeOptional(request.PhysicalAddress),
            CreatedAt = DateTime.UtcNow
        };

        _context.NextOfKinContacts.Add(contact);
        await _context.SaveChangesAsync(cancellationToken);

        return Created(
            "/api/employee-profile/me/next-of-kin",
            new NextOfKinResponse
            {
                Id = contact.Id,
                FullName = contact.FullName,
                Relationship = contact.Relationship,
                PhoneNumber = contact.PhoneNumber,
                AlternativePhoneNumber = contact.AlternativePhoneNumber,
                Email = contact.Email,
                PhysicalAddress = contact.PhysicalAddress,
                CreatedAt = contact.CreatedAt,
                UpdatedAt = contact.UpdatedAt
            });
    }

    // PUT: api/employee-profile/me/next-of-kin/{id}
    [HttpPut("me/next-of-kin/{id:int}")]
    public async Task<IActionResult> UpdateNextOfKin(
        int id,
        [FromBody] NextOfKinRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId))
            return Unauthorized(new { message = "Employee identity was not found." });

        var contact = await _context.NextOfKinContacts
            .FirstOrDefaultAsync(
                n => n.Id == id && n.EmployeeId == employeeId,
                cancellationToken);

        if (contact == null)
            return NotFound(new { message = "Next of Kin contact was not found." });

        contact.FullName = request.FullName.Trim();
        contact.Relationship = request.Relationship.Trim();
        contact.PhoneNumber = request.PhoneNumber.Trim();
        contact.AlternativePhoneNumber = NormalizeOptional(request.AlternativePhoneNumber);
        contact.Email = NormalizeOptional(request.Email);
        contact.PhysicalAddress = NormalizeOptional(request.PhysicalAddress);
        contact.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new NextOfKinResponse
        {
            Id = contact.Id,
            FullName = contact.FullName,
            Relationship = contact.Relationship,
            PhoneNumber = contact.PhoneNumber,
            AlternativePhoneNumber = contact.AlternativePhoneNumber,
            Email = contact.Email,
            PhysicalAddress = contact.PhysicalAddress,
            CreatedAt = contact.CreatedAt,
            UpdatedAt = contact.UpdatedAt
        });
    }

    // DELETE: api/employee-profile/me/next-of-kin/{id}
    [HttpDelete("me/next-of-kin/{id:int}")]
    public async Task<IActionResult> DeleteNextOfKin(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId))
            return Unauthorized(new { message = "Employee identity was not found." });

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        var contact = await _context.NextOfKinContacts
            .FirstOrDefaultAsync(
                n => n.Id == id && n.EmployeeId == employeeId,
                cancellationToken);

        if (contact == null)
            return NotFound(new { message = "Next of Kin contact was not found." });

        var contactCount = await _context.NextOfKinContacts
            .CountAsync(n => n.EmployeeId == employeeId, cancellationToken);

        if (contactCount <= 1)
        {
            return Conflict(new
            {
                message = "At least one Next of Kin contact must remain registered."
            });
        }

        _context.NextOfKinContacts.Remove(contact);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return NoContent();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
