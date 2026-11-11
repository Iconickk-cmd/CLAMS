
using System.ComponentModel.DataAnnotations;

namespace CLAMS.API.DTOs.PositionAssignments;

public class EndPositionAssignmentRequest
{
    [Required]
    public DateOnly EndDate { get; set; }

    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}
