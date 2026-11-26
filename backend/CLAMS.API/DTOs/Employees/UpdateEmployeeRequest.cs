
using System.ComponentModel.DataAnnotations;
using CLAMS.Domain.Enums;

namespace CLAMS.API.DTOs.Employees;

public class UpdateEmployeeRequest
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? MiddleName { get; set; }

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(30)]
    public string? NationalId { get; set; }

    public Gender? Gender { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    [EmailAddress]
    [StringLength(150)]
    public string? Email { get; set; }

    [Phone]
    [StringLength(30)]
    public string? PhoneNumber { get; set; }

    [StringLength(500)]
    public string? ResidentialAddress { get; set; }

    [StringLength(150)]
    public string? EmergencyContactName { get; set; }

    [Phone]
    [StringLength(30)]
    public string? EmergencyContactPhone { get; set; }

    [StringLength(100)]
    public string? EmergencyContactRelationship { get; set; }

    [Required]
    public DateOnly DateOfEmployment { get; set; }

    [Required]
    public EmploymentType EmploymentType { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int SupervisorEmployeeId { get; set; }

    public int DepartmentId { get; set; }
}
