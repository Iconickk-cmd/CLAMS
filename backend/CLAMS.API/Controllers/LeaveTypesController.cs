
using CLAMS.Domain.Entities;
using CLAMS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CLAMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class LeaveTypesController : ControllerBase
{
    private readonly CLAMSHRDbContext _context;

    public LeaveTypesController(CLAMSHRDbContext context)
    {
        _context = context;
    }

    // GET: api/LeaveTypes
    // Any authenticated user can view leave types.
    [HttpGet]
    public async Task<IActionResult> GetLeaveTypes(
        [FromQuery] bool? isActive = null,
        [FromQuery] bool? isPaid = null,
        [FromQuery] string? search = null)
    {
        var query = _context.LeaveTypes
            .AsNoTracking()
            .AsQueryable();

        if (isActive.HasValue)
            query = query.Where(l => l.IsActive == isActive.Value);

        if (isPaid.HasValue)
            query = query.Where(l => l.IsPaid == isPaid.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();

            query = query.Where(l =>
                l.Code.ToLower().Contains(term) ||
                l.Name.ToLower().Contains(term));
        }

        var leaveTypes = await query
            .OrderBy(l => l.Name)
            .Select(l => new
            {
                l.Id,
                l.Code,
                l.Name,
                l.Description,
                l.IsPaid,
                l.RequiresApproval,
                l.IsActive,
                l.CreatedAt,
                l.UpdatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            totalCount = leaveTypes.Count,
            leaveTypes
        });
    }

    // GET: api/LeaveTypes/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetLeaveType(int id)
    {
        var leaveType = await _context.LeaveTypes
            .AsNoTracking()
            .Where(l => l.Id == id)
            .Select(l => new
            {
                l.Id,
                l.Code,
                l.Name,
                l.Description,
                l.IsPaid,
                l.RequiresApproval,
                l.IsActive,
                l.CreatedAt,
                l.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (leaveType == null)
            return NotFound(new { message = "Leave type not found." });

        return Ok(leaveType);
    }

    // POST: api/LeaveTypes
    [HttpPost]
    [Authorize(Roles = "HRManager,HROfficer,SystemAdministrator")]
    public async Task<IActionResult> CreateLeaveType(
        [FromBody] LeaveTypeRequest request)
    {
        var validationError = ValidateRequest(request);

        if (validationError != null)
            return BadRequest(new { message = validationError });

        var code = request.Code.Trim().ToUpperInvariant();

        var duplicateCode = await _context.LeaveTypes
            .AnyAsync(l => l.Code == code);

        if (duplicateCode)
        {
            return Conflict(new
            {
                message = "A leave type with this code already exists."
            });
        }

        var leaveType = new LeaveType
        {
            Code = code,
            Name = request.Name.Trim(),
            Description = NormalizeDescription(request.Description),
            IsPaid = request.IsPaid,
            RequiresApproval = request.RequiresApproval,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.LeaveTypes.Add(leaveType);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetLeaveType),
            new { id = leaveType.Id },
            leaveType);
    }

    // PUT: api/LeaveTypes/5
    [HttpPut("{id:int}")]
    [Authorize(Roles = "HRManager,HROfficer,SystemAdministrator")]
    public async Task<IActionResult> UpdateLeaveType(
        int id,
        [FromBody] LeaveTypeRequest request)
    {
        var validationError = ValidateRequest(request);

        if (validationError != null)
            return BadRequest(new { message = validationError });

        var leaveType = await _context.LeaveTypes
            .FirstOrDefaultAsync(l => l.Id == id);

        if (leaveType == null)
            return NotFound(new { message = "Leave type not found." });

        var code = request.Code.Trim().ToUpperInvariant();

        var duplicateCode = await _context.LeaveTypes
            .AnyAsync(l => l.Id != id && l.Code == code);

        if (duplicateCode)
        {
            return Conflict(new
            {
                message = "Another leave type already uses this code."
            });
        }

        leaveType.Code = code;
        leaveType.Name = request.Name.Trim();
        leaveType.Description = NormalizeDescription(request.Description);
        leaveType.IsPaid = request.IsPaid;
        leaveType.RequiresApproval = request.RequiresApproval;
        leaveType.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Leave type updated successfully.",
            leaveType
        });
    }

    // PATCH: api/LeaveTypes/5/deactivate
    [HttpPatch("{id:int}/deactivate")]
    [Authorize(Roles = "HRManager,HROfficer,SystemAdministrator")]
    public async Task<IActionResult> DeactivateLeaveType(int id)
    {
        var leaveType = await _context.LeaveTypes
            .FirstOrDefaultAsync(l => l.Id == id);

        if (leaveType == null)
            return NotFound(new { message = "Leave type not found." });

        if (!leaveType.IsActive)
        {
            return BadRequest(new
            {
                message = "This leave type is already inactive."
            });
        }

        leaveType.IsActive = false;
        leaveType.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Leave type deactivated successfully."
        });
    }

    // PATCH: api/LeaveTypes/5/activate
    [HttpPatch("{id:int}/activate")]
    [Authorize(Roles = "HRManager,HROfficer,SystemAdministrator")]
    public async Task<IActionResult> ActivateLeaveType(int id)
    {
        var leaveType = await _context.LeaveTypes
            .FirstOrDefaultAsync(l => l.Id == id);

        if (leaveType == null)
            return NotFound(new { message = "Leave type not found." });

        if (leaveType.IsActive)
        {
            return BadRequest(new
            {
                message = "This leave type is already active."
            });
        }

        leaveType.IsActive = true;
        leaveType.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Leave type activated successfully."
        });
    }

    private static string? ValidateRequest(LeaveTypeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return "Leave type code is required.";

        if (request.Code.Trim().Length > 30)
            return "Leave type code cannot exceed 30 characters.";

        if (string.IsNullOrWhiteSpace(request.Name))
            return "Leave type name is required.";

        if (request.Name.Trim().Length > 100)
            return "Leave type name cannot exceed 100 characters.";

        if (request.Description?.Length > 500)
            return "Description cannot exceed 500 characters.";

        return null;
    }

    private static string? NormalizeDescription(string? description)
    {
        return string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
    }
}

public class LeaveTypeRequest
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsPaid { get; set; } = true;

    public bool RequiresApproval { get; set; } = true;
}
