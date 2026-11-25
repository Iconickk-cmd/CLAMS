using CLAMS.Domain.Entities;
using CLAMS.Domain.Enums;
using CLAMS.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CLAMS.Infrastructure.Data;

public class CLAMSHRDbContext
    : IdentityDbContext<CLAMSApplicationUser, IdentityRole<int>, int>
{
    public CLAMSHRDbContext(
        DbContextOptions<CLAMSHRDbContext> options)
        : base(options)
    {
    }

    // ORGANISATION
    public DbSet<Department> Departments => Set<Department>();

    // EMPLOYEES
    public DbSet<Employee> Employees => Set<Employee>();

    // POSITIONS
    public DbSet<Position> Positions => Set<Position>();

    public DbSet<EmployeePositionHistory> EmployeePositionHistories
        => Set<EmployeePositionHistory>();

    // LEAVE MANAGEMENT
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();

    public DbSet<LeaveEntitlementPolicy> LeaveEntitlementPolicies
        => Set<LeaveEntitlementPolicy>();

    public DbSet<EmployeeLeaveBalance> EmployeeLeaveBalances
        => Set<EmployeeLeaveBalance>();

    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

    public DbSet<LeaveRequestAllocation> LeaveRequestAllocations
        => Set<LeaveRequestAllocation>();

    public DbSet<LeaveApprovalHistory> LeaveApprovalHistories
        => Set<LeaveApprovalHistory>();

    public DbSet<LeaveAccrualTransaction> LeaveAccrualTransactions
        => Set<LeaveAccrualTransaction>();

    public DbSet<EmployeeLeaveCarryForward> EmployeeLeaveCarryForwards
        => Set<EmployeeLeaveCarryForward>();

    public DbSet<LeaveCarryForwardTransaction> LeaveCarryForwardTransactions
        => Set<LeaveCarryForwardTransaction>();

    // HOLIDAY CALENDAR
    public DbSet<Holiday> Holidays => Set<Holiday>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // DEPARTMENTS
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(d => d.Id);

            entity.Property(d => d.Code)
                .IsRequired()
                .HasMaxLength(30);

            entity.HasIndex(d => d.Code)
                .IsUnique();

            entity.Property(d => d.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(d => d.Description)
                .HasMaxLength(500);
        });

        // EMPLOYEES
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.EmployeeNumber)
                .IsRequired()
                .HasMaxLength(30);

            entity.HasIndex(e => e.EmployeeNumber)
                .IsUnique();

            entity.Property(e => e.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.MiddleName)
                .HasMaxLength(100);

            entity.Property(e => e.LastName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.NationalId)
                .HasMaxLength(30);

            entity.HasIndex(e => e.NationalId)
                .IsUnique();

            entity.Property(e => e.Gender)
                .HasConversion<string>()
                .HasMaxLength(30);

            entity.Property(e => e.Email)
                .HasMaxLength(150);

            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(30);

            entity.Property(e => e.ResidentialAddress)
                .HasMaxLength(500);

            entity.Property(e => e.EmergencyContactName)
                .HasMaxLength(150);

            entity.Property(e => e.EmergencyContactPhone)
                .HasMaxLength(30);

            entity.Property(e => e.EmergencyContactRelationship)
                .HasMaxLength(100);

            entity.Property(e => e.EmploymentType)
                .HasConversion<string>()
                .HasMaxLength(30);

            entity.Property(e => e.EmploymentStatus)
                .HasConversion<string>()
                .HasMaxLength(30);

            entity.HasOne(e => e.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // POSITIONS
        modelBuilder.Entity<Position>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Code)
                .IsRequired()
                .HasMaxLength(30);

            entity.HasIndex(p => p.Code)
                .IsUnique();

            entity.Property(p => p.Title)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(p => p.Description)
                .HasMaxLength(500);

            entity.Property(p => p.Grade)
                .HasMaxLength(50);

            entity.ToTable(table =>
                table.HasCheckConstraint(
                    "CK_Positions_ApprovedHeadcount",
                    "\"ApprovedHeadcount\" > 0"));

            entity.HasOne(p => p.Department)
                .WithMany(d => d.Positions)
                .HasForeignKey(p => p.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // EMPLOYEE POSITION HISTORY
        modelBuilder.Entity<EmployeePositionHistory>(entity =>
        {
            entity.HasKey(h => h.Id);

            entity.Property(h => h.ChangeReason)
                .HasMaxLength(500);

            entity.Property(h => h.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(PositionAssignmentStatus.Current)
                .HasSentinel((PositionAssignmentStatus)0);

            entity.HasIndex(h => h.EmployeeId);

            entity.HasIndex(h => h.PositionId);

            entity.HasIndex(h => h.EmployeeId)
                .IsUnique()
                .HasFilter("\"Status\" = 'Current'");

            entity.ToTable(table =>
                table.HasCheckConstraint(
                    "CK_EmployeePositionHistories_ValidDates",
                    "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\""));

            entity.HasOne(h => h.Employee)
                .WithMany(e => e.PositionHistories)
                .HasForeignKey(h => h.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(h => h.Position)
                .WithMany(p => p.EmployeePositionHistories)
                .HasForeignKey(h => h.PositionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // LEAVE TYPES
        modelBuilder.Entity<LeaveType>(entity =>
        {
            entity.HasKey(l => l.Id);

            entity.Property(l => l.Code)
                .IsRequired()
                .HasMaxLength(30);

            entity.HasIndex(l => l.Code)
                .IsUnique();

            entity.Property(l => l.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(l => l.Description)
                .HasMaxLength(500);
        });

        // LEAVE ENTITLEMENT POLICIES
        modelBuilder.Entity<LeaveEntitlementPolicy>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.AnnualEntitlementDays)
                .HasPrecision(10, 6);

            entity.Property(p => p.HalfPayEntitlementDays)
                .HasPrecision(10, 6);

            entity.Property(p => p.MaxCarryForwardDays)
                .HasPrecision(10, 6);

            entity.Property(p => p.EmploymentType)
                .HasConversion<string>()
                .HasMaxLength(30);

            entity.Property(p => p.AllocationRule)
                .HasConversion<string>()
                .HasMaxLength(40)
                .HasDefaultValue(LeaveEntitlementAllocationRule.FullPayOnly)
                .HasSentinel((LeaveEntitlementAllocationRule)0);

            entity.HasIndex(p => new
            {
                p.LeaveTypeId,
                p.EmploymentType,
                p.EffectiveFrom
            });

            entity.HasIndex(p => new
            {
                p.LeaveTypeId,
                p.EmployeeId,
                p.EffectiveFrom
            });

            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_LeaveEntitlementPolicies_Entitlement",
                    "\"AnnualEntitlementDays\" >= 0 AND " +
                    "\"HalfPayEntitlementDays\" >= 0");

                table.HasCheckConstraint(
                    "CK_LeaveEntitlementPolicies_CarryForward",
                    "\"MaxCarryForwardDays\" >= 0 AND " +
                    "\"CarryForwardExpiryMonths\" > 0");

                table.HasCheckConstraint(
                    "CK_LeaveEntitlementPolicies_Target",
                    "(\"EmployeeId\" IS NOT NULL AND \"EmploymentType\" IS NULL) OR " +
                    "(\"EmployeeId\" IS NULL AND \"EmploymentType\" IS NOT NULL)");

                table.HasCheckConstraint(
                    "CK_LeaveEntitlementPolicies_ValidDates",
                    "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
            });

            entity.HasOne(p => p.LeaveType)
                .WithMany(l => l.EntitlementPolicies)
                .HasForeignKey(p => p.LeaveTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Employee)
                .WithMany()
                .HasForeignKey(p => p.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // EMPLOYEE LEAVE BALANCES
        modelBuilder.Entity<EmployeeLeaveBalance>(entity =>
        {
            entity.HasKey(b => b.Id);

            entity.Property<uint>("xmin")
                .IsRowVersion();

            entity.Property(b => b.PayCategory)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(PayCategory.FullPay)
                .HasSentinel((PayCategory)0);

            entity.Property(b => b.AnnualEntitlementDays)
                .HasPrecision(10, 6);

            entity.Property(b => b.AccruedDays)
                .HasPrecision(10, 6);

            entity.Property(b => b.TakenDays)
                .HasPrecision(10, 6);

            entity.Property(b => b.AdjustmentDays)
                .HasPrecision(10, 6);

            entity.HasIndex(b => new
            {
                b.EmployeeId,
                b.LeaveTypeId,
                b.LeaveYear,
                b.PayCategory
            }).IsUnique();

            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_EmployeeLeaveBalances_NonNegative",
                    "\"AccruedDays\" >= 0 AND \"TakenDays\" >= 0");
            });

            entity.HasOne(b => b.Employee)
                .WithMany()
                .HasForeignKey(b => b.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.LeaveType)
                .WithMany(l => l.EmployeeBalances)
                .HasForeignKey(b => b.LeaveTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // LEAVE REQUESTS
        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.Property(r => r.RequestedDays)
                .HasPrecision(10, 6);

            entity.Property(r => r.ApprovedDays)
                .HasPrecision(10, 6);

            entity.Property(r => r.Reason)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(r => r.Status)
                .HasConversion<string>()
                .HasMaxLength(30);

            entity.Property(r => r.DecisionBy)
                .HasMaxLength(150);

            entity.Property(r => r.DecisionRemarks)
                .HasMaxLength(1000);

            entity.HasIndex(r => r.EmployeeId);

            entity.HasIndex(r => r.Status);

            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_LeaveRequests_ValidDates",
                    "\"EndDate\" >= \"StartDate\"");

                table.HasCheckConstraint(
                    "CK_LeaveRequests_ValidDays",
                    "\"RequestedDays\" > 0 AND \"ApprovedDays\" >= 0");
            });

            entity.HasOne(r => r.Employee)
                .WithMany()
                .HasForeignKey(r => r.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.LeaveType)
                .WithMany(l => l.LeaveRequests)
                .HasForeignKey(r => r.LeaveTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // LEAVE REQUEST ALLOCATIONS
        modelBuilder.Entity<LeaveRequestAllocation>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.Property(a => a.DaysAllocated)
                .HasPrecision(10, 6);

            entity.HasIndex(a => a.LeaveRequestId);

            entity.HasIndex(a => a.EmployeeLeaveBalanceId);

            entity.HasIndex(a => a.EmployeeLeaveCarryForwardId);

            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_LeaveRequestAllocations_ExactlyOneSource",
                    "(\"EmployeeLeaveBalanceId\" IS NOT NULL AND " +
                    "\"EmployeeLeaveCarryForwardId\" IS NULL) OR " +
                    "(\"EmployeeLeaveBalanceId\" IS NULL AND " +
                    "\"EmployeeLeaveCarryForwardId\" IS NOT NULL)");

                table.HasCheckConstraint(
                    "CK_LeaveRequestAllocations_PositiveDays",
                    "\"DaysAllocated\" > 0");
            });

            entity.HasOne(a => a.LeaveRequest)
                .WithMany(r => r.Allocations)
                .HasForeignKey(a => a.LeaveRequestId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.EmployeeLeaveBalance)
                .WithMany(b => b.Allocations)
                .HasForeignKey(a => a.EmployeeLeaveBalanceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.EmployeeLeaveCarryForward)
                .WithMany(c => c.Allocations)
                .HasForeignKey(a => a.EmployeeLeaveCarryForwardId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // LEAVE APPROVAL HISTORY
        modelBuilder.Entity<LeaveApprovalHistory>(entity =>
        {
            entity.HasKey(h => h.Id);

            entity.Property(h => h.Action)
                .HasConversion<string>()
                .HasMaxLength(30);

            entity.Property(h => h.ActorName)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(h => h.Comments)
                .HasMaxLength(1000);

            entity.HasIndex(h => h.LeaveRequestId);

            entity.HasOne(h => h.LeaveRequest)
                .WithMany(r => r.ApprovalHistory)
                .HasForeignKey(h => h.LeaveRequestId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // LEAVE ACCRUAL TRANSACTIONS
        modelBuilder.Entity<LeaveAccrualTransaction>(entity =>
        {
            entity.HasKey(t => t.Id);

            entity.Property(t => t.DaysAccrued)
                .HasPrecision(10, 6);

            entity.HasIndex(t => new
            {
                t.EmployeeLeaveBalanceId,
                t.AccrualDate
            }).IsUnique();

            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_LeaveAccrualTransactions_Days",
                    "\"DaysAccrued\" > 0");

                table.HasCheckConstraint(
                    "CK_LeaveAccrualTransactions_Months",
                    "\"CompletedServiceMonths\" > 0");
            });

            entity.HasOne(t => t.EmployeeLeaveBalance)
                .WithMany(b => b.AccrualTransactions)
                .HasForeignKey(t => t.EmployeeLeaveBalanceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // EMPLOYEE LEAVE CARRY-FORWARD LOTS
        modelBuilder.Entity<EmployeeLeaveCarryForward>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property<uint>("xmin")
                .IsRowVersion();

            entity.Property(c => c.PayCategory)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(PayCategory.FullPay)
                .HasSentinel((PayCategory)0);

            entity.Property(c => c.OriginalDays)
                .HasPrecision(10, 6);

            entity.Property(c => c.RemainingDays)
                .HasPrecision(10, 6);

            entity.HasIndex(c => new
            {
                c.EmployeeId,
                c.LeaveTypeId,
                c.SourceLeaveYear,
                c.PayCategory
            }).IsUnique();

            entity.HasIndex(c => new
            {
                c.EmployeeId,
                c.LeaveTypeId,
                c.ExpiresOn
            });

            entity.HasIndex(c => c.ExpiresOn)
                .HasFilter("\"RemainingDays\" > 0");

            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_EmployeeLeaveCarryForwards_Days",
                    "\"OriginalDays\" > 0 AND " +
                    "\"RemainingDays\" >= 0 AND " +
                    "\"RemainingDays\" <= \"OriginalDays\"");

                table.HasCheckConstraint(
                    "CK_EmployeeLeaveCarryForwards_Dates",
                    "\"ExpiresOn\" > \"AvailableFrom\"");

                table.HasCheckConstraint(
                    "CK_EmployeeLeaveCarryForwards_Years",
                    "\"TargetLeaveYear\" > \"SourceLeaveYear\"");
            });

            entity.HasOne(c => c.Employee)
                .WithMany()
                .HasForeignKey(c => c.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.LeaveType)
                .WithMany()
                .HasForeignKey(c => c.LeaveTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // LEAVE CARRY-FORWARD TRANSACTIONS
        modelBuilder.Entity<LeaveCarryForwardTransaction>(entity =>
        {
            entity.HasKey(t => t.Id);

            entity.Property(t => t.Action)
                .HasConversion<string>()
                .HasMaxLength(30);

            entity.Property(t => t.Days)
                .HasPrecision(10, 6);

            entity.Property(t => t.Remarks)
                .HasMaxLength(1000);

            entity.HasIndex(t => t.EmployeeLeaveCarryForwardId);

            entity.HasIndex(t => t.LeaveRequestId);

            entity.HasIndex(t => t.TransactionDate);

            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_LeaveCarryForwardTransactions_Days",
                    "\"Days\" > 0");
            });

            entity.HasOne(t => t.EmployeeLeaveCarryForward)
                .WithMany(c => c.Transactions)
                .HasForeignKey(t => t.EmployeeLeaveCarryForwardId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.LeaveRequest)
                .WithMany()
                .HasForeignKey(t => t.LeaveRequestId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // HOLIDAY CALENDAR
        modelBuilder.Entity<Holiday>(entity =>
        {
            entity.HasKey(h => h.Id);

            entity.Property(h => h.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(h => h.Description)
                .HasMaxLength(500);

            entity.Property(h => h.Category)
                .HasConversion<string>()
                .HasMaxLength(40);

            entity.HasIndex(h => h.HolidayDate);

            entity.HasIndex(h => new
            {
                h.HolidayDate,
                h.Category
            }).IsUnique();

            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_Holidays_ValidCategory",
                    "\"Category\" IN ('KenyanPublicHoliday', 'OrganisationClosure')");
            });
        });

        // CLAMS HR USER ACCOUNTS
        modelBuilder.Entity<CLAMSApplicationUser>(entity =>
        {
            entity.Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(u => u.LastName)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasOne(u => u.Employee)
                .WithOne()
                .HasForeignKey<CLAMSApplicationUser>(u => u.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(u => u.EmployeeId)
                .IsUnique();
        });
    }
}




