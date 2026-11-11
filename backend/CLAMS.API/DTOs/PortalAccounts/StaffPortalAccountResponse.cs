
namespace CLAMS.API.DTOs.PortalAccounts;

public class StaffPortalAccountResponse
{
    public int UserId { get; set; }

    public int EmployeeId { get; set; }

    public string EmployeeNumber { get; set; } = string.Empty;

    public string EmployeeName { get; set; } = string.Empty;

    public string PortalUsername { get; set; } = string.Empty;

    public string TemporaryPassword { get; set; } = string.Empty;

    public string ActivationUrl { get; set; } = string.Empty;

    public DateTime ActivationExpiresAt { get; set; }

    public string Message { get; set; } = string.Empty;
}
