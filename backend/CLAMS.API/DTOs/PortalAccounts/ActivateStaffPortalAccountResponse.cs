
namespace CLAMS.API.DTOs.PortalAccounts;

public class ActivateStaffPortalAccountResponse
{
    public bool IsSuccessful { get; set; }

    public string PortalUsername { get; set; } = string.Empty;

    public string EmployeeName { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
