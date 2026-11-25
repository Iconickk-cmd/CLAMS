using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLAMS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveEntitlementAllocationRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AllocationRule",
                table: "LeaveEntitlementPolicies",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "FullPayOnly");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllocationRule",
                table: "LeaveEntitlementPolicies");
        }
    }
}
