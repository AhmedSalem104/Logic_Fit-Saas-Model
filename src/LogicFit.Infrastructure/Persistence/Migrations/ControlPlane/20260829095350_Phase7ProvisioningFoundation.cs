using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFit.Infrastructure.Persistence.Migrations.ControlPlane
{
    /// <inheritdoc />
    public partial class Phase7ProvisioningFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "provisioning");

            migrationBuilder.CreateTable(
                name: "servers",
                schema: "platform",
                columns: table => new
                {
                    server_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    environment = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    provider_key = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "active"),
                    health_status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "healthy"),
                    endpoint_ref = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_servers", x => x.server_id);
                    table.CheckConstraint("CK_platform_servers_health", "[health_status] IN (N'healthy', N'degraded', N'unavailable')");
                    table.CheckConstraint("CK_platform_servers_status", "[status] IN (N'active', N'inactive')");
                });

            migrationBuilder.InsertData(
                schema: "platform",
                table: "servers",
                columns: new[] { "server_id", "name", "environment", "provider_key", "status", "health_status", "endpoint_ref" },
                values: new object[] { new Guid("5e5f5f7e-31d2-4f0c-9ec4-0fcf3fdbac73"), "LogicFit Local SQL Server", "local", "sql-server-local", "active", "healthy", "configured-local-sql-server" });

            migrationBuilder.AddColumn<Guid>(
                name: "owner_user_id",
                schema: "platform",
                table: "gyms",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "server_id",
                schema: "platform",
                table: "gym_databases",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("5e5f5f7e-31d2-4f0c-9ec4-0fcf3fdbac73"));

            migrationBuilder.CreateTable(
                name: "runs",
                schema: "provisioning",
                columns: table => new
                {
                    provisioning_run_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    gym_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    requested_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    current_step = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    attempt_no = table.Column<int>(type: "int", nullable: false),
                    idempotency_key_hash = table.Column<string>(type: "char(64)", nullable: false),
                    request_fingerprint = table.Column<string>(type: "char(64)", nullable: false),
                    server_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    gym_database_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    requested_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    started_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    completed_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    failure_category = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    error_code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    safe_error_metadata_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    last_retry_idempotency_key_hash = table.Column<string>(type: "char(64)", nullable: true),
                    last_retry_fingerprint = table.Column<string>(type: "char(64)", nullable: true),
                    last_retry_failed_step = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    last_retry_next_step = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    last_retry_attempt_no = table.Column<int>(type: "int", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provisioning_runs", x => x.provisioning_run_id);
                    table.CheckConstraint("CK_provisioning_runs_attempt", "[attempt_no] > 0");
                    table.CheckConstraint("CK_provisioning_runs_status", "[status] IN (N'Requested', N'Provisioning', N'Migrating', N'Seeding', N'Verifying', N'Active', N'ProvisioningFailed', N'MigrationFailed', N'SeedingFailed', N'VerificationFailed')");
                    table.ForeignKey(
                        name: "FK_runs_gym_databases_gym_database_id",
                        column: x => x.gym_database_id,
                        principalSchema: "platform",
                        principalTable: "gym_databases",
                        principalColumn: "gym_database_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_runs_gyms_gym_id",
                        column: x => x.gym_id,
                        principalSchema: "platform",
                        principalTable: "gyms",
                        principalColumn: "gym_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_runs_organizations_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "platform",
                        principalTable: "organizations",
                        principalColumn: "organization_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_runs_servers_server_id",
                        column: x => x.server_id,
                        principalSchema: "platform",
                        principalTable: "servers",
                        principalColumn: "server_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_runs_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalSchema: "iam",
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_runs_users_requested_by_user_id",
                        column: x => x.requested_by_user_id,
                        principalSchema: "iam",
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "steps",
                schema: "provisioning",
                columns: table => new
                {
                    provisioning_step_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    provisioning_run_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    step_key = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    attempt_no = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    started_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    completed_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    retryable = table.Column<bool>(type: "bit", nullable: false),
                    failure_category = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    error_code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    safe_metadata_json = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provisioning_steps", x => x.provisioning_step_id);
                    table.CheckConstraint("CK_provisioning_steps_attempt", "[attempt_no] > 0");
                    table.CheckConstraint("CK_provisioning_steps_status", "[status] IN (N'Pending', N'Running', N'Success', N'Failed')");
                    table.ForeignKey(
                        name: "FK_steps_runs_provisioning_run_id",
                        column: x => x.provisioning_run_id,
                        principalSchema: "provisioning",
                        principalTable: "runs",
                        principalColumn: "provisioning_run_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gyms_owner_user_id",
                schema: "platform",
                table: "gyms",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_gym_databases_server_id",
                schema: "platform",
                table: "gym_databases",
                column: "server_id");

            migrationBuilder.CreateIndex(
                name: "IX_provisioning_runs_status_updated",
                schema: "provisioning",
                table: "runs",
                columns: new[] { "status", "updated_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_runs_gym_database_id",
                schema: "provisioning",
                table: "runs",
                column: "gym_database_id");

            migrationBuilder.CreateIndex(
                name: "IX_runs_organization_id",
                schema: "provisioning",
                table: "runs",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_runs_owner_user_id",
                schema: "provisioning",
                table: "runs",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_runs_server_id",
                schema: "provisioning",
                table: "runs",
                column: "server_id");

            migrationBuilder.CreateIndex(
                name: "UQ_provisioning_runs_actor_idempotency",
                schema: "provisioning",
                table: "runs",
                columns: new[] { "requested_by_user_id", "idempotency_key_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_provisioning_runs_gym_active",
                schema: "provisioning",
                table: "runs",
                column: "gym_id",
                unique: true,
                filter: "[status] IN (N'Requested', N'Provisioning', N'Migrating', N'Seeding', N'Verifying', N'Active')");

            migrationBuilder.CreateIndex(
                name: "UQ_platform_servers_environment_name",
                schema: "platform",
                table: "servers",
                columns: new[] { "environment", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provisioning_steps_run_step",
                schema: "provisioning",
                table: "steps",
                columns: new[] { "provisioning_run_id", "step_key" });

            migrationBuilder.CreateIndex(
                name: "UQ_provisioning_steps_run_step_attempt",
                schema: "provisioning",
                table: "steps",
                columns: new[] { "provisioning_run_id", "step_key", "attempt_no" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_gym_databases_servers_server_id",
                schema: "platform",
                table: "gym_databases",
                column: "server_id",
                principalSchema: "platform",
                principalTable: "servers",
                principalColumn: "server_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_gyms_users_owner_user_id",
                schema: "platform",
                table: "gyms",
                column: "owner_user_id",
                principalSchema: "iam",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_gym_databases_servers_server_id",
                schema: "platform",
                table: "gym_databases");

            migrationBuilder.DropForeignKey(
                name: "FK_gyms_users_owner_user_id",
                schema: "platform",
                table: "gyms");

            migrationBuilder.DropTable(
                name: "steps",
                schema: "provisioning");

            migrationBuilder.DropTable(
                name: "runs",
                schema: "provisioning");

            migrationBuilder.DropTable(
                name: "servers",
                schema: "platform");

            migrationBuilder.DropIndex(
                name: "IX_gyms_owner_user_id",
                schema: "platform",
                table: "gyms");

            migrationBuilder.DropIndex(
                name: "IX_gym_databases_server_id",
                schema: "platform",
                table: "gym_databases");

            migrationBuilder.DropColumn(
                name: "owner_user_id",
                schema: "platform",
                table: "gyms");

            migrationBuilder.DropColumn(
                name: "server_id",
                schema: "platform",
                table: "gym_databases");
        }
    }
}
