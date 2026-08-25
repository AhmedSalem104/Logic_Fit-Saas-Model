using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFit.Infrastructure.Persistence.Migrations.Gym
{
    /// <inheritdoc />
    public partial class InitialGymFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "library");

            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.EnsureSchema(
                name: "core");

            migrationBuilder.EnsureSchema(
                name: "auth");

            migrationBuilder.CreateTable(
                name: "__seed_installations",
                schema: "library",
                columns: table => new
                {
                    installation_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    seed_domain = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    seed_version = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    source_version = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    checksum_sha256 = table.Column<string>(type: "char(64)", nullable: false),
                    record_count = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    installed_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK___seed_installations", x => x.installation_id);
                });

            migrationBuilder.CreateTable(
                name: "equipment",
                schema: "library",
                columns: table => new
                {
                    equipment_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    seed_key = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    name_ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    name_en = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    record_scope = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    source = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    source_version = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    payload_json = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seed_equipment", x => x.equipment_id);
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
                    table.PrimaryKey("PK_gym_audit_events", x => x.audit_event_id);
                });

            migrationBuilder.CreateTable(
                name: "exercise_categories",
                schema: "library",
                columns: table => new
                {
                    category_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    seed_key = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    name_ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    name_en = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    record_scope = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    source = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    source_version = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    payload_json = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seed_exercise_categories", x => x.category_id);
                });

            migrationBuilder.CreateTable(
                name: "food_categories",
                schema: "library",
                columns: table => new
                {
                    food_category_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    seed_key = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    name_ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    name_en = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    record_scope = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    source = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    source_version = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    payload_json = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seed_food_categories", x => x.food_category_id);
                });

            migrationBuilder.CreateTable(
                name: "food_units",
                schema: "library",
                columns: table => new
                {
                    food_unit_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    seed_key = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    name_ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    name_en = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    dimension = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    base_quantity = table.Column<decimal>(type: "decimal(12,3)", nullable: false),
                    base_unit_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    record_scope = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    source = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    source_version = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    payload_json = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seed_food_units", x => x.food_unit_id);
                });

            migrationBuilder.CreateTable(
                name: "gym_context",
                schema: "core",
                columns: table => new
                {
                    gym_context_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    control_plane_gym_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    gym_code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    display_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    timezone_name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false, defaultValue: "Africa/Cairo"),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "provisioning"),
                    created_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_core_gym_context", x => x.gym_context_id);
                });

            migrationBuilder.CreateTable(
                name: "gym_users",
                schema: "auth",
                columns: table => new
                {
                    gym_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    control_plane_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "active"),
                    display_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    last_permission_sync_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_gym_users", x => x.gym_user_id);
                });

            migrationBuilder.CreateTable(
                name: "levels",
                schema: "library",
                columns: table => new
                {
                    level_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    seed_key = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    level_type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    name_ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    name_en = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    record_scope = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    source = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    source_version = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    payload_json = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seed_levels", x => x.level_id);
                });

            migrationBuilder.CreateTable(
                name: "muscle_groups",
                schema: "library",
                columns: table => new
                {
                    muscle_group_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    seed_key = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    name_ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    name_en = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    record_scope = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    source = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    source_version = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    payload_json = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seed_muscle_groups", x => x.muscle_group_id);
                });

            migrationBuilder.CreateTable(
                name: "foods",
                schema: "library",
                columns: table => new
                {
                    food_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    seed_key = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    name_ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    name_en = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    record_scope = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    source = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    source_version = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    payload_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    food_category_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    slug = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    serving_quantity = table.Column<decimal>(type: "decimal(12,3)", nullable: false),
                    serving_unit_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    calculation_quantity = table.Column<decimal>(type: "decimal(12,3)", nullable: false),
                    calculation_unit_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    calories = table.Column<decimal>(type: "decimal(12,3)", nullable: false),
                    protein = table.Column<decimal>(type: "decimal(12,3)", nullable: false),
                    carbs = table.Column<decimal>(type: "decimal(12,3)", nullable: false),
                    fat = table.Column<decimal>(type: "decimal(12,3)", nullable: false),
                    fiber = table.Column<decimal>(type: "decimal(12,3)", nullable: true),
                    sugar = table.Column<decimal>(type: "decimal(12,3)", nullable: true),
                    sodium = table.Column<decimal>(type: "decimal(12,3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seed_foods", x => x.food_id);
                    table.ForeignKey(
                        name: "FK_foods_food_categories_food_category_id",
                        column: x => x.food_category_id,
                        principalSchema: "library",
                        principalTable: "food_categories",
                        principalColumn: "food_category_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_foods_food_units_calculation_unit_id",
                        column: x => x.calculation_unit_id,
                        principalSchema: "library",
                        principalTable: "food_units",
                        principalColumn: "food_unit_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_foods_food_units_serving_unit_id",
                        column: x => x.serving_unit_id,
                        principalSchema: "library",
                        principalTable: "food_units",
                        principalColumn: "food_unit_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "muscles",
                schema: "library",
                columns: table => new
                {
                    muscle_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    muscle_group_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    seed_key = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    name_ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    record_scope = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    source = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    source_version = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    payload_json = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seed_muscles", x => x.muscle_id);
                    table.ForeignKey(
                        name: "FK_muscles_muscle_groups_muscle_group_id",
                        column: x => x.muscle_group_id,
                        principalSchema: "library",
                        principalTable: "muscle_groups",
                        principalColumn: "muscle_group_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "anatomy_mappings",
                schema: "library",
                columns: table => new
                {
                    anatomy_mapping_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    seed_key = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    name_ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    name_en = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    record_scope = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    source = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    source_version = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    payload_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    muscle_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    body_region = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    view_code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    asset_key = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seed_anatomy_mappings", x => x.anatomy_mapping_id);
                    table.ForeignKey(
                        name: "FK_anatomy_mappings_muscles_muscle_id",
                        column: x => x.muscle_id,
                        principalSchema: "library",
                        principalTable: "muscles",
                        principalColumn: "muscle_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "exercises",
                schema: "library",
                columns: table => new
                {
                    exercise_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    seed_key = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    name_ar = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    record_scope = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    source = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    source_version = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    payload_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    slug = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    primary_muscle_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    difficulty_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    category_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    level_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    movement_pattern = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    instructions_ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    instructions_en = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    tips_ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    common_mistakes_ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    media_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    legacy_status = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seed_exercises", x => x.exercise_id);
                    table.ForeignKey(
                        name: "FK_exercises_exercise_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "library",
                        principalTable: "exercise_categories",
                        principalColumn: "category_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_exercises_levels_level_id",
                        column: x => x.level_id,
                        principalSchema: "library",
                        principalTable: "levels",
                        principalColumn: "level_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_exercises_muscles_primary_muscle_id",
                        column: x => x.primary_muscle_id,
                        principalSchema: "library",
                        principalTable: "muscles",
                        principalColumn: "muscle_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "exercise_equipment",
                schema: "library",
                columns: table => new
                {
                    exercise_equipment_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    exercise_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    equipment_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_required = table.Column<bool>(type: "bit", nullable: true),
                    payload_json = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seed_exercise_equipment", x => x.exercise_equipment_id);
                    table.ForeignKey(
                        name: "FK_exercise_equipment_equipment_equipment_id",
                        column: x => x.equipment_id,
                        principalSchema: "library",
                        principalTable: "equipment",
                        principalColumn: "equipment_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_exercise_equipment_exercises_exercise_id",
                        column: x => x.exercise_id,
                        principalSchema: "library",
                        principalTable: "exercises",
                        principalColumn: "exercise_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "exercise_muscles",
                schema: "library",
                columns: table => new
                {
                    exercise_muscle_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    exercise_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    muscle_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    role = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    payload_json = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seed_exercise_muscles", x => x.exercise_muscle_id);
                    table.ForeignKey(
                        name: "FK_exercise_muscles_exercises_exercise_id",
                        column: x => x.exercise_id,
                        principalSchema: "library",
                        principalTable: "exercises",
                        principalColumn: "exercise_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_exercise_muscles_muscles_muscle_id",
                        column: x => x.muscle_id,
                        principalSchema: "library",
                        principalTable: "muscles",
                        principalColumn: "muscle_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UQ___seed_installations",
                schema: "library",
                table: "__seed_installations",
                columns: new[] { "seed_domain", "seed_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_anatomy_mappings_muscle_id",
                schema: "library",
                table: "anatomy_mappings",
                column: "muscle_id");

            migrationBuilder.CreateIndex(
                name: "UQ_seed_anatomy_mappings_seed",
                schema: "library",
                table: "anatomy_mappings",
                column: "seed_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_seed_equipment_seed",
                schema: "library",
                table: "equipment",
                column: "seed_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_seed_exercise_categories_seed",
                schema: "library",
                table: "exercise_categories",
                column: "seed_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_exercise_equipment_equipment_id",
                schema: "library",
                table: "exercise_equipment",
                column: "equipment_id");

            migrationBuilder.CreateIndex(
                name: "UQ_seed_exercise_equipment",
                schema: "library",
                table: "exercise_equipment",
                columns: new[] { "exercise_id", "equipment_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_exercise_muscles_muscle_id",
                schema: "library",
                table: "exercise_muscles",
                column: "muscle_id");

            migrationBuilder.CreateIndex(
                name: "UQ_seed_exercise_muscles",
                schema: "library",
                table: "exercise_muscles",
                columns: new[] { "exercise_id", "muscle_id", "role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_exercises_category_id",
                schema: "library",
                table: "exercises",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_exercises_level_id",
                schema: "library",
                table: "exercises",
                column: "level_id");

            migrationBuilder.CreateIndex(
                name: "IX_exercises_primary_muscle_id",
                schema: "library",
                table: "exercises",
                column: "primary_muscle_id");

            migrationBuilder.CreateIndex(
                name: "UQ_seed_exercises_seed",
                schema: "library",
                table: "exercises",
                column: "seed_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_seed_exercises_slug",
                schema: "library",
                table: "exercises",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_seed_food_categories_seed",
                schema: "library",
                table: "food_categories",
                column: "seed_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_seed_food_units_code",
                schema: "library",
                table: "food_units",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_seed_food_units_seed",
                schema: "library",
                table: "food_units",
                column: "seed_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_foods_calculation_unit_id",
                schema: "library",
                table: "foods",
                column: "calculation_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_foods_food_category_id",
                schema: "library",
                table: "foods",
                column: "food_category_id");

            migrationBuilder.CreateIndex(
                name: "IX_foods_serving_unit_id",
                schema: "library",
                table: "foods",
                column: "serving_unit_id");

            migrationBuilder.CreateIndex(
                name: "UQ_seed_foods_seed",
                schema: "library",
                table: "foods",
                column: "seed_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_seed_foods_slug",
                schema: "library",
                table: "foods",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_core_gym_context_code",
                schema: "core",
                table: "gym_context",
                column: "gym_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_core_gym_context_control_plane_id",
                schema: "core",
                table: "gym_context",
                column: "control_plane_gym_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_auth_gym_users_status",
                schema: "auth",
                table: "gym_users",
                columns: new[] { "status", "control_plane_user_id" });

            migrationBuilder.CreateIndex(
                name: "UQ_auth_gym_users_control_plane_user",
                schema: "auth",
                table: "gym_users",
                column: "control_plane_user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_seed_levels_seed",
                schema: "library",
                table: "levels",
                column: "seed_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_seed_levels_type_code",
                schema: "library",
                table: "levels",
                columns: new[] { "level_type", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_seed_muscle_groups_seed",
                schema: "library",
                table: "muscle_groups",
                column: "seed_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_muscles_muscle_group_id",
                schema: "library",
                table: "muscles",
                column: "muscle_group_id");

            migrationBuilder.CreateIndex(
                name: "UQ_seed_muscles_seed",
                schema: "library",
                table: "muscles",
                column: "seed_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "__seed_installations",
                schema: "library");

            migrationBuilder.DropTable(
                name: "anatomy_mappings",
                schema: "library");

            migrationBuilder.DropTable(
                name: "events",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "exercise_equipment",
                schema: "library");

            migrationBuilder.DropTable(
                name: "exercise_muscles",
                schema: "library");

            migrationBuilder.DropTable(
                name: "foods",
                schema: "library");

            migrationBuilder.DropTable(
                name: "gym_context",
                schema: "core");

            migrationBuilder.DropTable(
                name: "gym_users",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "equipment",
                schema: "library");

            migrationBuilder.DropTable(
                name: "exercises",
                schema: "library");

            migrationBuilder.DropTable(
                name: "food_categories",
                schema: "library");

            migrationBuilder.DropTable(
                name: "food_units",
                schema: "library");

            migrationBuilder.DropTable(
                name: "exercise_categories",
                schema: "library");

            migrationBuilder.DropTable(
                name: "levels",
                schema: "library");

            migrationBuilder.DropTable(
                name: "muscles",
                schema: "library");

            migrationBuilder.DropTable(
                name: "muscle_groups",
                schema: "library");
        }
    }
}
