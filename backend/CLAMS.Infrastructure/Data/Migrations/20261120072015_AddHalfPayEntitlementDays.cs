using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLAMS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHalfPayEntitlementDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_LeaveEntitlementPolicies_Entitlement",
                table: "LeaveEntitlementPolicies");

            migrationBuilder.AddColumn<decimal>(
                name: "HalfPayEntitlementDays",
                table: "LeaveEntitlementPolicies",
                type: "numeric(10,6)",
                precision: 10,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddCheckConstraint(
                name: "CK_LeaveEntitlementPolicies_Entitlement",
                table: "LeaveEntitlementPolicies",
                sql: "\"AnnualEntitlementDays\" >= 0 AND \"HalfPayEntitlementDays\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_LeaveEntitlementPolicies_Entitlement",
                table: "LeaveEntitlementPolicies");

            migrationBuilder.DropColumn(
                name: "HalfPayEntitlementDays",
                table: "LeaveEntitlementPolicies");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LeaveEntitlementPolicies_Entitlement",
                table: "LeaveEntitlementPolicies",
                sql: "\"AnnualEntitlementDays\" >= 0");
        }
    }
}
