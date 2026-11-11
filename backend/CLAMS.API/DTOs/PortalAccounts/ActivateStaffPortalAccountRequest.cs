
using System.ComponentModel.DataAnnotations;

namespace CLAMS.API.DTOs.PortalAccounts;

public class ActivateStaffPortalAccountRequest
{
    [Required]
    [RegularExpression(
        @"^[0-9]+$",
        ErrorMessage = "Portal username must contain numbers only.")]
    public string PortalUsername { get; set; } = string.Empty;

    [Required]
    public string ActivationToken { get; set; } = string.Empty;

    [Required]
    public string TemporaryPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [Compare(
        nameof(NewPassword),
        ErrorMessage = "The passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
