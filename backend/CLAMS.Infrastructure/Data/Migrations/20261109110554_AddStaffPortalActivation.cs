using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLAMS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffPortalActivation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActivationTokenExpiresAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActivationTokenHash",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPortalActivated",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "TemporaryPasswordExpiresAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActivationTokenExpiresAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ActivationTokenHash",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsPortalActivated",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TemporaryPasswordExpiresAt",
                table: "AspNetUsers");
        }
    }
}
