
namespace CLAMS.API.DTOs.PositionAssignments;

public class PositionAssignmentResponse
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;

    public int PositionId { get; set; }
    public string PositionCode { get; set; } = string.Empty;
    public string PositionTitle { get; set; } = string.Empty;

    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? ChangeReason { get; set; }

    public bool IsCurrent { get; set; }

    public DateTime CreatedAt { get; set; }
}
