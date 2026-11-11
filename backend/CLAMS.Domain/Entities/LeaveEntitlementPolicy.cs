
using CLAMS.Domain.Enums;

namespace CLAMS.Domain.Entities;

public class LeaveEntitlementPolicy
{
    public int Id { get; set; }

    public int LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;

    public EmploymentType? EmploymentType { get; set; }

    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public decimal AnnualEntitlementDays { get; set; }

    public bool AccruesMonthly { get; set; } = true;

    public bool AllowCarryForward { get; set; } = false;

    public decimal MaxCarryForwardDays { get; set; } = 10m;

    public int CarryForwardExpiryMonths { get; set; } = 18;

    public DateOnly EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
