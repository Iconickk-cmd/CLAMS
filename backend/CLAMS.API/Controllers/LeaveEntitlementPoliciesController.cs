
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
public class LeaveEntitlementPoliciesController : ControllerBase
{
    private readonly CLAMSHRDbContext _context;

    public LeaveEntitlementPoliciesController(CLAMSHRDbContext context)
    {
        _context = context;
    }

    // GET: api/LeaveEntitlementPolicies
    [HttpGet]
    public async Task<IActionResult> GetPolicies(
        [FromQuery] int? leaveTypeId = null,
        [FromQuery] string? employmentType = null,
        [FromQuery] int? employeeId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null)
    {
        var query = _context.LeaveEntitlementPolicies
            .AsNoTracking()
            .Include(p => p.LeaveType)
            .Include(p => p.Employee)
            .AsQueryable();

        if (leaveTypeId.HasValue)
            query = query.Where(p => p.LeaveTypeId == leaveTypeId.Value);

        if (employeeId.HasValue)
            query = query.Where(p => p.EmployeeId == employeeId.Value);

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(employmentType))
        {
            if (!Enum.TryParse<EmploymentType>(
                    employmentType.Trim(),
                    true,
                    out var parsedType) ||
                !Enum.IsDefined(typeof(EmploymentType), parsedType))
            {
                return BadRequest(new
                {
                    message = "Invalid employment type."
                });
            }

            query = query.Where(p => p.EmploymentType == parsedType);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();

            query = query.Where(p =>
                p.LeaveType.Name.ToLower().Contains(term) ||
                p.LeaveType.Code.ToLower().Contains(term) ||
                (p.Employee != null &&
                 (p.Employee.EmployeeNumber.ToLower().Contains(term) ||
                  p.Employee.FirstName.ToLower().Contains(term) ||
                  p.Employee.LastName.ToLower().Contains(term))));
        }

        var policies = await query
            .OrderBy(p => p.LeaveType.Name)
            .ThenBy(p => p.EmploymentType)
            .ThenBy(p => p.EmployeeId)
            .ThenByDescending(p => p.EffectiveFrom)
            .Select(p => new
            {
                p.Id,

                p.LeaveTypeId,
                LeaveTypeCode = p.LeaveType.Code,
                LeaveTypeName = p.LeaveType.Name,

                EmploymentType = p.EmploymentType.HasValue
                    ? p.EmploymentType.Value.ToString()
                    : null,

                p.EmployeeId,

                EmployeeNumber = p.Employee != null
                    ? p.Employee.EmployeeNumber
                    : null,

                EmployeeName = p.Employee != null
                    ? p.Employee.FirstName + " " + p.Employee.LastName
                    : null,

                p.AnnualEntitlementDays,
                p.HalfPayEntitlementDays,
                p.AllocationRule,
                p.AccruesMonthly,
                p.AllowCarryForward,
                p.MaxCarryForwardDays,
                p.CarryForwardExpiryMonths,
                p.EffectiveFrom,
                p.EffectiveTo,
                p.IsActive,
                p.CreatedAt,
                p.UpdatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            totalCount = policies.Count,
            policies
        });
    }

    // GET: api/LeaveEntitlementPolicies/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetPolicy(int id)
    {
        var policy = await _context.LeaveEntitlementPolicies
            .AsNoTracking()
            .Include(p => p.LeaveType)
            .Include(p => p.Employee)
            .Where(p => p.Id == id)
            .Select(p => new
            {
                p.Id,

                p.LeaveTypeId,
                LeaveTypeCode = p.LeaveType.Code,
                LeaveTypeName = p.LeaveType.Name,

                EmploymentType = p.EmploymentType.HasValue
                    ? p.EmploymentType.Value.ToString()
                    : null,

                p.EmployeeId,

                EmployeeNumber = p.Employee != null
                    ? p.Employee.EmployeeNumber
                    : null,

                EmployeeName = p.Employee != null
                    ? p.Employee.FirstName + " " + p.Employee.LastName
                    : null,

                p.AnnualEntitlementDays,
                p.HalfPayEntitlementDays,
                p.AllocationRule,
                p.AccruesMonthly,
                p.AllowCarryForward,
                p.MaxCarryForwardDays,
                p.CarryForwardExpiryMonths,
                p.EffectiveFrom,
                p.EffectiveTo,
                p.IsActive,
                p.CreatedAt,
                p.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (policy == null)
        {
            return NotFound(new
            {
                message = "Leave entitlement policy not found."
            });
        }

        return Ok(policy);
    }

    // POST: api/LeaveEntitlementPolicies
    [HttpPost]
    [Authorize(Roles = "HRManager,HROfficer,SystemAdministrator")]
    public async Task<IActionResult> CreatePolicy(
        [FromBody] LeaveEntitlementPolicyRequest request)
    {
        var validationError = await ValidateRequest(request);

        if (validationError != null)
            return BadRequest(new { message = validationError });

        var conflict = await HasOverlappingPolicy(request);

        if (conflict)
        {
            return Conflict(new
            {
                message = "An active policy already exists for this leave type and target during the specified effective dates."
            });
        }

        var policy = new LeaveEntitlementPolicy
        {
            LeaveTypeId = request.LeaveTypeId,
            EmploymentType = request.EmploymentType,
            EmployeeId = request.EmployeeId,

            AnnualEntitlementDays = request.AnnualEntitlementDays,
            HalfPayEntitlementDays = request.HalfPayEntitlementDays,
            AllocationRule = request.AllocationRule,
            AccruesMonthly = request.AccruesMonthly,

            AllowCarryForward = request.AllowCarryForward,
            MaxCarryForwardDays = request.MaxCarryForwardDays,
            CarryForwardExpiryMonths = request.CarryForwardExpiryMonths,

            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,

            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.LeaveEntitlementPolicies.Add(policy);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetPolicy),
            new { id = policy.Id },
            new
            {
                message = "Leave entitlement policy created successfully.",
                policyId = policy.Id
            });
    }

    // PUT: api/LeaveEntitlementPolicies/5
    [HttpPut("{id:int}")]
    [Authorize(Roles = "HRManager,HROfficer,SystemAdministrator")]
    public async Task<IActionResult> UpdatePolicy(
        int id,
        [FromBody] LeaveEntitlementPolicyRequest request)
    {
        var policy = await _context.LeaveEntitlementPolicies
            .FirstOrDefaultAsync(p => p.Id == id);

        if (policy == null)
        {
            return NotFound(new
            {
                message = "Leave entitlement policy not found."
            });
        }

        var validationError = await ValidateRequest(request);

        if (validationError != null)
            return BadRequest(new { message = validationError });

        var conflict = await HasOverlappingPolicy(request, id);

        if (policy.IsActive && conflict)
        {
            return Conflict(new
            {
                message = "Another active policy already exists for this leave type and target during the specified effective dates."
            });
        }

        policy.LeaveTypeId = request.LeaveTypeId;
        policy.EmploymentType = request.EmploymentType;
        policy.EmployeeId = request.EmployeeId;

        policy.AnnualEntitlementDays = request.AnnualEntitlementDays;
        policy.HalfPayEntitlementDays = request.HalfPayEntitlementDays;
        policy.AllocationRule = request.AllocationRule;
        policy.AccruesMonthly = request.AccruesMonthly;

        policy.AllowCarryForward = request.AllowCarryForward;
        policy.MaxCarryForwardDays = request.MaxCarryForwardDays;
        policy.CarryForwardExpiryMonths = request.CarryForwardExpiryMonths;

        policy.EffectiveFrom = request.EffectiveFrom;
        policy.EffectiveTo = request.EffectiveTo;

        policy.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Leave entitlement policy updated successfully.",
            policyId = policy.Id
        });
    }

    // PATCH: api/LeaveEntitlementPolicies/5/deactivate
    [HttpPatch("{id:int}/deactivate")]
    [Authorize(Roles = "HRManager,HROfficer,SystemAdministrator")]
    public async Task<IActionResult> DeactivatePolicy(int id)
    {
        var policy = await _context.LeaveEntitlementPolicies
            .FirstOrDefaultAsync(p => p.Id == id);

        if (policy == null)
        {
            return NotFound(new
            {
                message = "Leave entitlement policy not found."
            });
        }

        if (!policy.IsActive)
        {
            return BadRequest(new
            {
                message = "This policy is already inactive."
            });
        }

        policy.IsActive = false;
        policy.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Leave entitlement policy deactivated successfully."
        });
    }

    // PATCH: api/LeaveEntitlementPolicies/5/activate
    [HttpPatch("{id:int}/activate")]
    [Authorize(Roles = "HRManager,HROfficer,SystemAdministrator")]
    public async Task<IActionResult> ActivatePolicy(int id)
    {
        var policy = await _context.LeaveEntitlementPolicies
            .FirstOrDefaultAsync(p => p.Id == id);

        if (policy == null)
        {
            return NotFound(new
            {
                message = "Leave entitlement policy not found."
            });
        }

        if (policy.IsActive)
        {
            return BadRequest(new
            {
                message = "This policy is already active."
            });
        }

        var request = new LeaveEntitlementPolicyRequest
        {
            LeaveTypeId = policy.LeaveTypeId,
            EmploymentType = policy.EmploymentType,
            EmployeeId = policy.EmployeeId,
            AnnualEntitlementDays = policy.AnnualEntitlementDays,
            HalfPayEntitlementDays = policy.HalfPayEntitlementDays,
            AllocationRule = policy.AllocationRule,
            AccruesMonthly = policy.AccruesMonthly,
            AllowCarryForward = policy.AllowCarryForward,
            MaxCarryForwardDays = policy.MaxCarryForwardDays,
            CarryForwardExpiryMonths = policy.CarryForwardExpiryMonths,
            EffectiveFrom = policy.EffectiveFrom,
            EffectiveTo = policy.EffectiveTo
        };

        var conflict = await HasOverlappingPolicy(request, id);

        if (conflict)
        {
            return Conflict(new
            {
                message = "Another active policy overlaps this policy's effective dates. Resolve the conflict before activating it."
            });
        }

        policy.IsActive = true;
        policy.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Leave entitlement policy activated successfully."
        });
    }

    // VALIDATION

    private async Task<string?> ValidateRequest(
        LeaveEntitlementPolicyRequest request)
    {
        if (request.LeaveTypeId <= 0)
            return "A valid leave type is required.";

        var leaveType = await _context.LeaveTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == request.LeaveTypeId);

        if (leaveType == null)
            return "The selected leave type does not exist.";

        if (!leaveType.IsActive)
            return "An inactive leave type cannot be used for a new policy.";

        if (!leaveType.IsPaid)
            return "Entitlement policies can only be configured for paid leave types.";

        if (request.EmploymentType.HasValue &&
            !Enum.IsDefined(
                typeof(EmploymentType),
                request.EmploymentType.Value))
        {
            return "Invalid employment type.";
        }

        // Exactly one target must be selected.
        bool hasEmploymentType = request.EmploymentType.HasValue;
        bool hasEmployee = request.EmployeeId.HasValue;

        if (hasEmploymentType == hasEmployee)
        {
            return "Select either an employment type or an individual employee, but not both.";
        }

        if (request.EmployeeId.HasValue)
        {
            if (request.EmployeeId.Value <= 0)
                return "Invalid employee selection.";

            var employeeExists = await _context.Employees
                .AnyAsync(e => e.Id == request.EmployeeId.Value);

            if (!employeeExists)
                return "The selected employee does not exist.";
        }

        if (!Enum.IsDefined(typeof(LeaveEntitlementAllocationRule), request.AllocationRule))
            return "Invalid leave entitlement allocation rule."; 

        if (request.AnnualEntitlementDays < 0)
            return "Annual entitlement days cannot be negative.";

        if (request.AnnualEntitlementDays > 366)
            return "Annual entitlement cannot exceed 366 days.";

        if (request.HalfPayEntitlementDays < 0)
            return "Half-pay entitlement days cannot be negative."; 

        if (request.HalfPayEntitlementDays > 366)
            return "Half-pay entitlement cannot exceed 366 days."; 

        if (request.HalfPayEntitlementDays > 0 &&
            !string.Equals(
                leaveType.Code,
                "SL",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Half-pay entitlement is only permitted for Sick Leave."; 
        }

        bool isAnnualLeave = string.Equals(
            leaveType.Code,
            "AL",
            StringComparison.OrdinalIgnoreCase);

        if (request.AccruesMonthly != isAnnualLeave)
        {
            return "Monthly accrual must be enabled for Annual Leave and disabled for all other leave types."; 
        }

        if (request.MaxCarryForwardDays < 0)
            return "Maximum carry-forward days cannot be negative.";

        if (request.CarryForwardExpiryMonths <= 0)
            return "Carry-forward expiry must be greater than zero months.";

        if (request.CarryForwardExpiryMonths > 120)
            return "Carry-forward expiry cannot exceed 120 months.";

        if (request.EffectiveTo.HasValue &&
            request.EffectiveTo.Value < request.EffectiveFrom)
        {
            return "The effective end date cannot be earlier than the effective start date.";
        }

        if (request.AllowCarryForward &&
            !string.Equals(
                leaveType.Code,
                "AL",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Carry-forward is only permitted for Annual Leave.";
        }

        if (request.AllowCarryForward &&
            request.MaxCarryForwardDays <= 0)
        {
            return "Maximum carry-forward days must be greater than zero when carry-forward is enabled.";
        }

        return null;
    }

    // PREVENT OVERLAPPING ACTIVE POLICIES

    private async Task<bool> HasOverlappingPolicy(
        LeaveEntitlementPolicyRequest request,
        int? excludedPolicyId = null)
    {
        var proposedEnd = request.EffectiveTo ?? DateOnly.MaxValue;

        var query = _context.LeaveEntitlementPolicies
            .Where(p =>
                p.IsActive &&
                p.LeaveTypeId == request.LeaveTypeId &&
                p.EmployeeId == request.EmployeeId &&
                p.EmploymentType == request.EmploymentType &&
                p.EffectiveFrom <= proposedEnd &&
                (!p.EffectiveTo.HasValue ||
                 p.EffectiveTo.Value >= request.EffectiveFrom));

        if (excludedPolicyId.HasValue)
        {
            query = query.Where(p => p.Id != excludedPolicyId.Value);
        }

        return await query.AnyAsync();
    }
}

// REQUEST MODEL

public class LeaveEntitlementPolicyRequest
{
    public int LeaveTypeId { get; set; }

    public EmploymentType? EmploymentType { get; set; }

    public int? EmployeeId { get; set; }

    public decimal AnnualEntitlementDays { get; set; }
    public decimal HalfPayEntitlementDays { get; set; }
    public LeaveEntitlementAllocationRule AllocationRule { get; set; } = LeaveEntitlementAllocationRule.FullPayOnly;

    public bool AccruesMonthly { get; set; } = true;

    public bool AllowCarryForward { get; set; }

    public decimal MaxCarryForwardDays { get; set; } = 10m;

    public int CarryForwardExpiryMonths { get; set; } = 18;

    public DateOnly EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }
}









