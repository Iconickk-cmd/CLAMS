
namespace CLAMS.Domain.Entities;

public class LeaveRequestAllocation
{
    public int Id { get; set; }

    public int LeaveRequestId { get; set; }
    public LeaveRequest LeaveRequest { get; set; } = null!;

    // Exactly one of these two source IDs must be populated.
    public int? EmployeeLeaveBalanceId { get; set; }
    public EmployeeLeaveBalance? EmployeeLeaveBalance { get; set; }

    public int? EmployeeLeaveCarryForwardId { get; set; }
    public EmployeeLeaveCarryForward? EmployeeLeaveCarryForward { get; set; }

    public decimal DaysAllocated { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
