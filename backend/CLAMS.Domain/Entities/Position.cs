namespace CLAMS.Domain.Entities;

public class Position
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int DepartmentId { get; set; }

    public Department Department { get; set; } = null!;

    public string? Grade { get; set; }

    public int ApprovedHeadcount { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<EmployeePositionHistory> EmployeePositionHistories { get; set; }
        = new List<EmployeePositionHistory>();
}