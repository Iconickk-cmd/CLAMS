
namespace CLAMS.Domain.Entities;

public class EmployeeLeaveBalance
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public int LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;

    public int LeaveYear { get; set; }

    public decimal AnnualEntitlementDays { get; set; }
    public decimal AccruedDays { get; set; }
    public decimal TakenDays { get; set; }
    public decimal AdjustmentDays { get; set; }

    public DateOnly? LastAccruedOn { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<LeaveAccrualTransaction> AccrualTransactions { get; set; }
        = new List<LeaveAccrualTransaction>();

    public ICollection<LeaveRequestAllocation> Allocations { get; set; }
        = new List<LeaveRequestAllocation>();
}
