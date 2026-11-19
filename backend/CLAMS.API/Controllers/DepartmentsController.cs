
using CLAMS.API.DTOs.Departments;
using CLAMS.Domain.Entities;
using CLAMS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CLAMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DepartmentsController : ControllerBase
{
    private readonly CLAMSHRDbContext _context;

    public DepartmentsController(CLAMSHRDbContext context)
    {
        _context = context;
    }

    // GET: api/departments
    [Authorize(Roles = "SystemAdministrator,HRManager,HROfficer,Management,ReadOnly")]
    [HttpGet]
    public async Task<IActionResult> GetDepartments(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new
            {
                message = "Page must be at least 1 and pageSize must be between 1 and 100."
            });
        }

        var query = _context.Departments
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            query = query.Where(d =>
                d.Code.Contains(term) ||
                d.Name.Contains(term));
        }

        if (isActive.HasValue)
        {
            query = query.Where(d => d.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync();

        var departments = await query
            .OrderBy(d => d.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DepartmentResponse
            {
                Id = d.Id,
                Code = d.Code,
                Name = d.Name,
                Description = d.Description,
                IsActive = d.IsActive,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt,
                EmployeeCount = d.Employees.Count,
                PositionCount = d.Positions.Count
            })
            .ToListAsync();

        return Ok(new
        {
            page,
            pageSize,
            totalCount,
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            departments
        });
    }

    // GET: api/departments/5
    [Authorize(Roles = "SystemAdministrator,HRManager,HROfficer,Management,ReadOnly")]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDepartment(int id)
    {
        var department = await _context.Departments
            .AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new DepartmentResponse
            {
                Id = d.Id,
                Code = d.Code,
                Name = d.Name,
                Description = d.Description,
                IsActive = d.IsActive,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt,
                EmployeeCount = d.Employees.Count,
                PositionCount = d.Positions.Count
            })
            .FirstOrDefaultAsync();

        if (department == null)
        {
            return NotFound(new
            {
                message = $"Department with ID {id} was not found."
            });
        }

        return Ok(department);
    }

    // POST: api/departments
    [Authorize(Roles = "SystemAdministrator,HRManager")]
    [HttpPost]
    public async Task<IActionResult> CreateDepartment(
        CreateDepartmentRequest request)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        var name = request.Name.Trim();

        var codeExists = await _context.Departments
            .AnyAsync(d => d.Code == code);

        if (codeExists)
        {
            return Conflict(new
            {
                message = $"Department code '{code}' already exists."
            });
        }

        var department = new Department
        {
            Code = code,
            Name = name,
            Description = request.Description?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

        var response = new DepartmentResponse
        {
            Id = department.Id,
            Code = department.Code,
            Name = department.Name,
            Description = department.Description,
            IsActive = department.IsActive,
            CreatedAt = department.CreatedAt,
            UpdatedAt = department.UpdatedAt,
            EmployeeCount = 0,
            PositionCount = 0
        };

        return CreatedAtAction(
            nameof(GetDepartment),
            new { id = department.Id },
            response);
    }

    // PUT: api/departments/5
    [Authorize(Roles = "SystemAdministrator,HRManager")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateDepartment(
        int id,
        UpdateDepartmentRequest request)
    {
        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == id);

        if (department == null)
        {
            return NotFound(new
            {
                message = $"Department with ID {id} was not found."
            });
        }

        var code = request.Code.Trim().ToUpperInvariant();
        var name = request.Name.Trim();

        var codeExists = await _context.Departments
            .AnyAsync(d => d.Code == code && d.Id != id);

        if (codeExists)
        {
            return Conflict(new
            {
                message = $"Department code '{code}' is already assigned to another department."
            });
        }

        department.Code = code;
        department.Name = name;
        department.Description = request.Description?.Trim();
        department.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new DepartmentResponse
        {
            Id = department.Id,
            Code = department.Code,
            Name = department.Name,
            Description = department.Description,
            IsActive = department.IsActive,
            CreatedAt = department.CreatedAt,
            UpdatedAt = department.UpdatedAt,
            EmployeeCount = await _context.Employees.CountAsync(
                e => e.DepartmentId == id),
            PositionCount = await _context.Positions.CountAsync(
                p => p.DepartmentId == id)
        });
    }

    // PATCH: api/departments/5/deactivate
    [Authorize(Roles = "SystemAdministrator,HRManager")]
    [HttpPatch("{id:int}/deactivate")]
    public async Task<IActionResult> DeactivateDepartment(int id)
    {
        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == id);

        if (department == null)
        {
            return NotFound(new
            {
                message = $"Department with ID {id} was not found."
            });
        }

        if (!department.IsActive)
        {
            return BadRequest(new
            {
                message = "This department is already inactive."
            });
        }

        department.IsActive = false;
        department.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Department deactivated successfully.",
            departmentId = department.Id,
            departmentCode = department.Code,
            departmentName = department.Name,
            isActive = department.IsActive
        });
    }

    // PATCH: api/departments/5/activate
    [Authorize(Roles = "SystemAdministrator,HRManager")]
    [HttpPatch("{id:int}/activate")]
    public async Task<IActionResult> ActivateDepartment(int id)
    {
        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == id);

        if (department == null)
        {
            return NotFound(new
            {
                message = $"Department with ID {id} was not found."
            });
        }

        if (department.IsActive)
        {
            return BadRequest(new
            {
                message = "This department is already active."
            });
        }

        department.IsActive = true;
        department.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Department activated successfully.",
            departmentId = department.Id,
            departmentCode = department.Code,
            departmentName = department.Name,
            isActive = department.IsActive
        });
    }
}
