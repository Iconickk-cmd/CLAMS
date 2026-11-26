using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLAMS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeSupervisor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SupervisorEmployeeId",
                table: "Employees",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_SupervisorEmployeeId",
                table: "Employees",
                column: "SupervisorEmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Employees_SupervisorEmployeeId",
                table: "Employees",
                column: "SupervisorEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Employees_SupervisorEmployeeId",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_SupervisorEmployeeId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "SupervisorEmployeeId",
                table: "Employees");
        }
    }
}
