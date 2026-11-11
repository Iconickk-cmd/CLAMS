
using CLAMS.Domain.Enums;

namespace CLAMS.Domain.Entities;

public class LeaveCarryForwardTransaction
{
    public int Id { get; set; }

    public int EmployeeLeaveCarryForwardId { get; set; }

    public EmployeeLeaveCarryForward EmployeeLeaveCarryForward
    { get; set; } = null!;

    public LeaveCarryForwardAction Action { get; set; }

    public int? LeaveRequestId { get; set; }

    public LeaveRequest? LeaveRequest { get; set; }

    public decimal Days { get; set; }

    public string? Remarks { get; set; }

    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
}
