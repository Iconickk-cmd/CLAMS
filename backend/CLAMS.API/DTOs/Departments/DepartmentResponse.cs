namespace CLAMS.API.DTOs.Departments;

public class DepartmentResponse
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int EmployeeCount { get; set; }

    public int PositionCount { get; set; }
}