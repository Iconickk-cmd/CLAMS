namespace CLAMS.API.DTOs.Positions;

public class PositionResponse
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;

    public string? Grade { get; set; }
    public int ApprovedHeadcount { get; set; }

    public int OccupiedHeadcount { get; set; }
    public int VacantHeadcount { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}