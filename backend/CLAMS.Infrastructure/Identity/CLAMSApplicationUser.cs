using CLAMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace CLAMS.Infrastructure.Identity;

public class CLAMSApplicationUser : IdentityUser<int>
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public int? EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsPortalActivated { get; set; } = false;

    public bool MustChangePassword { get; set; } = true;

    public string? ActivationTokenHash { get; set; }

    public DateTime? ActivationTokenExpiresAt { get; set; }

    public DateTime? TemporaryPasswordExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}