namespace CLAMS.Domain.Entities;

public class LeaveType
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsPaid { get; set; } = true;

    public bool RequiresApproval { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<LeaveEntitlementPolicy> EntitlementPolicies { get; set; }
        = new List<LeaveEntitlementPolicy>();

    public ICollection<EmployeeLeaveBalance> EmployeeBalances { get; set; }
        = new List<EmployeeLeaveBalance>();

    public ICollection<LeaveRequest> LeaveRequests { get; set; }
        = new List<LeaveRequest>();
}