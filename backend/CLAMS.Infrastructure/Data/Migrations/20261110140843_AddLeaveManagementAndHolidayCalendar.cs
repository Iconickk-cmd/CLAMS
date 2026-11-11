using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CLAMS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveManagementAndHolidayCalendar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Holidays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    HolidayDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Holidays", x => x.Id);
                    table.CheckConstraint("CK_Holidays_ValidCategory", "\"Category\" IN ('KenyanPublicHoliday', 'OrganisationClosure')");
                });

            migrationBuilder.CreateTable(
                name: "LeaveTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsPaid = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeLeaveBalances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    LeaveTypeId = table.Column<int>(type: "integer", nullable: false),
                    LeaveYear = table.Column<int>(type: "integer", nullable: false),
                    AnnualEntitlementDays = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: false),
                    AccruedDays = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: false),
                    TakenDays = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: false),
                    AdjustmentDays = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: false),
                    LastAccruedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeLeaveBalances", x => x.Id);
                    table.CheckConstraint("CK_EmployeeLeaveBalances_NonNegative", "\"AccruedDays\" >= 0 AND \"TakenDays\" >= 0");
                    table.ForeignKey(
                        name: "FK_EmployeeLeaveBalances_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeLeaveBalances_LeaveTypes_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalTable: "LeaveTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeLeaveCarryForwards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    LeaveTypeId = table.Column<int>(type: "integer", nullable: false),
                    SourceLeaveYear = table.Column<int>(type: "integer", nullable: false),
                    TargetLeaveYear = table.Column<int>(type: "integer", nullable: false),
                    AvailableFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpiresOn = table.Column<DateOnly>(type: "date", nullable: false),
                    OriginalDays = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: false),
                    RemainingDays = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeLeaveCarryForwards", x => x.Id);
                    table.CheckConstraint("CK_EmployeeLeaveCarryForwards_Dates", "\"ExpiresOn\" > \"AvailableFrom\"");
                    table.CheckConstraint("CK_EmployeeLeaveCarryForwards_Days", "\"OriginalDays\" > 0 AND \"RemainingDays\" >= 0 AND \"RemainingDays\" <= \"OriginalDays\"");
                    table.CheckConstraint("CK_EmployeeLeaveCarryForwards_Years", "\"TargetLeaveYear\" > \"SourceLeaveYear\"");
                    table.ForeignKey(
                        name: "FK_EmployeeLeaveCarryForwards_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeLeaveCarryForwards_LeaveTypes_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalTable: "LeaveTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveEntitlementPolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeaveTypeId = table.Column<int>(type: "integer", nullable: false),
                    EmploymentType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    EmployeeId = table.Column<int>(type: "integer", nullable: true),
                    AnnualEntitlementDays = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: false),
                    AccruesMonthly = table.Column<bool>(type: "boolean", nullable: false),
                    AllowCarryForward = table.Column<bool>(type: "boolean", nullable: false),
                    MaxCarryForwardDays = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: false),
                    CarryForwardExpiryMonths = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveEntitlementPolicies", x => x.Id);
                    table.CheckConstraint("CK_LeaveEntitlementPolicies_CarryForward", "\"MaxCarryForwardDays\" >= 0 AND \"CarryForwardExpiryMonths\" > 0");
                    table.CheckConstraint("CK_LeaveEntitlementPolicies_Entitlement", "\"AnnualEntitlementDays\" >= 0");
                    table.CheckConstraint("CK_LeaveEntitlementPolicies_Target", "(\"EmployeeId\" IS NOT NULL AND \"EmploymentType\" IS NULL) OR (\"EmployeeId\" IS NULL AND \"EmploymentType\" IS NOT NULL)");
                    table.CheckConstraint("CK_LeaveEntitlementPolicies_ValidDates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
                    table.ForeignKey(
                        name: "FK_LeaveEntitlementPolicies_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveEntitlementPolicies_LeaveTypes_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalTable: "LeaveTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    LeaveTypeId = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RequestedDays = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: false),
                    ApprovedDays = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DecisionAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DecisionBy = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    DecisionRemarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequests", x => x.Id);
                    table.CheckConstraint("CK_LeaveRequests_ValidDates", "\"EndDate\" >= \"StartDate\"");
                    table.CheckConstraint("CK_LeaveRequests_ValidDays", "\"RequestedDays\" > 0 AND \"ApprovedDays\" >= 0");
                    table.ForeignKey(
                        name: "FK_LeaveRequests_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_LeaveTypes_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalTable: "LeaveTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveAccrualTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeLeaveBalanceId = table.Column<int>(type: "integer", nullable: false),
                    AccrualDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CompletedServiceMonths = table.Column<int>(type: "integer", nullable: false),
                    DaysAccrued = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveAccrualTransactions", x => x.Id);
                    table.CheckConstraint("CK_LeaveAccrualTransactions_Days", "\"DaysAccrued\" > 0");
                    table.CheckConstraint("CK_LeaveAccrualTransactions_Months", "\"CompletedServiceMonths\" > 0");
                    table.ForeignKey(
                        name: "FK_LeaveAccrualTransactions_EmployeeLeaveBalances_EmployeeLeav~",
                        column: x => x.EmployeeLeaveBalanceId,
                        principalTable: "EmployeeLeaveBalances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveApprovalHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeaveRequestId = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ActorName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Comments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ActionAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveApprovalHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveApprovalHistories_LeaveRequests_LeaveRequestId",
                        column: x => x.LeaveRequestId,
                        principalTable: "LeaveRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveCarryForwardTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeLeaveCarryForwardId = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    LeaveRequestId = table.Column<int>(type: "integer", nullable: true),
                    Days = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveCarryForwardTransactions", x => x.Id);
                    table.CheckConstraint("CK_LeaveCarryForwardTransactions_Days", "\"Days\" > 0");
                    table.ForeignKey(
                        name: "FK_LeaveCarryForwardTransactions_EmployeeLeaveCarryForwards_Em~",
                        column: x => x.EmployeeLeaveCarryForwardId,
                        principalTable: "EmployeeLeaveCarryForwards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveCarryForwardTransactions_LeaveRequests_LeaveRequestId",
                        column: x => x.LeaveRequestId,
                        principalTable: "LeaveRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveRequestAllocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeaveRequestId = table.Column<int>(type: "integer", nullable: false),
                    EmployeeLeaveBalanceId = table.Column<int>(type: "integer", nullable: true),
                    EmployeeLeaveCarryForwardId = table.Column<int>(type: "integer", nullable: true),
                    DaysAllocated = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequestAllocations", x => x.Id);
                    table.CheckConstraint("CK_LeaveRequestAllocations_ExactlyOneSource", "(\"EmployeeLeaveBalanceId\" IS NOT NULL AND \"EmployeeLeaveCarryForwardId\" IS NULL) OR (\"EmployeeLeaveBalanceId\" IS NULL AND \"EmployeeLeaveCarryForwardId\" IS NOT NULL)");
                    table.CheckConstraint("CK_LeaveRequestAllocations_PositiveDays", "\"DaysAllocated\" > 0");
                    table.ForeignKey(
                        name: "FK_LeaveRequestAllocations_EmployeeLeaveBalances_EmployeeLeave~",
                        column: x => x.EmployeeLeaveBalanceId,
                        principalTable: "EmployeeLeaveBalances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveRequestAllocations_EmployeeLeaveCarryForwards_Employee~",
                        column: x => x.EmployeeLeaveCarryForwardId,
                        principalTable: "EmployeeLeaveCarryForwards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveRequestAllocations_LeaveRequests_LeaveRequestId",
                        column: x => x.LeaveRequestId,
                        principalTable: "LeaveRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLeaveBalances_EmployeeId_LeaveTypeId_LeaveYear",
                table: "EmployeeLeaveBalances",
                columns: new[] { "EmployeeId", "LeaveTypeId", "LeaveYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLeaveBalances_LeaveTypeId",
                table: "EmployeeLeaveBalances",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLeaveCarryForwards_EmployeeId_LeaveTypeId_ExpiresOn",
                table: "EmployeeLeaveCarryForwards",
                columns: new[] { "EmployeeId", "LeaveTypeId", "ExpiresOn" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLeaveCarryForwards_EmployeeId_LeaveTypeId_SourceLea~",
                table: "EmployeeLeaveCarryForwards",
                columns: new[] { "EmployeeId", "LeaveTypeId", "SourceLeaveYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLeaveCarryForwards_ExpiresOn",
                table: "EmployeeLeaveCarryForwards",
                column: "ExpiresOn",
                filter: "\"RemainingDays\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLeaveCarryForwards_LeaveTypeId",
                table: "EmployeeLeaveCarryForwards",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_HolidayDate",
                table: "Holidays",
                column: "HolidayDate");

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_HolidayDate_Category",
                table: "Holidays",
                columns: new[] { "HolidayDate", "Category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveAccrualTransactions_EmployeeLeaveBalanceId_AccrualDate",
                table: "LeaveAccrualTransactions",
                columns: new[] { "EmployeeLeaveBalanceId", "AccrualDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveApprovalHistories_LeaveRequestId",
                table: "LeaveApprovalHistories",
                column: "LeaveRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveCarryForwardTransactions_EmployeeLeaveCarryForwardId",
                table: "LeaveCarryForwardTransactions",
                column: "EmployeeLeaveCarryForwardId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveCarryForwardTransactions_LeaveRequestId",
                table: "LeaveCarryForwardTransactions",
                column: "LeaveRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveCarryForwardTransactions_TransactionDate",
                table: "LeaveCarryForwardTransactions",
                column: "TransactionDate");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveEntitlementPolicies_EmployeeId",
                table: "LeaveEntitlementPolicies",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveEntitlementPolicies_LeaveTypeId_EmployeeId_EffectiveFr~",
                table: "LeaveEntitlementPolicies",
                columns: new[] { "LeaveTypeId", "EmployeeId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveEntitlementPolicies_LeaveTypeId_EmploymentType_Effecti~",
                table: "LeaveEntitlementPolicies",
                columns: new[] { "LeaveTypeId", "EmploymentType", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequestAllocations_EmployeeLeaveBalanceId",
                table: "LeaveRequestAllocations",
                column: "EmployeeLeaveBalanceId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequestAllocations_EmployeeLeaveCarryForwardId",
                table: "LeaveRequestAllocations",
                column: "EmployeeLeaveCarryForwardId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequestAllocations_LeaveRequestId",
                table: "LeaveRequestAllocations",
                column: "LeaveRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_EmployeeId",
                table: "LeaveRequests",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_LeaveTypeId",
                table: "LeaveRequests",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_Status",
                table: "LeaveRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveTypes_Code",
                table: "LeaveTypes",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Holidays");

            migrationBuilder.DropTable(
                name: "LeaveAccrualTransactions");

            migrationBuilder.DropTable(
                name: "LeaveApprovalHistories");

            migrationBuilder.DropTable(
                name: "LeaveCarryForwardTransactions");

            migrationBuilder.DropTable(
                name: "LeaveEntitlementPolicies");

            migrationBuilder.DropTable(
                name: "LeaveRequestAllocations");

            migrationBuilder.DropTable(
                name: "EmployeeLeaveBalances");

            migrationBuilder.DropTable(
                name: "EmployeeLeaveCarryForwards");

            migrationBuilder.DropTable(
                name: "LeaveRequests");

            migrationBuilder.DropTable(
                name: "LeaveTypes");
        }
    }
}
