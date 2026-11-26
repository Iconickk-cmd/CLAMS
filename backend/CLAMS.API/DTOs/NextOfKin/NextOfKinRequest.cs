using System.ComponentModel.DataAnnotations;

namespace CLAMS.API.DTOs.NextOfKin;

public class NextOfKinRequest
{
    [Required]
    [StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Relationship { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;

    [StringLength(30)]
    public string? AlternativePhoneNumber { get; set; }

    [EmailAddress]
    [StringLength(150)]
    public string? Email { get; set; }

    [StringLength(500)]
    public string? PhysicalAddress { get; set; }
}
