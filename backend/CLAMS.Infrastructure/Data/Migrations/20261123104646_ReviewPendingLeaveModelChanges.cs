using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLAMS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReviewPendingLeaveModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "EmployeeLeaveCarryForwards",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "EmployeeLeaveBalances",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                table: "EmployeeLeaveCarryForwards");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "EmployeeLeaveBalances");
        }
    }
}
