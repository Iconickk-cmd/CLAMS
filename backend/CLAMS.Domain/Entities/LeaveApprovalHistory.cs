using CLAMS.Domain.Enums;

namespace CLAMS.Domain.Entities;

public class LeaveApprovalHistory
{
    public int Id { get; set; }

    public int LeaveRequestId { get; set; }

    public LeaveRequest LeaveRequest { get; set; } = null!;

    public LeaveApprovalAction Action { get; set; }

    public string ActorName { get; set; } = string.Empty;

    public string? Comments { get; set; }

    public DateTime ActionAt { get; set; } = DateTime.UtcNow;
}