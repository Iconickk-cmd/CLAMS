using System.Data;
using CLAMS.Domain.Entities;
using CLAMS.Domain.Enums;
using CLAMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CLAMS.API.Services;

public sealed record LeaveApprovalResult(
    bool Success,
    string Message,
    decimal ApprovedDays = 0m);

public class LeaveAllocationService
{
    private readonly CLAMSHRDbContext _context;
    private readonly WorkingDayCalculator _workingDayCalculator;

    public LeaveAllocationService(
        CLAMSHRDbContext context,
        WorkingDayCalculator workingDayCalculator)
    {
        _context = context;
        _workingDayCalculator = workingDayCalculator;
    }

    public async Task<LeaveApprovalResult> ApproveAsync(
        int leaveRequestId,
        string actorName,
        string? remarks = null,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            var request = await _context.Set<LeaveRequest>()
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(
                    r => r.Id == leaveRequestId,
                    cancellationToken);

            if (request is null)
                return new(false, "Leave request was not found.");

            if (request.Status != LeaveRequestStatus.Pending)
                return new(false, "Only pending leave requests can be approved.");

            var workingDates = await _workingDayCalculator.GetWorkingDatesAsync(
                request.StartDate,
                request.EndDate,
                cancellationToken);

            if (workingDates.Count == 0)
                return new(false, "The requested period contains no working days.");

            var employee = request.Employee;

            var policies = await _context.Set<LeaveEntitlementPolicy>()
                .Where(p =>
                    p.LeaveTypeId == request.LeaveTypeId &&
                    p.IsActive &&
                    (p.EmployeeId == employee.Id ||
                     (p.EmployeeId == null &&
                      p.EmploymentType == employee.EmploymentType)))
                .ToListAsync(cancellationToken);

            var leaveYears = workingDates
                .Select(d => d.Year)
                .Distinct()
                .ToArray();

            var balances = await _context.Set<EmployeeLeaveBalance>()
                .Where(b =>
                    b.EmployeeId == employee.Id &&
                    b.LeaveTypeId == request.LeaveTypeId &&
                    leaveYears.Contains(b.LeaveYear))
                .ToListAsync(cancellationToken);

            var carryForwards = await _context.Set<EmployeeLeaveCarryForward>()
                .Where(c =>
                    c.EmployeeId == employee.Id &&
                    c.LeaveTypeId == request.LeaveTypeId &&
                    c.RemainingDays > 0 &&
                    leaveYears.Contains(c.TargetLeaveYear))
                .ToListAsync(cancellationToken);

            var balanceAllocations = new Dictionary<int, decimal>();
            var carryForwardAllocations = new Dictionary<int, decimal>();

            foreach (var leaveDate in workingDates)
            {
                var policy = policies
                    .Where(p =>
                        p.EffectiveFrom <= leaveDate &&
                        (!p.EffectiveTo.HasValue ||
                         p.EffectiveTo.Value >= leaveDate))
                    .OrderByDescending(p => p.EmployeeId == employee.Id)
                    .ThenByDescending(p => p.EffectiveFrom)
                    .ThenByDescending(p => p.Id)
                    .FirstOrDefault();

                if (policy is null)
                    return new(false,
                        $"No active entitlement policy applies on {leaveDate:yyyy-MM-dd}.");

                var categories = policy.AllocationRule switch
                {
                    LeaveEntitlementAllocationRule.FullPayOnly =>
                        new[] { PayCategory.FullPay },

                    LeaveEntitlementAllocationRule.HalfPayOnly =>
                        new[] { PayCategory.HalfPay },

                    LeaveEntitlementAllocationRule.FullPayThenHalfPay =>
                        new[] { PayCategory.FullPay, PayCategory.HalfPay },

                    _ => Array.Empty<PayCategory>()
                };

                if (categories.Length == 0)
                    return new(false, "The entitlement allocation rule is invalid.");

                decimal remainingForDate = 1m;

                foreach (var category in categories)
                {
                    if (remainingForDate <= 0)
                        break;

                    if (policy.AllowCarryForward)
                    {
                        var eligibleLots = carryForwards
                            .Where(c =>
                                c.PayCategory == category &&
                                c.TargetLeaveYear == leaveDate.Year &&
                                c.AvailableFrom <= leaveDate &&
                                c.ExpiresOn >= leaveDate &&
                                c.RemainingDays > 0)
                            .OrderBy(c => c.ExpiresOn)
                            .ThenBy(c => c.Id)
                            .ToList();

                        foreach (var lot in eligibleLots)
                        {
                            var used = Math.Min(
                                lot.RemainingDays,
                                remainingForDate);

                            if (used <= 0)
                                continue;

                            lot.RemainingDays -= used;

                            carryForwardAllocations[lot.Id] =
                                carryForwardAllocations.GetValueOrDefault(lot.Id) + used;

                            remainingForDate -= used;

                            if (remainingForDate <= 0)
                                break;
                        }
                    }

                    if (remainingForDate <= 0)
                        break;

                    var balance = balances.FirstOrDefault(b =>
                        b.LeaveYear == leaveDate.Year &&
                        b.PayCategory == category);

                    if (balance is null)
                        continue;

                    var available = balance.AccruedDays +
                                    balance.AdjustmentDays -
                                    balance.TakenDays;

                    if (available <= 0)
                        continue;

                    var balanceUsed = Math.Min(available, remainingForDate);

                    balance.TakenDays += balanceUsed;
                    balance.UpdatedAt = DateTime.UtcNow;

                    balanceAllocations[balance.Id] =
                        balanceAllocations.GetValueOrDefault(balance.Id) + balanceUsed;

                    remainingForDate -= balanceUsed;
                }

                if (remainingForDate > 0)
                {
                    return new(
                        false,
                        $"Insufficient leave balance for {leaveDate:yyyy-MM-dd}. " +
                        $"{remainingForDate:0.######} day(s) could not be allocated.");
                }
            }

            foreach (var allocation in balanceAllocations)
            {
                _context.Set<LeaveRequestAllocation>().Add(
                    new LeaveRequestAllocation
                    {
                        LeaveRequestId = request.Id,
                        EmployeeLeaveBalanceId = allocation.Key,
                        DaysAllocated = allocation.Value
                    });
            }

            foreach (var allocation in carryForwardAllocations)
            {
                _context.Set<LeaveRequestAllocation>().Add(
                    new LeaveRequestAllocation
                    {
                        LeaveRequestId = request.Id,
                        EmployeeLeaveCarryForwardId = allocation.Key,
                        DaysAllocated = allocation.Value
                    });

                _context.Set<LeaveCarryForwardTransaction>().Add(
                    new LeaveCarryForwardTransaction
                    {
                        EmployeeLeaveCarryForwardId = allocation.Key,
                        Action = LeaveCarryForwardAction.Used,
                        LeaveRequestId = request.Id,
                        Days = allocation.Value,
                        Remarks = $"Allocated to approved leave request #{request.Id}."
                    });
            }

            decimal approvedDays = workingDates.Count;

            request.ApprovedDays = approvedDays;
            request.Status = LeaveRequestStatus.Approved;
            request.DecisionAt = DateTime.UtcNow;
            request.DecisionBy = actorName;
            request.DecisionRemarks = remarks;

            _context.Set<LeaveApprovalHistory>().Add(
                new LeaveApprovalHistory
                {
                    LeaveRequestId = request.Id,
                    Action = LeaveApprovalAction.Approved,
                    ActorName = actorName,
                    Comments = remarks
                });

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new(
                true,
                "Leave request approved and leave balances allocated.",
                approvedDays);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new(
                false,
                "A leave balance was changed by another approval. Please retry.");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
