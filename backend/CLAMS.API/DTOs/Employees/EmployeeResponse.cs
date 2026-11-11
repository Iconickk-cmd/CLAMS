
using CLAMS.Domain.Enums;

namespace CLAMS.API.DTOs.Employees;

public class EmployeeResponse
{
    public int Id { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

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
    public EmploymentType EmploymentType { get; set; }
    public EmploymentStatus EmploymentStatus { get; set; }

    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;

    public int? CurrentPositionId { get; set; }
    public string? CurrentPositionTitle { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
