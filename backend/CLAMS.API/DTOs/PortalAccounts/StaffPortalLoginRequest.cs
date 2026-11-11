using System.ComponentModel.DataAnnotations;

namespace CLAMS.API.DTOs.PortalAccounts;

public class StaffPortalLoginRequest
{
    [Required]
    [RegularExpression(
        @"^[0-9]+$",
        ErrorMessage = "Portal username must contain numbers only.")]
    public string PortalUsername { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}