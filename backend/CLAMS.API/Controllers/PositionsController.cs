
using CLAMS.API.DTOs.Positions;
using CLAMS.Domain.Entities;
using CLAMS.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CLAMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PositionsController : ControllerBase
{
    private readonly CLAMSHRDbContext _context;

    public PositionsController(CLAMSHRDbContext context)
    {
        _context = context;
    }

    // GET: api/positions
    [HttpGet]
    public async Task<IActionResult> GetPositions(
        [FromQuery] string? search,
        [FromQuery] int? departmentId,
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

        var query = _context.Positions
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            query = query.Where(p =>
                p.Code.Contains(term) ||
                p.Title.Contains(term));
        }

        if (departmentId.HasValue)
        {
            query = query.Where(p =>
                p.DepartmentId == departmentId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(p =>
                p.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync();

        var positions = await query
            .OrderBy(p => p.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PositionResponse
            {
                Id = p.Id,
                Code = p.Code,
                Title = p.Title,
                Description = p.Description,
                DepartmentId = p.DepartmentId,
                DepartmentName = p.Department.Name,
                Grade = p.Grade,
                ApprovedHeadcount = p.ApprovedHeadcount,

                OccupiedHeadcount = p.EmployeePositionHistories
                    .Count(h => h.EndDate == null),

                VacantHeadcount = Math.Max(
                    0,
                    p.ApprovedHeadcount -
                    p.EmployeePositionHistories.Count(h => h.EndDate == null)),

                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            page,
            pageSize,
            totalCount,
            totalPages = (int)Math.Ceiling(
                totalCount / (double)pageSize),
            positions
        });
    }

    // GET: api/positions/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetPosition(int id)
    {
        var position = await _context.Positions
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PositionResponse
            {
                Id = p.Id,
                Code = p.Code,
                Title = p.Title,
                Description = p.Description,
                DepartmentId = p.DepartmentId,
                DepartmentName = p.Department.Name,
                Grade = p.Grade,
                ApprovedHeadcount = p.ApprovedHeadcount,

                OccupiedHeadcount = p.EmployeePositionHistories
                    .Count(h => h.EndDate == null),

                VacantHeadcount = Math.Max(
                    0,
                    p.ApprovedHeadcount -
                    p.EmployeePositionHistories.Count(h => h.EndDate == null)),

                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (position == null)
        {
            return NotFound(new
            {
                message = $"Position with ID {id} was not found."
            });
        }

        return Ok(position);
    }

    // POST: api/positions
    [HttpPost]
    public async Task<IActionResult> CreatePosition(
        CreatePositionRequest request)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        var title = request.Title.Trim();

        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == request.DepartmentId);

        if (department == null)
        {
            return BadRequest(new
            {
                message = $"Department with ID {request.DepartmentId} was not found."
            });
        }

        if (!department.IsActive)
        {
            return BadRequest(new
            {
                message = "Positions cannot be created under an inactive department."
            });
        }

        var codeExists = await _context.Positions
            .AnyAsync(p => p.Code == code);

        if (codeExists)
        {
            return Conflict(new
            {
                message = $"Position code '{code}' already exists."
            });
        }

        if (request.ApprovedHeadcount < 1)
        {
            return BadRequest(new
            {
                message = "Approved headcount must be at least 1."
            });
        }

        var position = new Position
        {
            Code = code,
            Title = title,
            Description = request.Description?.Trim(),
            DepartmentId = department.Id,
            Grade = request.Grade?.Trim(),
            ApprovedHeadcount = request.ApprovedHeadcount,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Positions.Add(position);
        await _context.SaveChangesAsync();

        var response = new PositionResponse
        {
            Id = position.Id,
            Code = position.Code,
            Title = position.Title,
            Description = position.Description,
            DepartmentId = position.DepartmentId,
            DepartmentName = department.Name,
            Grade = position.Grade,
            ApprovedHeadcount = position.ApprovedHeadcount,
            OccupiedHeadcount = 0,
            VacantHeadcount = position.ApprovedHeadcount,
            IsActive = position.IsActive,
            CreatedAt = position.CreatedAt,
            UpdatedAt = position.UpdatedAt
        };

        return CreatedAtAction(
            nameof(GetPosition),
            new { id = position.Id },
            response);
    }

    // PUT: api/positions/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdatePosition(
        int id,
        UpdatePositionRequest request)
    {
        var position = await _context.Positions
            .FirstOrDefaultAsync(p => p.Id == id);

        if (position == null)
        {
            return NotFound(new
            {
                message = $"Position with ID {id} was not found."
            });
        }

        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == request.DepartmentId);

        if (department == null)
        {
            return BadRequest(new
            {
                message = $"Department with ID {request.DepartmentId} was not found."
            });
        }

        if (!department.IsActive)
        {
            return BadRequest(new
            {
                message = "Positions cannot be assigned to an inactive department."
            });
        }

        var code = request.Code.Trim().ToUpperInvariant();
        var title = request.Title.Trim();

        var codeExists = await _context.Positions
            .AnyAsync(p => p.Code == code && p.Id != id);

        if (codeExists)
        {
            return Conflict(new
            {
                message = $"Position code '{code}' is already assigned to another position."
            });
        }

        if (request.ApprovedHeadcount < 1)
        {
            return BadRequest(new
            {
                message = "Approved headcount must be at least 1."
            });
        }

        var occupiedHeadcount = await _context.EmployeePositionHistories
            .CountAsync(h =>
                h.PositionId == id &&
                h.EndDate == null);

        if (request.ApprovedHeadcount < occupiedHeadcount)
        {
            return BadRequest(new
            {
                message = $"Approved headcount cannot be less than the current occupied headcount of {occupiedHeadcount}."
            });
        }

        position.Code = code;
        position.Title = title;
        position.Description = request.Description?.Trim();
        position.DepartmentId = department.Id;
        position.Grade = request.Grade?.Trim();
        position.ApprovedHeadcount = request.ApprovedHeadcount;
        position.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var response = new PositionResponse
        {
            Id = position.Id,
            Code = position.Code,
            Title = position.Title,
            Description = position.Description,
            DepartmentId = position.DepartmentId,
            DepartmentName = department.Name,
            Grade = position.Grade,
            ApprovedHeadcount = position.ApprovedHeadcount,
            OccupiedHeadcount = occupiedHeadcount,
            VacantHeadcount = Math.Max(
                0,
                position.ApprovedHeadcount - occupiedHeadcount),
            IsActive = position.IsActive,
            CreatedAt = position.CreatedAt,
            UpdatedAt = position.UpdatedAt
        };

        return Ok(response);
    }

    // PATCH: api/positions/5/deactivate
    [HttpPatch("{id:int}/deactivate")]
    public async Task<IActionResult> DeactivatePosition(int id)
    {
        var position = await _context.Positions
            .FirstOrDefaultAsync(p => p.Id == id);

        if (position == null)
        {
            return NotFound(new
            {
                message = $"Position with ID {id} was not found."
            });
        }

        if (!position.IsActive)
        {
            return BadRequest(new
            {
                message = "This position is already inactive."
            });
        }

        var occupiedHeadcount = await _context.EmployeePositionHistories
            .CountAsync(h =>
                h.PositionId == id &&
                h.EndDate == null);

        if (occupiedHeadcount > 0)
        {
            return BadRequest(new
            {
                message = "This position cannot be deactivated while employees are assigned to it.",
                occupiedHeadcount
            });
        }

        position.IsActive = false;
        position.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Position deactivated successfully.",
            positionId = position.Id,
            positionCode = position.Code,
            positionTitle = position.Title,
            isActive = position.IsActive
        });
    }

    // PATCH: api/positions/5/activate
    [HttpPatch("{id:int}/activate")]
    public async Task<IActionResult> ActivatePosition(int id)
    {
        var position = await _context.Positions
            .FirstOrDefaultAsync(p => p.Id == id);

        if (position == null)
        {
            return NotFound(new
            {
                message = $"Position with ID {id} was not found."
            });
        }

        if (position.IsActive)
        {
            return BadRequest(new
            {
                message = "This position is already active."
            });
        }

        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == position.DepartmentId);

        if (department == null || !department.IsActive)
        {
            return BadRequest(new
            {
                message = "This position cannot be activated because its department is inactive or unavailable."
            });
        }

        position.IsActive = true;
        position.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Position activated successfully.",
            positionId = position.Id,
            positionCode = position.Code,
            positionTitle = position.Title,
            isActive = position.IsActive
        });
    }
}
