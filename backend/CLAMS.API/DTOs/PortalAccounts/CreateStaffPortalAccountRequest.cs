
using System.ComponentModel.DataAnnotations;

namespace CLAMS.API.DTOs.PortalAccounts;

public class CreateStaffPortalAccountRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }

    // Optional: HR can enter an existing staff number.
    // If omitted, CLAMS will generate the next available number.
    [StringLength(20, MinimumLength = 1)]
    [RegularExpression(
        @"^[0-9]+$",
        ErrorMessage = "Portal username must contain numbers only.")]
    public string? PortalUsername { get; set; }
}
