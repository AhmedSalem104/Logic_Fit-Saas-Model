using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFit.Infrastructure.Persistence.Migrations.Gym
{
    /// <inheritdoc />
    public partial class Phase8Members : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "members");

            migrationBuilder.CreateTable(
                name: "members",
                schema: "members",
                columns: table => new
                {
                    member_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    gym_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    member_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    full_name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    registration_date = table.Column<DateOnly>(type: "date", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "ACTIVE"),
                    create_idempotency_key_hash = table.Column<string>(type: "char(64)", nullable: false),
                    create_request_fingerprint = table.Column<string>(type: "char(64)", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_members_members", x => x.member_id);
                    table.CheckConstraint("CK_members_status", "[status] IN (N'ACTIVE', N'INACTIVE', N'ARCHIVED')");
                });

            migrationBuilder.CreateTable(
                name: "timeline_events",
                schema: "members",
                columns: table => new
                {
                    timeline_event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    member_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    event_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    event_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    source_type = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    source_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    summary = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    metadata_json = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_members_timeline_events", x => x.timeline_event_id);
                    table.ForeignKey(
                        name: "FK_timeline_events_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "members",
                        principalTable: "members",
                        principalColumn: "member_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_members_email",
                schema: "members",
                table: "members",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "IX_members_gym_updated",
                schema: "members",
                table: "members",
                columns: new[] { "gym_id", "updated_at_utc", "member_id" });

            migrationBuilder.CreateIndex(
                name: "IX_members_phone",
                schema: "members",
                table: "members",
                column: "phone");

            migrationBuilder.CreateIndex(
                name: "IX_members_status_created",
                schema: "members",
                table: "members",
                columns: new[] { "status", "created_at_utc", "member_id" });

            migrationBuilder.CreateIndex(
                name: "UQ_members_create_idempotency",
                schema: "members",
                table: "members",
                columns: new[] { "gym_id", "create_idempotency_key_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_members_member_code_gym",
                schema: "members",
                table: "members",
                columns: new[] { "gym_id", "member_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_members_timeline_member_event",
                schema: "members",
                table: "timeline_events",
                columns: new[] { "member_id", "event_at_utc", "timeline_event_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "timeline_events",
                schema: "members");

            migrationBuilder.DropTable(
                name: "members",
                schema: "members");
        }
    }
}
