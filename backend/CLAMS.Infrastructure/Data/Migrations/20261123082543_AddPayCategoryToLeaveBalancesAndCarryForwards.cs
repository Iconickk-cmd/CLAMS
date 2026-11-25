using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLAMS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPayCategoryToLeaveBalancesAndCarryForwards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmployeeLeaveCarryForwards_EmployeeId_LeaveTypeId_SourceLea~",
                table: "EmployeeLeaveCarryForwards");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeLeaveBalances_EmployeeId_LeaveTypeId_LeaveYear",
                table: "EmployeeLeaveBalances");

            migrationBuilder.AddColumn<string>(
                name: "PayCategory",
                table: "EmployeeLeaveCarryForwards",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "FullPay");

            migrationBuilder.AddColumn<string>(
                name: "PayCategory",
                table: "EmployeeLeaveBalances",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "FullPay");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLeaveCarryForwards_EmployeeId_LeaveTypeId_SourceLea~",
                table: "EmployeeLeaveCarryForwards",
                columns: new[] { "EmployeeId", "LeaveTypeId", "SourceLeaveYear", "PayCategory" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLeaveBalances_EmployeeId_LeaveTypeId_LeaveYear_PayC~",
                table: "EmployeeLeaveBalances",
                columns: new[] { "EmployeeId", "LeaveTypeId", "LeaveYear", "PayCategory" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmployeeLeaveCarryForwards_EmployeeId_LeaveTypeId_SourceLea~",
                table: "EmployeeLeaveCarryForwards");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeLeaveBalances_EmployeeId_LeaveTypeId_LeaveYear_PayC~",
                table: "EmployeeLeaveBalances");

            migrationBuilder.DropColumn(
                name: "PayCategory",
                table: "EmployeeLeaveCarryForwards");

            migrationBuilder.DropColumn(
                name: "PayCategory",
                table: "EmployeeLeaveBalances");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLeaveCarryForwards_EmployeeId_LeaveTypeId_SourceLea~",
                table: "EmployeeLeaveCarryForwards",
                columns: new[] { "EmployeeId", "LeaveTypeId", "SourceLeaveYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLeaveBalances_EmployeeId_LeaveTypeId_LeaveYear",
                table: "EmployeeLeaveBalances",
                columns: new[] { "EmployeeId", "LeaveTypeId", "LeaveYear" },
                unique: true);
        }
    }
}
