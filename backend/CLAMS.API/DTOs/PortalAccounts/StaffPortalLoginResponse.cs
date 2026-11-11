namespace CLAMS.API.DTOs.PortalAccounts;

public class StaffPortalLoginResponse
{
    public bool IsSuccessful { get; set; }

    public string AccessToken { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public int UserId { get; set; }

    public int EmployeeId { get; set; }

    public string PortalUsername { get; set; } = string.Empty;

    public string EmployeeNumber { get; set; } = string.Empty;

    public string EmployeeName { get; set; } = string.Empty;

    public string[] Roles { get; set; } = [];

    public string Message { get; set; } = string.Empty;
}