
using CLAMS.Domain.Enums;

namespace CLAMS.Domain.Entities;

public class LeaveRequest
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public int LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public decimal RequestedDays { get; set; }
    public decimal ApprovedDays { get; set; }

    public string Reason { get; set; } = string.Empty;

    public LeaveRequestStatus Status { get; set; }
        = LeaveRequestStatus.Pending;

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public DateTime? DecisionAt { get; set; }
    public string? DecisionBy { get; set; }
    public string? DecisionRemarks { get; set; }

    public DateTime? CancelledAt { get; set; }

    public ICollection<LeaveApprovalHistory> ApprovalHistory { get; set; }
        = new List<LeaveApprovalHistory>();

    public ICollection<LeaveRequestAllocation> Allocations { get; set; }
        = new List<LeaveRequestAllocation>();
}
