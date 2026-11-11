using System.ComponentModel.DataAnnotations;

namespace CLAMS.API.DTOs.Positions;

public class UpdatePositionRequest
{
    [Required]
    [StringLength(30)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int DepartmentId { get; set; }

    [StringLength(50)]
    public string? Grade { get; set; }

    [Range(1, 10000)]
    public int ApprovedHeadcount { get; set; } = 1;
}