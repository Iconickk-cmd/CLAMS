using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLAMS.Infrastructure.Data.Migrations
{
    public partial class AddAssignmentDateOverlapProtection : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enable GiST support for equality comparisons on EmployeeId.
            migrationBuilder.Sql(
                """
                CREATE EXTENSION IF NOT EXISTS btree_gist;
                """);

            // Prevent overlapping assignment date ranges
            // for the same employee.
            migrationBuilder.Sql(
                """
                ALTER TABLE "EmployeePositionHistories"
                ADD CONSTRAINT "EX_EmployeePositionHistories_Employee_DateRange"
                EXCLUDE USING GIST
                (
                    "EmployeeId" WITH =,
                    daterange("StartDate", "EndDate", '[]') WITH &&
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "EmployeePositionHistories"
                DROP CONSTRAINT IF EXISTS
                "EX_EmployeePositionHistories_Employee_DateRange";
                """);

            // Leave btree_gist installed because other database
            // objects may depend on the extension.
        }
    }
}