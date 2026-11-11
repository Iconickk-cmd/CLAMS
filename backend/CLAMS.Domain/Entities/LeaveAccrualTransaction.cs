namespace CLAMS.Domain.Entities;

public class LeaveAccrualTransaction
{
    public int Id { get; set; }

    public int EmployeeLeaveBalanceId { get; set; }

    public EmployeeLeaveBalance EmployeeLeaveBalance { get; set; }
        = null!;

    public DateOnly AccrualDate { get; set; }

    public int CompletedServiceMonths { get; set; }

    public decimal DaysAccrued { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}