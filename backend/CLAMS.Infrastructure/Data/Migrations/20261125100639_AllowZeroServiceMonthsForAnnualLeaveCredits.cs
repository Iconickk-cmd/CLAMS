using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLAMS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AllowZeroServiceMonthsForAnnualLeaveCredits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_LeaveAccrualTransactions_Months",
                table: "LeaveAccrualTransactions");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LeaveAccrualTransactions_Months",
                table: "LeaveAccrualTransactions",
                sql: "\"CompletedServiceMonths\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_LeaveAccrualTransactions_Months",
                table: "LeaveAccrualTransactions");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LeaveAccrualTransactions_Months",
                table: "LeaveAccrualTransactions",
                sql: "\"CompletedServiceMonths\" > 0");
        }
    }
}
