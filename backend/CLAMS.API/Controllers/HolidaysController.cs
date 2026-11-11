
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
public class HolidaysController : ControllerBase
{
    private readonly CLAMSHRDbContext _context;

    public HolidaysController(CLAMSHRDbContext context)
    {
        _context = context;
    }

    // GET: api/holidays
    [HttpGet]
    public async Task<IActionResult> GetHolidays(
        [FromQuery] int? year,
        [FromQuery] HolidayCategory? category,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new
            {
                message = "Page must be at least 1 and pageSize must be between 1 and 100."
            });
        }

        if (year.HasValue && (year.Value < 1900 || year.Value > 9999))
        {
            return BadRequest(new
            {
                message = "The year must be between 1900 and 9999."
            });
        }

        if (category.HasValue &&
            !Enum.IsDefined(typeof(HolidayCategory), category.Value))
        {
            return BadRequest(new
            {
                message = "Invalid holiday category."
            });
        }

        var query = _context.Holidays
            .AsNoTracking()
            .AsQueryable();

        if (year.HasValue)
        {
            query = query.Where(h => h.HolidayDate.Year == year.Value);
        }

        if (category.HasValue)
        {
            query = query.Where(h => h.Category == category.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(h => h.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync();

        var holidays = await query
            .OrderBy(h => h.HolidayDate)
            .ThenBy(h => h.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new
            {
                h.Id,
                h.Name,
                h.Description,
                h.HolidayDate,
                Category = h.Category.ToString(),
                h.IsActive,
                h.CreatedAt,
                h.UpdatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            page,
            pageSize,
            totalCount,
            totalPages = (int)Math.Ceiling(
                totalCount / (double)pageSize),
            holidays
        });
    }

    // GET: api/holidays/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetHoliday(int id)
    {
        var holiday = await _context.Holidays
            .AsNoTracking()
            .Where(h => h.Id == id)
            .Select(h => new
            {
                h.Id,
                h.Name,
                h.Description,
                h.HolidayDate,
                Category = h.Category.ToString(),
                h.IsActive,
                h.CreatedAt,
                h.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (holiday == null)
        {
            return NotFound(new
            {
                message = $"Holiday with ID {id} was not found."
            });
        }

        return Ok(holiday);
    }

    // POST: api/holidays
    [HttpPost]
    [Authorize(Roles = "HRManager,HROfficer,SystemAdministrator")]
    public async Task<IActionResult> CreateHoliday(
        [FromBody] HolidayRequest request)
    {
        var validationError = ValidateRequest(request);

        if (validationError != null)
        {
            return BadRequest(new { message = validationError });
        }

        var name = request.Name.Trim();

        var duplicateExists = await _context.Holidays
            .AnyAsync(h =>
                h.HolidayDate == request.HolidayDate &&
                h.Category == request.Category);

        if (duplicateExists)
        {
            return Conflict(new
            {
                message = "A holiday of this category already exists on this date."
            });
        }

        var holiday = new Holiday
        {
            Name = name,
            Description = request.Description?.Trim(),
            HolidayDate = request.HolidayDate,
            Category = request.Category,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Holidays.Add(holiday);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetHoliday),
            new { id = holiday.Id },
            new
            {
                holiday.Id,
                holiday.Name,
                holiday.Description,
                holiday.HolidayDate,
                Category = holiday.Category.ToString(),
                holiday.IsActive,
                holiday.CreatedAt,
                holiday.UpdatedAt
            });
    }

    // PUT: api/holidays/5
    [HttpPut("{id:int}")]
    [Authorize(Roles = "HRManager,HROfficer,SystemAdministrator")]
    public async Task<IActionResult> UpdateHoliday(
        int id,
        [FromBody] HolidayRequest request)
    {
        var validationError = ValidateRequest(request);

        if (validationError != null)
        {
            return BadRequest(new { message = validationError });
        }

        var holiday = await _context.Holidays
            .FirstOrDefaultAsync(h => h.Id == id);

        if (holiday == null)
        {
            return NotFound(new
            {
                message = $"Holiday with ID {id} was not found."
            });
        }

        var name = request.Name.Trim();

        var duplicateExists = await _context.Holidays
            .AnyAsync(h =>
                h.Id != id &&
                h.HolidayDate == request.HolidayDate &&
                h.Category == request.Category);

        if (duplicateExists)
        {
            return Conflict(new
            {
                message = "Another holiday of this category already exists on this date."
            });
        }

        holiday.Name = name;
        holiday.Description = request.Description?.Trim();
        holiday.HolidayDate = request.HolidayDate;
        holiday.Category = request.Category;
        holiday.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Holiday updated successfully.",
            holiday.Id,
            holiday.Name,
            holiday.Description,
            holiday.HolidayDate,
            Category = holiday.Category.ToString(),
            holiday.IsActive,
            holiday.CreatedAt,
            holiday.UpdatedAt
        });
    }

    // PATCH: api/holidays/5/deactivate
    [HttpPatch("{id:int}/deactivate")]
    [Authorize(Roles = "HRManager,HROfficer,SystemAdministrator")]
    public async Task<IActionResult> DeactivateHoliday(int id)
    {
        var holiday = await _context.Holidays
            .FirstOrDefaultAsync(h => h.Id == id);

        if (holiday == null)
        {
            return NotFound(new
            {
                message = $"Holiday with ID {id} was not found."
            });
        }

        if (!holiday.IsActive)
        {
            return BadRequest(new
            {
                message = "This holiday is already inactive."
            });
        }

        holiday.IsActive = false;
        holiday.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Holiday deactivated successfully.",
            holidayId = holiday.Id,
            holidayName = holiday.Name,
            holidayDate = holiday.HolidayDate,
            isActive = holiday.IsActive
        });
    }

    // PATCH: api/holidays/5/activate
    [HttpPatch("{id:int}/activate")]
    [Authorize(Roles = "HRManager,HROfficer,SystemAdministrator")]
    public async Task<IActionResult> ActivateHoliday(int id)
    {
        var holiday = await _context.Holidays
            .FirstOrDefaultAsync(h => h.Id == id);

        if (holiday == null)
        {
            return NotFound(new
            {
                message = $"Holiday with ID {id} was not found."
            });
        }

        if (holiday.IsActive)
        {
            return BadRequest(new
            {
                message = "This holiday is already active."
            });
        }

        holiday.IsActive = true;
        holiday.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Holiday activated successfully.",
            holidayId = holiday.Id,
            holidayName = holiday.Name,
            holidayDate = holiday.HolidayDate,
            isActive = holiday.IsActive
        });
    }

    private static string? ValidateRequest(HolidayRequest request)
    {
        if (request == null)
        {
            return "A holiday request is required.";
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return "Holiday name is required.";
        }

        if (request.Name.Trim().Length > 150)
        {
            return "Holiday name cannot exceed 150 characters.";
        }

        if (request.Description?.Length > 500)
        {
            return "Holiday description cannot exceed 500 characters.";
        }

        if (request.HolidayDate == default)
        {
            return "A valid holiday date is required.";
        }

        if (!Enum.IsDefined(typeof(HolidayCategory), request.Category))
        {
            return "Invalid holiday category.";
        }

        return null;
    }
}

public class HolidayRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateOnly HolidayDate { get; set; }

    public HolidayCategory Category { get; set; }
}
