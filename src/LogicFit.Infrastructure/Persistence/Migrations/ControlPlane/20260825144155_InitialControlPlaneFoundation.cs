using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFit.Infrastructure.Persistence.Migrations.ControlPlane
{
    /// <inheritdoc />
    public partial class InitialControlPlaneFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "iam");

            migrationBuilder.EnsureSchema(
                name: "migrations");

            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.EnsureSchema(
                name: "platform");

            migrationBuilder.CreateTable(
                name: "definitions",
                schema: "migrations",
                columns: table => new
                {
                    migration_definition_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    migration_key = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    from_version = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    to_version = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    checksum_sha256 = table.Column<string>(type: "char(64)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "approved"),
                    created_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_migrations_definitions", x => x.migration_definition_id);
                });

            migrationBuilder.CreateTable(
                name: "events",
                schema: "audit",
                columns: table => new
                {
                    audit_event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    request_id = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    actor_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    target_type = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    target_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    action = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    result = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    metadata_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    occurred_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_events", x => x.audit_event_id);
                });

            migrationBuilder.CreateTable(
                name: "feature_flags",
                schema: "platform",
                columns: table => new
                {
                    feature_flag_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    flag_key = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    scope_type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    scope_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    enabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    config_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_feature_flags", x => x.feature_flag_id);
                });

            migrationBuilder.CreateTable(
                name: "organizations",
                schema: "platform",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    slug = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "active"),
                    created_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_organizations", x => x.organization_id);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                schema: "iam",
                columns: table => new
                {
                    permission_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    permission_key = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    domain = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    action = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    risk_level = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "normal"),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_permissions", x => x.permission_id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "iam",
                columns: table => new
                {
                    role_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    scope_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "active"),
                    created_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_roles", x => x.role_id);
                });

            migrationBuilder.CreateTable(
                name: "runs",
                schema: "migrations",
                columns: table => new
                {
                    migration_run_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    migration_key = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "running"),
                    requested_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    completed_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    error_code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_migrations_runs", x => x.migration_run_id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "iam",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    display_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "active"),
                    last_login_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_users", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "gyms",
                schema: "platform",
                columns: table => new
                {
                    gym_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    slug = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "provisioning"),
                    timezone_name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false, defaultValue: "Africa/Cairo"),
                    created_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_gyms", x => x.gym_id);
                    table.ForeignKey(
                        name: "FK_gyms_organizations_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "platform",
                        principalTable: "organizations",
                        principalColumn: "organization_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                schema: "iam",
                columns: table => new
                {
                    role_permission_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    role_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    permission_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    scope_rule_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_role_permissions", x => x.role_permission_id);
                    table.ForeignKey(
                        name: "FK_role_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalSchema: "iam",
                        principalTable: "permissions",
                        principalColumn: "permission_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "iam",
                        principalTable: "roles",
                        principalColumn: "role_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "credentials",
                schema: "iam",
                columns: table => new
                {
                    credential_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    credential_type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    secret_hash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    secret_version = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    last_rotated_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_credentials", x => x.credential_id);
                    table.ForeignKey(
                        name: "FK_credentials_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "iam",
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mfa_factors",
                schema: "iam",
                columns: table => new
                {
                    mfa_factor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    factor_type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "totp"),
                    secret_ref = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "pending"),
                    verified_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_mfa_factors", x => x.mfa_factor_id);
                    table.ForeignKey(
                        name: "FK_mfa_factors_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "iam",
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "password_reset_tokens",
                schema: "iam",
                columns: table => new
                {
                    password_reset_token_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    token_hash = table.Column<string>(type: "char(64)", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    used_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    revoked_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    requested_ip = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    request_id = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_password_reset_tokens", x => x.password_reset_token_id);
                    table.ForeignKey(
                        name: "FK_password_reset_tokens_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "iam",
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "gym_databases",
                schema: "platform",
                columns: table => new
                {
                    gym_database_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    gym_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    database_name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    environment = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "local"),
                    schema_version = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    seed_version = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "pending"),
                    connection_secret_ref = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
                    last_health_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_gym_databases", x => x.gym_database_id);
                    table.ForeignKey(
                        name: "FK_gym_databases_gyms_gym_id",
                        column: x => x.gym_id,
                        principalSchema: "platform",
                        principalTable: "gyms",
                        principalColumn: "gym_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sessions",
                schema: "iam",
                columns: table => new
                {
                    session_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    gym_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    token_hash = table.Column<string>(type: "char(64)", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    revoked_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    last_seen_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    session_kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "staff"),
                    mfa_verified = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    idle_expires_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    absolute_expires_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    user_agent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ip_address = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_sessions", x => x.session_id);
                    table.ForeignKey(
                        name: "FK_sessions_gyms_gym_id",
                        column: x => x.gym_id,
                        principalSchema: "platform",
                        principalTable: "gyms",
                        principalColumn: "gym_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sessions_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "iam",
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_gym_roles",
                schema: "iam",
                columns: table => new
                {
                    assignment_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    gym_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    role_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    scope_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "active"),
                    created_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_user_gym_roles", x => x.assignment_id);
                    table.ForeignKey(
                        name: "FK_user_gym_roles_gyms_gym_id",
                        column: x => x.gym_id,
                        principalSchema: "platform",
                        principalTable: "gyms",
                        principalColumn: "gym_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_gym_roles_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "iam",
                        principalTable: "roles",
                        principalColumn: "role_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_gym_roles_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "iam",
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mfa_recovery_codes",
                schema: "iam",
                columns: table => new
                {
                    recovery_code_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    mfa_factor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    code_hash = table.Column<string>(type: "char(64)", nullable: false),
                    used_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    revoked_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_mfa_recovery_codes", x => x.recovery_code_id);
                    table.ForeignKey(
                        name: "FK_mfa_recovery_codes_mfa_factors_mfa_factor_id",
                        column: x => x.mfa_factor_id,
                        principalSchema: "iam",
                        principalTable: "mfa_factors",
                        principalColumn: "mfa_factor_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_mfa_recovery_codes_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "iam",
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_iam_credentials_type",
                schema: "iam",
                table: "credentials",
                columns: new[] { "user_id", "credential_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_migrations_definitions_key",
                schema: "migrations",
                table: "definitions",
                column: "migration_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_platform_feature_flags_scope",
                schema: "platform",
                table: "feature_flags",
                columns: new[] { "flag_key", "scope_type", "scope_id" },
                unique: true,
                filter: "[scope_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_gym_databases_gym_id",
                schema: "platform",
                table: "gym_databases",
                column: "gym_id");

            migrationBuilder.CreateIndex(
                name: "UQ_platform_gym_databases_name",
                schema: "platform",
                table: "gym_databases",
                columns: new[] { "environment", "database_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_platform_gyms_org_slug",
                schema: "platform",
                table: "gyms",
                columns: new[] { "organization_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mfa_factors_user_id",
                schema: "iam",
                table: "mfa_factors",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_iam_recovery_codes_active",
                schema: "iam",
                table: "mfa_recovery_codes",
                columns: new[] { "user_id", "mfa_factor_id", "used_at_utc", "revoked_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_mfa_recovery_codes_mfa_factor_id",
                schema: "iam",
                table: "mfa_recovery_codes",
                column: "mfa_factor_id");

            migrationBuilder.CreateIndex(
                name: "UQ_iam_mfa_recovery_codes_hash",
                schema: "iam",
                table: "mfa_recovery_codes",
                column: "code_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_platform_organizations_slug",
                schema: "platform",
                table: "organizations",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_iam_password_reset_active",
                schema: "iam",
                table: "password_reset_tokens",
                columns: new[] { "user_id", "expires_at_utc", "used_at_utc", "revoked_at_utc" });

            migrationBuilder.CreateIndex(
                name: "UQ_iam_password_reset_tokens_hash",
                schema: "iam",
                table: "password_reset_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_iam_permissions_key",
                schema: "iam",
                table: "permissions",
                column: "permission_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_permission_id",
                schema: "iam",
                table: "role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "UQ_iam_role_permissions_pair",
                schema: "iam",
                table: "role_permissions",
                columns: new[] { "role_id", "permission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_iam_roles_scope_name",
                schema: "iam",
                table: "roles",
                columns: new[] { "scope_type", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_iam_sessions_gym",
                schema: "iam",
                table: "sessions",
                columns: new[] { "gym_id", "user_id", "revoked_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_iam_sessions_user_active",
                schema: "iam",
                table: "sessions",
                columns: new[] { "user_id", "revoked_at_utc", "expires_at_utc" });

            migrationBuilder.CreateIndex(
                name: "UQ_iam_sessions_token",
                schema: "iam",
                table: "sessions",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_iam_user_gym_roles_scope",
                schema: "iam",
                table: "user_gym_roles",
                columns: new[] { "user_id", "scope_type", "gym_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_user_gym_roles_gym_id",
                schema: "iam",
                table: "user_gym_roles",
                column: "gym_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_gym_roles_role_id",
                schema: "iam",
                table: "user_gym_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "UX_iam_user_gym_roles_gym",
                schema: "iam",
                table: "user_gym_roles",
                columns: new[] { "user_id", "gym_id", "role_id" },
                unique: true,
                filter: "[scope_type] = N'gym' AND [status] = N'active'");

            migrationBuilder.CreateIndex(
                name: "UX_iam_user_gym_roles_platform",
                schema: "iam",
                table: "user_gym_roles",
                columns: new[] { "user_id", "role_id" },
                unique: true,
                filter: "[scope_type] = N'platform' AND [status] = N'active'");

            migrationBuilder.CreateIndex(
                name: "UQ_iam_users_email",
                schema: "iam",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "credentials",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "definitions",
                schema: "migrations");

            migrationBuilder.DropTable(
                name: "events",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "feature_flags",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "gym_databases",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "mfa_recovery_codes",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "password_reset_tokens",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "role_permissions",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "runs",
                schema: "migrations");

            migrationBuilder.DropTable(
                name: "sessions",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "user_gym_roles",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "mfa_factors",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "permissions",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "gyms",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "users",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "organizations",
                schema: "platform");
        }
    }
}
