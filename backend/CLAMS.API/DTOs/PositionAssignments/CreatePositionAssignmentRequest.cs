
using System.ComponentModel.DataAnnotations;

namespace CLAMS.API.DTOs.PositionAssignments;

public class CreatePositionAssignmentRequest
{
    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int PositionId { get; set; }

    [Required]
    public DateOnly StartDate { get; set; }

    [StringLength(500)]
    public string? ChangeReason { get; set; }
}
