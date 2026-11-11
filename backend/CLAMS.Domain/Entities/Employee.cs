using CLAMS.Domain.Enums;

namespace CLAMS.Domain.Entities;

public class Employee
{
    public int Id { get; set; }

    public string EmployeeNumber { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string? MiddleName { get; set; }

    public string LastName { get; set; } = string.Empty;

    public string? NationalId { get; set; }

    public Gender? Gender { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? ResidentialAddress { get; set; }

    public string? EmergencyContactName { get; set; }

    public string? EmergencyContactPhone { get; set; }

    public string? EmergencyContactRelationship { get; set; }

    public DateOnly DateOfEmployment { get; set; }

    public EmploymentType EmploymentType { get; set; } = EmploymentType.Permanent;

    public EmploymentStatus EmploymentStatus { get; set; } = EmploymentStatus.Active;

    public int DepartmentId { get; set; }

    public Department Department { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<EmployeePositionHistory> PositionHistories { get; set; }
        = new List<EmployeePositionHistory>();
}