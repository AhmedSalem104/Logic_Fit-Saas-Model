using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFit.Infrastructure.Persistence.Migrations.ControlPlane
{
    /// <inheritdoc />
    public partial class Phase7LifecycleStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Replace the legacy Phase 2/5 registry constraints without
            // touching existing registry rows. The local databases may have
            // either or both definitions depending on their migration age.
            migrationBuilder.Sql(
                "IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_platform_gyms_status' AND parent_object_id = OBJECT_ID(N'[platform].[gyms]')) " +
                "ALTER TABLE [platform].[gyms] DROP CONSTRAINT [CK_platform_gyms_status]; " +
                "IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_platform_gym_databases_status' AND parent_object_id = OBJECT_ID(N'[platform].[gym_databases]')) " +
                "ALTER TABLE [platform].[gym_databases] DROP CONSTRAINT [CK_platform_gym_databases_status];");

            migrationBuilder.AddCheckConstraint(
                name: "CK_platform_gyms_status",
                schema: "platform",
                table: "gyms",
                sql: "[status] IN (N'archived', N'suspended', N'ready', N'provisioning', N'Provisioning', N'Migrating', N'Seeding', N'Verifying', N'Active')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_platform_gym_databases_status",
                schema: "platform",
                table: "gym_databases",
                sql: "[status] IN (N'pending', N'provisioning', N'Provisioning', N'Migrating', N'Seeding', N'Verifying', N'Active', N'healthy', N'degraded', N'failed', N'disabled')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_platform_gyms_status",
                schema: "platform",
                table: "gyms");

            migrationBuilder.DropCheckConstraint(
                name: "CK_platform_gym_databases_status",
                schema: "platform",
                table: "gym_databases");
        }
    }
}
