using CLAMS.Domain.Enums;

namespace CLAMS.Domain.Entities;

public class EmployeePositionHistory
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    public int PositionId { get; set; }

    public Position Position { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public PositionAssignmentStatus Status { get; set; }
        = PositionAssignmentStatus.Current;

    public string? ChangeReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}