namespace CLAMS.API.DTOs.NextOfKin;

public class NextOfKinResponse
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Relationship { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string? AlternativePhoneNumber { get; set; }

    public string? Email { get; set; }

    public string? PhysicalAddress { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
