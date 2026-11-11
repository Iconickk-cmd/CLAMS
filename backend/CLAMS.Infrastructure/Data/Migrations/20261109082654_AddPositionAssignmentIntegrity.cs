using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLAMS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPositionAssignmentIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmployeePositionHistories_EmployeeId",
                table: "EmployeePositionHistories");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePositionHistories_EmployeeId",
                table: "EmployeePositionHistories",
                column: "EmployeeId",
                unique: true,
                filter: "\"EndDate\" IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmployeePositionHistories_ValidDates",
                table: "EmployeePositionHistories",
                sql: "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmployeePositionHistories_EmployeeId",
                table: "EmployeePositionHistories");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmployeePositionHistories_ValidDates",
                table: "EmployeePositionHistories");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePositionHistories_EmployeeId",
                table: "EmployeePositionHistories",
                column: "EmployeeId");
        }
    }
}
