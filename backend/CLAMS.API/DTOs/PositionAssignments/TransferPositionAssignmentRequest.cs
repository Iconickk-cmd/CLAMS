
using System.ComponentModel.DataAnnotations;

namespace CLAMS.API.DTOs.PositionAssignments;

public class TransferPositionAssignmentRequest
{
    [Required]
    public int NewPositionId { get; set; }

    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    [StringLength(500)]
    public string ChangeReason { get; set; } = string.Empty;
}
