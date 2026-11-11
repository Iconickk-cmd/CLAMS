
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLAMS.Infrastructure.Data.Migrations
{
    public partial class AddPositionAssignmentStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove the old unique index based on EndDate.
            migrationBuilder.DropIndex(
                name: "IX_EmployeePositionHistories_EmployeeId",
                table: "EmployeePositionHistories");

            // Add Status. Existing records initially receive Current.
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "EmployeePositionHistories",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Current");

            // Backfill the correct status for every existing assignment.
            migrationBuilder.Sql(
                """
                UPDATE "EmployeePositionHistories"
                SET "Status" =
                    CASE
                        WHEN "EndDate" IS NOT NULL THEN 'Completed'
                        WHEN "StartDate" > CURRENT_DATE THEN 'Scheduled'
                        ELSE 'Current'
                    END;
                """);

            // Only one Current assignment is allowed per employee.
            // Scheduled and Completed assignments may coexist.
            migrationBuilder.CreateIndex(
                name: "IX_EmployeePositionHistories_EmployeeId",
                table: "EmployeePositionHistories",
                column: "EmployeeId",
                unique: true,
                filter: "\"Status\" = 'Current'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmployeePositionHistories_EmployeeId",
                table: "EmployeePositionHistories");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "EmployeePositionHistories");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePositionHistories_EmployeeId",
                table: "EmployeePositionHistories",
                column: "EmployeeId",
                unique: true,
                filter: "\"EndDate\" IS NULL");
        }
    }
}
