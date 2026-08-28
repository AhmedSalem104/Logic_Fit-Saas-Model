using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFit.Infrastructure.Persistence.Migrations.ControlPlane
{
    /// <inheritdoc />
    public partial class Phase6PlatformAuditScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "scope_id",
                schema: "audit",
                table: "events",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "scope_type",
                schema: "audit",
                table: "events",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "scope_id",
                schema: "audit",
                table: "events");

            migrationBuilder.DropColumn(
                name: "scope_type",
                schema: "audit",
                table: "events");
        }
    }
}
