using System.ComponentModel.DataAnnotations;

namespace CLAMS.API.DTOs.Departments;

public class CreateDepartmentRequest
{
    [Required]
    [StringLength(30)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }
}