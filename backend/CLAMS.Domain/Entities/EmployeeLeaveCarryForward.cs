
namespace CLAMS.Domain.Entities;

public class EmployeeLeaveCarryForward
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public int LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;

    public int SourceLeaveYear { get; set; }
    public int TargetLeaveYear { get; set; }

    public DateOnly AvailableFrom { get; set; }
    public DateOnly ExpiresOn { get; set; }

    public decimal OriginalDays { get; set; }
    public decimal RemainingDays { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<LeaveCarryForwardTransaction> Transactions { get; set; }
        = new List<LeaveCarryForwardTransaction>();

    public ICollection<LeaveRequestAllocation> Allocations { get; set; }
        = new List<LeaveRequestAllocation>();
}
