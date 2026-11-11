
using CLAMS.API.DTOs.Employees;
using CLAMS.Domain.Entities;
using CLAMS.Domain.Enums;
using CLAMS.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CLAMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EmployeesController : ControllerBase
{
    private readonly CLAMSHRDbContext _context;

    public EmployeesController(CLAMSHRDbContext context)
    {
        _context = context;
    }

    // GET: api/employees
    [HttpGet]
    public async Task<IActionResult> GetEmployees(
        [FromQuery] string? search,
        [FromQuery] int? departmentId,
        [FromQuery] EmploymentStatus? employmentStatus,
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

        var query = _context.Employees
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            query = query.Where(e =>
                e.EmployeeNumber.Contains(term) ||
                e.FirstName.Contains(term) ||
                e.LastName.Contains(term) ||
                (e.NationalId != null && e.NationalId.Contains(term)) ||
                (e.Email != null && e.Email.Contains(term)));
        }

        if (departmentId.HasValue)
        {
            query = query.Where(e =>
                e.DepartmentId == departmentId.Value);
        }

        if (employmentStatus.HasValue)
        {
            query = query.Where(e =>
                e.EmploymentStatus == employmentStatus.Value);
        }

        var totalCount = await query.CountAsync();

        var employees = await query
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EmployeeResponse
            {
                Id = e.Id,
                EmployeeNumber = e.EmployeeNumber,

                FirstName = e.FirstName,
                MiddleName = e.MiddleName,
                LastName = e.LastName,
                FullName = e.FirstName +
                    (e.MiddleName == null ? " " : " " + e.MiddleName + " ") +
                    e.LastName,

                NationalId = e.NationalId,
                Gender = e.Gender,
                DateOfBirth = e.DateOfBirth,

                Email = e.Email,
                PhoneNumber = e.PhoneNumber,
                ResidentialAddress = e.ResidentialAddress,

                EmergencyContactName = e.EmergencyContactName,
                EmergencyContactPhone = e.EmergencyContactPhone,
                EmergencyContactRelationship = e.EmergencyContactRelationship,

                DateOfEmployment = e.DateOfEmployment,
                EmploymentType = e.EmploymentType,
                EmploymentStatus = e.EmploymentStatus,

                DepartmentId = e.DepartmentId,
                DepartmentName = e.Department.Name,

                CurrentPositionId = e.PositionHistories
                    .Where(h => h.EndDate == null)
                    .OrderByDescending(h => h.StartDate)
                    .Select(h => (int?)h.PositionId)
                    .FirstOrDefault(),

                CurrentPositionTitle = e.PositionHistories
                    .Where(h => h.EndDate == null)
                    .OrderByDescending(h => h.StartDate)
                    .Select(h => h.Position.Title)
                    .FirstOrDefault(),

                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            page,
            pageSize,
            totalCount,
            totalPages = (int)Math.Ceiling(
                totalCount / (double)pageSize),
            employees
        });
    }

    // GET: api/employees/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetEmployee(int id)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new EmployeeResponse
            {
                Id = e.Id,
                EmployeeNumber = e.EmployeeNumber,

                FirstName = e.FirstName,
                MiddleName = e.MiddleName,
                LastName = e.LastName,
                FullName = e.FirstName +
                    (e.MiddleName == null ? " " : " " + e.MiddleName + " ") +
                    e.LastName,

                NationalId = e.NationalId,
                Gender = e.Gender,
                DateOfBirth = e.DateOfBirth,

                Email = e.Email,
                PhoneNumber = e.PhoneNumber,
                ResidentialAddress = e.ResidentialAddress,

                EmergencyContactName = e.EmergencyContactName,
                EmergencyContactPhone = e.EmergencyContactPhone,
                EmergencyContactRelationship = e.EmergencyContactRelationship,

                DateOfEmployment = e.DateOfEmployment,
                EmploymentType = e.EmploymentType,
                EmploymentStatus = e.EmploymentStatus,

                DepartmentId = e.DepartmentId,
                DepartmentName = e.Department.Name,

                CurrentPositionId = e.PositionHistories
                    .Where(h => h.EndDate == null)
                    .OrderByDescending(h => h.StartDate)
                    .Select(h => (int?)h.PositionId)
                    .FirstOrDefault(),

                CurrentPositionTitle = e.PositionHistories
                    .Where(h => h.EndDate == null)
                    .OrderByDescending(h => h.StartDate)
                    .Select(h => h.Position.Title)
                    .FirstOrDefault(),

                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (employee == null)
        {
            return NotFound(new
            {
                message = $"Employee with ID {id} was not found."
            });
        }

        return Ok(employee);
    }

    // POST: api/employees
    [HttpPost]
    public async Task<IActionResult> CreateEmployee(
        CreateEmployeeRequest request)
    {
        var firstName = request.FirstName.Trim();
        var lastName = request.LastName.Trim();

        if (request.DateOfEmployment == DateOnly.MinValue)
        {
            return BadRequest(new
            {
                message = "A valid date of employment is required."
            });
        }

        if (request.DateOfEmployment > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return BadRequest(new
            {
                message = "Date of employment cannot be in the future."
            });
        }

        if (request.DateOfBirth.HasValue &&
            request.DateOfBirth.Value >= request.DateOfEmployment)
        {
            return BadRequest(new
            {
                message = "Date of birth must be earlier than the date of employment."
            });
        }

        if (request.Gender.HasValue &&
            !Enum.IsDefined(request.Gender.Value))
        {
            return BadRequest(new
            {
                message = "Invalid gender value."
            });
        }

        if (!Enum.IsDefined(request.EmploymentType))
        {
            return BadRequest(new
            {
                message = "Invalid employment type."
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
                message = "Employees cannot be registered under an inactive department."
            });
        }

        var nationalId = string.IsNullOrWhiteSpace(request.NationalId)
            ? null
            : request.NationalId.Trim();

        if (nationalId != null)
        {
            var nationalIdExists = await _context.Employees
                .AnyAsync(e => e.NationalId == nationalId);

            if (nationalIdExists)
            {
                return Conflict(new
                {
                    message = "An employee with this National ID already exists."
                });
            }
        }

        // Generate the final employee number after the database
        // assigns the employee's unique identity ID.
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        var employee = new Employee
        {
            EmployeeNumber = $"TMP-{Guid.NewGuid():N}"[..24],

            FirstName = firstName,
            MiddleName = string.IsNullOrWhiteSpace(request.MiddleName)
                ? null
                : request.MiddleName.Trim(),
            LastName = lastName,

            NationalId = nationalId,
            Gender = request.Gender,
            DateOfBirth = request.DateOfBirth,

            Email = string.IsNullOrWhiteSpace(request.Email)
                ? null
                : request.Email.Trim(),

            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber)
                ? null
                : request.PhoneNumber.Trim(),

            ResidentialAddress = string.IsNullOrWhiteSpace(request.ResidentialAddress)
                ? null
                : request.ResidentialAddress.Trim(),

            EmergencyContactName = string.IsNullOrWhiteSpace(request.EmergencyContactName)
                ? null
                : request.EmergencyContactName.Trim(),

            EmergencyContactPhone = string.IsNullOrWhiteSpace(request.EmergencyContactPhone)
                ? null
                : request.EmergencyContactPhone.Trim(),

            EmergencyContactRelationship = string.IsNullOrWhiteSpace(request.EmergencyContactRelationship)
                ? null
                : request.EmergencyContactRelationship.Trim(),

            DateOfEmployment = request.DateOfEmployment,
            EmploymentType = request.EmploymentType,
            EmploymentStatus = EmploymentStatus.Active,

            DepartmentId = department.Id,
            CreatedAt = DateTime.UtcNow
        };

        _context.Employees.Add(employee);

        await _context.SaveChangesAsync();

        employee.EmployeeNumber = $"CLAMS-EMP-{employee.Id:D4}";

        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

        var response = await _context.Employees
            .AsNoTracking()
            .Where(e => e.Id == employee.Id)
            .Select(e => new EmployeeResponse
            {
                Id = e.Id,
                EmployeeNumber = e.EmployeeNumber,

                FirstName = e.FirstName,
                MiddleName = e.MiddleName,
                LastName = e.LastName,
                FullName = e.FirstName +
                    (e.MiddleName == null ? " " : " " + e.MiddleName + " ") +
                    e.LastName,

                NationalId = e.NationalId,
                Gender = e.Gender,
                DateOfBirth = e.DateOfBirth,

                Email = e.Email,
                PhoneNumber = e.PhoneNumber,
                ResidentialAddress = e.ResidentialAddress,

                EmergencyContactName = e.EmergencyContactName,
                EmergencyContactPhone = e.EmergencyContactPhone,
                EmergencyContactRelationship = e.EmergencyContactRelationship,

                DateOfEmployment = e.DateOfEmployment,
                EmploymentType = e.EmploymentType,
                EmploymentStatus = e.EmploymentStatus,

                DepartmentId = e.DepartmentId,
                DepartmentName = e.Department.Name,

                CurrentPositionId = null,
                CurrentPositionTitle = null,

                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            })
            .FirstAsync();

        return CreatedAtAction(
            nameof(GetEmployee),
            new { id = employee.Id },
            response);
    }

    // PUT: api/employees/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateEmployee(
        int id,
        UpdateEmployeeRequest request)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
        {
            return NotFound(new
            {
                message = $"Employee with ID {id} was not found."
            });
        }

        if (request.DateOfEmployment == DateOnly.MinValue)
        {
            return BadRequest(new
            {
                message = "A valid date of employment is required."
            });
        }

        if (request.DateOfEmployment > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return BadRequest(new
            {
                message = "Date of employment cannot be in the future."
            });
        }

        if (request.DateOfBirth.HasValue &&
            request.DateOfBirth.Value >= request.DateOfEmployment)
        {
            return BadRequest(new
            {
                message = "Date of birth must be earlier than the date of employment."
            });
        }

        if (request.Gender.HasValue &&
            !Enum.IsDefined(request.Gender.Value))
        {
            return BadRequest(new
            {
                message = "Invalid gender value."
            });
        }

        if (!Enum.IsDefined(request.EmploymentType))
        {
            return BadRequest(new
            {
                message = "Invalid employment type."
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
                message = "Employees cannot be assigned to an inactive department."
            });
        }

        var nationalId = string.IsNullOrWhiteSpace(request.NationalId)
            ? null
            : request.NationalId.Trim();

        if (nationalId != null)
        {
            var nationalIdExists = await _context.Employees
                .AnyAsync(e =>
                    e.NationalId == nationalId &&
                    e.Id != id);

            if (nationalIdExists)
            {
                return Conflict(new
                {
                    message = "This National ID is already assigned to another employee."
                });
            }
        }

        // Prevent changing an employee's department while their
        // current position belongs to a different department.
        var currentPositionDepartment = await _context.EmployeePositionHistories
            .Where(h => h.EmployeeId == id && h.EndDate == null)
            .Select(h => (int?)h.Position.DepartmentId)
            .FirstOrDefaultAsync();

        if (currentPositionDepartment.HasValue &&
            currentPositionDepartment.Value != request.DepartmentId)
        {
            return BadRequest(new
            {
                message = "The employee's department cannot be changed while they have a current position in another department. Update the position assignment first."
            });
        }

        employee.FirstName = request.FirstName.Trim();

        employee.MiddleName = string.IsNullOrWhiteSpace(request.MiddleName)
            ? null
            : request.MiddleName.Trim();

        employee.LastName = request.LastName.Trim();

        employee.NationalId = nationalId;
        employee.Gender = request.Gender;
        employee.DateOfBirth = request.DateOfBirth;

        employee.Email = string.IsNullOrWhiteSpace(request.Email)
            ? null
            : request.Email.Trim();

        employee.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber)
            ? null
            : request.PhoneNumber.Trim();

        employee.ResidentialAddress = string.IsNullOrWhiteSpace(request.ResidentialAddress)
            ? null
            : request.ResidentialAddress.Trim();

        employee.EmergencyContactName = string.IsNullOrWhiteSpace(request.EmergencyContactName)
            ? null
            : request.EmergencyContactName.Trim();

        employee.EmergencyContactPhone = string.IsNullOrWhiteSpace(request.EmergencyContactPhone)
            ? null
            : request.EmergencyContactPhone.Trim();

        employee.EmergencyContactRelationship = string.IsNullOrWhiteSpace(request.EmergencyContactRelationship)
            ? null
            : request.EmergencyContactRelationship.Trim();

        employee.DateOfEmployment = request.DateOfEmployment;
        employee.EmploymentType = request.EmploymentType;
        employee.DepartmentId = department.Id;
        employee.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var response = await _context.Employees
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new EmployeeResponse
            {
                Id = e.Id,
                EmployeeNumber = e.EmployeeNumber,

                FirstName = e.FirstName,
                MiddleName = e.MiddleName,
                LastName = e.LastName,
                FullName = e.FirstName +
                    (e.MiddleName == null ? " " : " " + e.MiddleName + " ") +
                    e.LastName,

                NationalId = e.NationalId,
                Gender = e.Gender,
                DateOfBirth = e.DateOfBirth,

                Email = e.Email,
                PhoneNumber = e.PhoneNumber,
                ResidentialAddress = e.ResidentialAddress,

                EmergencyContactName = e.EmergencyContactName,
                EmergencyContactPhone = e.EmergencyContactPhone,
                EmergencyContactRelationship = e.EmergencyContactRelationship,

                DateOfEmployment = e.DateOfEmployment,
                EmploymentType = e.EmploymentType,
                EmploymentStatus = e.EmploymentStatus,

                DepartmentId = e.DepartmentId,
                DepartmentName = e.Department.Name,

                CurrentPositionId = e.PositionHistories
                    .Where(h => h.EndDate == null)
                    .OrderByDescending(h => h.StartDate)
                    .Select(h => (int?)h.PositionId)
                    .FirstOrDefault(),

                CurrentPositionTitle = e.PositionHistories
                    .Where(h => h.EndDate == null)
                    .OrderByDescending(h => h.StartDate)
                    .Select(h => h.Position.Title)
                    .FirstOrDefault(),

                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            })
            .FirstAsync();

        return Ok(response);
    }

    // PATCH: api/employees/5/suspend
    [HttpPatch("{id:int}/suspend")]
    public async Task<IActionResult> SuspendEmployee(int id)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
        {
            return NotFound(new
            {
                message = $"Employee with ID {id} was not found."
            });
        }

        if (employee.EmploymentStatus == EmploymentStatus.Separated)
        {
            return BadRequest(new
            {
                message = "A separated employee cannot be suspended."
            });
        }

        if (employee.EmploymentStatus == EmploymentStatus.Suspended)
        {
            return BadRequest(new
            {
                message = "This employee is already suspended."
            });
        }

        employee.EmploymentStatus = EmploymentStatus.Suspended;
        employee.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Employee suspended successfully.",
            employeeId = employee.Id,
            employeeNumber = employee.EmployeeNumber,
            employeeName = employee.FirstName + " " + employee.LastName,
            employmentStatus = employee.EmploymentStatus.ToString()
        });
    }

    // PATCH: api/employees/5/reactivate
    [HttpPatch("{id:int}/reactivate")]
    public async Task<IActionResult> ReactivateEmployee(int id)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
        {
            return NotFound(new
            {
                message = $"Employee with ID {id} was not found."
            });
        }

        if (employee.EmploymentStatus == EmploymentStatus.Separated)
        {
            return BadRequest(new
            {
                message = "A separated employee cannot be reactivated through this endpoint."
            });
        }

        if (employee.EmploymentStatus == EmploymentStatus.Active)
        {
            return BadRequest(new
            {
                message = "This employee is already active."
            });
        }

        employee.EmploymentStatus = EmploymentStatus.Active;
        employee.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Employee reactivated successfully.",
            employeeId = employee.Id,
            employeeNumber = employee.EmployeeNumber,
            employeeName = employee.FirstName + " " + employee.LastName,
            employmentStatus = employee.EmploymentStatus.ToString()
        });
    }
}
