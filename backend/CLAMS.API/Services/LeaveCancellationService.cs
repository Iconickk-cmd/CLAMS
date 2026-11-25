using System.Data;
using CLAMS.Domain.Entities;
using CLAMS.Domain.Enums;
using CLAMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CLAMS.API.Services;

public sealed record LeaveCancellationResult(
    bool Success,
    string Message);

public class LeaveCancellationService
{
    private readonly CLAMSHRDbContext _context;

    public LeaveCancellationService(CLAMSHRDbContext context)
    {
        _context = context;
    }

    public async Task<LeaveCancellationResult> CancelAsync(
        int leaveRequestId,
        string actorName,
        string? remarks = null,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        try
        {
            var request = await _context.Set<LeaveRequest>()
                .Include(r => r.Allocations)
                    .ThenInclude(a => a.EmployeeLeaveBalance)
                .Include(r => r.Allocations)
                    .ThenInclude(a => a.EmployeeLeaveCarryForward)
                .FirstOrDefaultAsync(
                    r => r.Id == leaveRequestId,
                    cancellationToken);

            if (request is null)
                return new(false, "Leave request was not found.");

            if (request.Status != LeaveRequestStatus.Pending &&
                request.Status != LeaveRequestStatus.Approved)
            {
                return new(false,
                    "Only pending or approved leave requests can be cancelled.");
            }

            var today = BusinessClock.Today;

            if (request.Status == LeaveRequestStatus.Approved &&
                request.StartDate <= today)
            {
                return new(false,
                    "Approved leave cannot be cancelled after its start date.");
            }

            if (request.Status == LeaveRequestStatus.Approved &&
                request.ApprovedDays > 0 &&
                request.Allocations.Count == 0)
            {
                return new(false,
                    "Allocation records are missing. Cancellation requires HR review.");
            }

            // Validate every allocation before modifying any balance.
            foreach (var allocation in request.Allocations)
            {
                var hasBalanceId =
                    allocation.EmployeeLeaveBalanceId.HasValue;

                var hasCarryForwardId =
                    allocation.EmployeeLeaveCarryForwardId.HasValue;

                if (hasBalanceId == hasCarryForwardId)
                {
                    return new(false,
                        "An allocation has an invalid source. HR review is required.");
                }

                if (allocation.DaysAllocated <= 0)
                {
                    return new(false,
                        "An allocation has an invalid number of days. HR review is required.");
                }

                if (hasBalanceId &&
                    allocation.EmployeeLeaveBalance is null)
                {
                    return new(false,
                        "An allocated leave balance could not be loaded. HR review is required.");
                }

                if (hasCarryForwardId &&
                    allocation.EmployeeLeaveCarryForward is null)
                {
                    return new(false,
                        "An allocated carry-forward lot could not be loaded. HR review is required.");
                }

                if (allocation.EmployeeLeaveBalance is not null &&
                    allocation.EmployeeLeaveBalance.TakenDays <
                    allocation.DaysAllocated)
                {
                    return new(false,
                        "The current-year balance cannot be safely restored. HR review is required.");
                }

                if (allocation.EmployeeLeaveCarryForward is not null &&
                    allocation.EmployeeLeaveCarryForward.RemainingDays +
                    allocation.DaysAllocated >
                    allocation.EmployeeLeaveCarryForward.OriginalDays)
                {
                    return new(false,
                        "The carry-forward balance cannot be safely restored. HR review is required.");
                }
            }

            // Restore allocated balances.
            foreach (var allocation in request.Allocations)
            {
                if (allocation.EmployeeLeaveBalance is not null)
                {
                    var balance = allocation.EmployeeLeaveBalance;

                    balance.TakenDays -= allocation.DaysAllocated;
                    balance.UpdatedAt = DateTime.UtcNow;
                }

                if (allocation.EmployeeLeaveCarryForward is not null)
                {
                    var lot = allocation.EmployeeLeaveCarryForward;

                    lot.RemainingDays += allocation.DaysAllocated;

                    _context.Set<LeaveCarryForwardTransaction>().Add(
                        new LeaveCarryForwardTransaction
                        {
                            EmployeeLeaveCarryForwardId = lot.Id,
                            Action = LeaveCarryForwardAction.Adjusted,
                            LeaveRequestId = request.Id,
                            Days = allocation.DaysAllocated,
                            Remarks =
                                $"Restored after cancellation of leave request #{request.Id}."
                        });
                }
            }

            request.Status = LeaveRequestStatus.Cancelled;
            request.CancelledAt = DateTime.UtcNow;

            _context.Set<LeaveApprovalHistory>().Add(
                new LeaveApprovalHistory
                {
                    LeaveRequestId = request.Id,
                    Action = LeaveApprovalAction.Cancelled,
                    ActorName = actorName,
                    Comments = string.IsNullOrWhiteSpace(remarks)
                        ? "Leave request cancelled."
                        : remarks.Trim()
                });

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new(true,
                "Leave request cancelled and allocated balances restored successfully.");
        }
        catch (Exception ex) when (
            ex is DbUpdateConcurrencyException ||
            (ex is PostgresException pg &&
                (pg.SqlState == "40001" || pg.SqlState == "40P01")) ||
            (ex is DbUpdateException db &&
                db.InnerException is PostgresException inner &&
                (inner.SqlState == "40001" || inner.SqlState == "40P01")))
        {
            await transaction.RollbackAsync(cancellationToken);

            return new(false,
                "A concurrent database change occurred during cancellation. Please retry.");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

