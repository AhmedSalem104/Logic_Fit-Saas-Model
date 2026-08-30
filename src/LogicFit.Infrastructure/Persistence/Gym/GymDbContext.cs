using System.Linq.Expressions;
using LogicFit.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence;

public sealed class GymDbContext(DbContextOptions<GymDbContext> options) : DbContext(options)
{
    public DbSet<GymContextEntity> GymContexts => Set<GymContextEntity>();
    public DbSet<GymUserEntity> GymUsers => Set<GymUserEntity>();
    public DbSet<GymAuditEventEntity> AuditEvents => Set<GymAuditEventEntity>();
    public DbSet<MemberEntity> Members => Set<MemberEntity>();
    public DbSet<MemberTimelineEventEntity> MemberTimelineEvents => Set<MemberTimelineEventEntity>();
    public DbSet<SeedInstallationEntity> SeedInstallations => Set<SeedInstallationEntity>();
    public DbSet<MuscleGroupEntity> MuscleGroups => Set<MuscleGroupEntity>();
    public DbSet<MuscleEntity> Muscles => Set<MuscleEntity>();
    public DbSet<EquipmentEntity> Equipment => Set<EquipmentEntity>();
    public DbSet<ExerciseCategoryEntity> ExerciseCategories => Set<ExerciseCategoryEntity>();
    public DbSet<LevelEntity> Levels => Set<LevelEntity>();
    public DbSet<ExerciseEntity> Exercises => Set<ExerciseEntity>();
    public DbSet<ExerciseMuscleEntity> ExerciseMuscles => Set<ExerciseMuscleEntity>();
    public DbSet<ExerciseEquipmentEntity> ExerciseEquipment => Set<ExerciseEquipmentEntity>();
    public DbSet<AnatomyMappingEntity> AnatomyMappings => Set<AnatomyMappingEntity>();
    public DbSet<FoodCategoryEntity> FoodCategories => Set<FoodCategoryEntity>();
    public DbSet<FoodUnitEntity> FoodUnits => Set<FoodUnitEntity>();
    public DbSet<FoodEntity> Foods => Set<FoodEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureGymContext(modelBuilder.Entity<GymContextEntity>());
        ConfigureGymUser(modelBuilder.Entity<GymUserEntity>());
        ConfigureAuditEvent(modelBuilder.Entity<GymAuditEventEntity>());
        ConfigureMember(modelBuilder.Entity<MemberEntity>());
        ConfigureMemberTimelineEvent(modelBuilder.Entity<MemberTimelineEventEntity>());
        ConfigureSeedInstallation(modelBuilder.Entity<SeedInstallationEntity>());
        ConfigureMuscleGroup(modelBuilder.Entity<MuscleGroupEntity>());
        ConfigureMuscle(modelBuilder.Entity<MuscleEntity>());
        ConfigureEquipment(modelBuilder.Entity<EquipmentEntity>());
        ConfigureExerciseCategory(modelBuilder.Entity<ExerciseCategoryEntity>());
        ConfigureLevel(modelBuilder.Entity<LevelEntity>());
        ConfigureExercise(modelBuilder.Entity<ExerciseEntity>());
        ConfigureExerciseMuscle(modelBuilder.Entity<ExerciseMuscleEntity>());
        ConfigureExerciseEquipment(modelBuilder.Entity<ExerciseEquipmentEntity>());
        ConfigureAnatomyMapping(modelBuilder.Entity<AnatomyMappingEntity>());
        ConfigureFoodCategory(modelBuilder.Entity<FoodCategoryEntity>());
        ConfigureFoodUnit(modelBuilder.Entity<FoodUnitEntity>());
        ConfigureFood(modelBuilder.Entity<FoodEntity>());
    }

    private static void ConfigureGymContext(EntityTypeBuilder<GymContextEntity> builder)
    {
        builder.ToTable("gym_context", "core");
        builder.HasKey(x => x.GymContextId).HasName("PK_core_gym_context");
        builder.Property(x => x.GymContextId).HasColumnName("gym_context_id").HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.ControlPlaneGymId).HasColumnName("control_plane_gym_id").IsRequired();
        builder.Property(x => x.GymCode).HasColumnName("gym_code").HasMaxLength(80).IsRequired();
        builder.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(160).IsRequired();
        builder.Property(x => x.TimezoneName).HasColumnName("timezone_name").HasMaxLength(80).HasDefaultValue("Africa/Cairo").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("provisioning").IsRequired();
        ConfigureAudit(builder, x => x.CreatedAtUtc, x => x.UpdatedAtUtc);
        builder.Property(x => x.RowVersion).HasColumnName("row_version").IsRowVersion();
        builder.HasIndex(x => x.ControlPlaneGymId).IsUnique().HasDatabaseName("UQ_core_gym_context_control_plane_id");
        builder.HasIndex(x => x.GymCode).IsUnique().HasDatabaseName("UQ_core_gym_context_code");
    }

    private static void ConfigureGymUser(EntityTypeBuilder<GymUserEntity> builder)
    {
        builder.ToTable("gym_users", "auth");
        builder.HasKey(x => x.GymUserId).HasName("PK_auth_gym_users");
        builder.Property(x => x.GymUserId).HasColumnName("gym_user_id").HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.ControlPlaneUserId).HasColumnName("control_plane_user_id").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("active").IsRequired();
        builder.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(160).IsRequired();
        builder.Property(x => x.LastPermissionSyncAtUtc).HasColumnName("last_permission_sync_at_utc").HasColumnType("datetime2(3)");
        ConfigureAudit(builder, x => x.CreatedAtUtc, x => x.UpdatedAtUtc);
        builder.Property(x => x.RowVersion).HasColumnName("row_version").IsRowVersion();
        builder.HasIndex(x => x.ControlPlaneUserId).IsUnique().HasDatabaseName("UQ_auth_gym_users_control_plane_user");
        builder.HasIndex(x => new { x.Status, x.ControlPlaneUserId }).HasDatabaseName("IX_auth_gym_users_status");
    }

    private static void ConfigureAuditEvent(EntityTypeBuilder<GymAuditEventEntity> builder)
    {
        builder.ToTable("events", "audit");
        builder.HasKey(x => x.AuditEventId).HasName("PK_gym_audit_events");
        builder.Property(x => x.AuditEventId).HasColumnName("audit_event_id").HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.RequestId).HasColumnName("request_id").HasMaxLength(80);
        builder.Property(x => x.ActorUserId).HasColumnName("actor_user_id");
        builder.Property(x => x.TargetType).HasColumnName("target_type").HasMaxLength(120).IsRequired();
        builder.Property(x => x.TargetId).HasColumnName("target_id");
        builder.Property(x => x.Action).HasColumnName("action").HasMaxLength(120).IsRequired();
        builder.Property(x => x.Result).HasColumnName("result").HasMaxLength(30).IsRequired();
        builder.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(500);
        builder.Property(x => x.MetadataJson).HasColumnName("metadata_json").HasColumnType("nvarchar(max)");
        builder.Property(x => x.OccurredAtUtc).HasColumnName("occurred_at_utc").HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
    }

    private static void ConfigureMember(EntityTypeBuilder<MemberEntity> builder)
    {
        builder.ToTable("members", "members", table => table.HasCheckConstraint(
            "CK_members_status",
            "[status] IN (N'ACTIVE', N'INACTIVE', N'ARCHIVED')"));
        builder.HasKey(x => x.MemberId).HasName("PK_members_members");
        builder.Property(x => x.MemberId).HasColumnName("member_id").HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.GymId).HasColumnName("gym_id").IsRequired();
        builder.Property(x => x.MemberCode).HasColumnName("member_code").HasMaxLength(40).IsRequired();
        builder.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(120).IsRequired();
        builder.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(30).IsRequired();
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(254);
        builder.Property(x => x.RegistrationDate).HasColumnName("registration_date").HasColumnType("date").IsRequired();
        builder.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(1000);
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(40).HasDefaultValue("ACTIVE").IsRequired();
        builder.Property(x => x.CreateIdempotencyKeyHash).HasColumnName("create_idempotency_key_hash").HasColumnType("char(64)").IsRequired();
        builder.Property(x => x.CreateRequestFingerprint).HasColumnName("create_request_fingerprint").HasColumnType("char(64)").IsRequired();
        builder.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(x => x.UpdatedByUserId).HasColumnName("updated_by_user_id");
        ConfigureAudit(builder, x => x.CreatedAtUtc, x => x.UpdatedAtUtc);
        builder.Property(x => x.RowVersion).HasColumnName("row_version").IsRowVersion();
        builder.HasIndex(x => new { x.GymId, x.MemberCode }).IsUnique().HasDatabaseName("UQ_members_member_code_gym");
        builder.HasIndex(x => new { x.GymId, x.CreateIdempotencyKeyHash }).IsUnique().HasDatabaseName("UQ_members_create_idempotency");
        builder.HasIndex(x => new { x.Status, x.CreatedAtUtc, x.MemberId }).HasDatabaseName("IX_members_status_created");
        builder.HasIndex(x => new { x.GymId, x.UpdatedAtUtc, x.MemberId }).HasDatabaseName("IX_members_gym_updated");
        builder.HasIndex(x => x.Phone).HasDatabaseName("IX_members_phone");
        builder.HasIndex(x => x.Email).HasDatabaseName("IX_members_email");
    }

    private static void ConfigureMemberTimelineEvent(EntityTypeBuilder<MemberTimelineEventEntity> builder)
    {
        builder.ToTable("timeline_events", "members");
        builder.HasKey(x => x.TimelineEventId).HasName("PK_members_timeline_events");
        builder.Property(x => x.TimelineEventId).HasColumnName("timeline_event_id").HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.MemberId).HasColumnName("member_id").IsRequired();
        builder.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(50).IsRequired();
        builder.Property(x => x.EventAtUtc).HasColumnName("event_at_utc").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.ActorUserId).HasColumnName("actor_user_id");
        builder.Property(x => x.SourceType).HasColumnName("source_type").HasMaxLength(80).IsRequired();
        builder.Property(x => x.SourceId).HasColumnName("source_id");
        builder.Property(x => x.Summary).HasColumnName("summary").HasMaxLength(240).IsRequired();
        builder.Property(x => x.MetadataJson).HasColumnName("metadata_json").HasMaxLength(2000).HasColumnType("nvarchar(2000)");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(x => new { x.MemberId, x.EventAtUtc, x.TimelineEventId }).HasDatabaseName("IX_members_timeline_member_event");
        builder.HasOne(x => x.Member).WithMany(x => x.TimelineEvents).HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureSeedInstallation(EntityTypeBuilder<SeedInstallationEntity> builder)
    {
        builder.ToTable("__seed_installations", "library");
        builder.HasKey(x => x.InstallationId).HasName("PK___seed_installations");
        builder.Property(x => x.InstallationId).HasColumnName("installation_id").HasDefaultValueSql("NEWID()");
        builder.Property(x => x.SeedDomain).HasColumnName("seed_domain").HasMaxLength(80).IsRequired();
        builder.Property(x => x.SeedVersion).HasColumnName("seed_version").HasMaxLength(40).IsRequired();
        builder.Property(x => x.SourceVersion).HasColumnName("source_version").HasMaxLength(120).IsRequired();
        builder.Property(x => x.ChecksumSha256).HasColumnName("checksum_sha256").HasColumnType("char(64)").IsRequired();
        builder.Property(x => x.RecordCount).HasColumnName("record_count").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(x => x.InstalledAtUtc).HasColumnName("installed_at_utc").HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(x => new { x.SeedDomain, x.SeedVersion }).IsUnique().HasDatabaseName("UQ___seed_installations");
    }

    private static void ConfigureMuscleGroup(EntityTypeBuilder<MuscleGroupEntity> builder)
    {
        builder.ToTable("muscle_groups", "library");
        ConfigureLibraryReference(builder);
        builder.HasKey(x => x.MuscleGroupId).HasName("PK_seed_muscle_groups");
        builder.Property(x => x.MuscleGroupId).HasColumnName("muscle_group_id").HasDefaultValueSql("NEWID()");
        builder.HasIndex(x => x.SeedKey).IsUnique().HasDatabaseName("UQ_seed_muscle_groups_seed");
    }

    private static void ConfigureMuscle(EntityTypeBuilder<MuscleEntity> builder)
    {
        builder.ToTable("muscles", "library");
        ConfigureLibraryReference(builder);
        builder.HasKey(x => x.MuscleId).HasName("PK_seed_muscles");
        builder.Property(x => x.MuscleId).HasColumnName("muscle_id").HasDefaultValueSql("NEWID()");
        builder.Property(x => x.MuscleGroupId).HasColumnName("muscle_group_id").IsRequired();
        builder.Property(x => x.NameAr).HasColumnName("name_ar").HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.SeedKey).IsUnique().HasDatabaseName("UQ_seed_muscles_seed");
        builder.HasOne(x => x.MuscleGroup).WithMany(x => x.Muscles).HasForeignKey(x => x.MuscleGroupId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureEquipment(EntityTypeBuilder<EquipmentEntity> builder)
    {
        builder.ToTable("equipment", "library");
        ConfigureLibraryReference(builder);
        builder.HasKey(x => x.EquipmentId).HasName("PK_seed_equipment");
        builder.Property(x => x.EquipmentId).HasColumnName("equipment_id").HasDefaultValueSql("NEWID()");
        builder.HasIndex(x => x.SeedKey).IsUnique().HasDatabaseName("UQ_seed_equipment_seed");
    }

    private static void ConfigureExerciseCategory(EntityTypeBuilder<ExerciseCategoryEntity> builder)
    {
        builder.ToTable("exercise_categories", "library");
        ConfigureLibraryReference(builder);
        builder.HasKey(x => x.CategoryId).HasName("PK_seed_exercise_categories");
        builder.Property(x => x.CategoryId).HasColumnName("category_id").HasDefaultValueSql("NEWID()");
        builder.HasIndex(x => x.SeedKey).IsUnique().HasDatabaseName("UQ_seed_exercise_categories_seed");
    }

    private static void ConfigureLevel(EntityTypeBuilder<LevelEntity> builder)
    {
        builder.ToTable("levels", "library");
        ConfigureLibraryReference(builder);
        builder.HasKey(x => x.LevelId).HasName("PK_seed_levels");
        builder.Property(x => x.LevelId).HasColumnName("level_id").HasDefaultValueSql("NEWID()");
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(40).IsRequired();
        builder.Property(x => x.LevelType).HasColumnName("level_type").HasMaxLength(40).IsRequired();
        builder.HasIndex(x => new { x.LevelType, x.Code }).IsUnique().HasDatabaseName("UQ_seed_levels_type_code");
        builder.HasIndex(x => x.SeedKey).IsUnique().HasDatabaseName("UQ_seed_levels_seed");
    }

    private static void ConfigureExercise(EntityTypeBuilder<ExerciseEntity> builder)
    {
        builder.ToTable("exercises", "library");
        ConfigureLibraryReference(builder);
        builder.HasKey(x => x.ExerciseId).HasName("PK_seed_exercises");
        builder.Property(x => x.ExerciseId).HasColumnName("exercise_id").HasDefaultValueSql("NEWID()");
        builder.Property(x => x.NameAr).HasColumnName("name_ar").HasMaxLength(300).IsRequired();
        builder.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(260).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasColumnType("nvarchar(max)");
        builder.Property(x => x.PrimaryMuscleId).HasColumnName("primary_muscle_id").IsRequired();
        builder.Property(x => x.DifficultyCode).HasColumnName("difficulty_code").HasMaxLength(40).IsRequired();
        builder.Property(x => x.CategoryId).HasColumnName("category_id").IsRequired();
        builder.Property(x => x.LevelId).HasColumnName("level_id").IsRequired();
        builder.Property(x => x.MovementPattern).HasColumnName("movement_pattern").HasMaxLength(120);
        builder.Property(x => x.InstructionsAr).HasColumnName("instructions_ar").HasColumnType("nvarchar(max)");
        builder.Property(x => x.InstructionsEn).HasColumnName("instructions_en").HasColumnType("nvarchar(max)");
        builder.Property(x => x.TipsAr).HasColumnName("tips_ar").HasColumnType("nvarchar(max)");
        builder.Property(x => x.CommonMistakesAr).HasColumnName("common_mistakes_ar").HasColumnType("nvarchar(max)");
        builder.Property(x => x.MediaJson).HasColumnName("media_json").HasColumnType("nvarchar(max)");
        builder.Property(x => x.LegacyStatus).HasColumnName("legacy_status").HasMaxLength(60);
        builder.HasIndex(x => x.SeedKey).IsUnique().HasDatabaseName("UQ_seed_exercises_seed");
        builder.HasIndex(x => x.Slug).IsUnique().HasDatabaseName("UQ_seed_exercises_slug");
        builder.HasOne(x => x.PrimaryMuscle).WithMany().HasForeignKey(x => x.PrimaryMuscleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Level).WithMany().HasForeignKey(x => x.LevelId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureExerciseMuscle(EntityTypeBuilder<ExerciseMuscleEntity> builder)
    {
        builder.ToTable("exercise_muscles", "library");
        builder.HasKey(x => x.ExerciseMuscleId).HasName("PK_seed_exercise_muscles");
        builder.Property(x => x.ExerciseMuscleId).HasColumnName("exercise_muscle_id").HasDefaultValueSql("NEWID()");
        builder.Property(x => x.ExerciseId).HasColumnName("exercise_id").IsRequired();
        builder.Property(x => x.MuscleId).HasColumnName("muscle_id").IsRequired();
        builder.Property(x => x.Role).HasColumnName("role").HasMaxLength(30).IsRequired();
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").IsRequired();
        builder.Property(x => x.PayloadJson).HasColumnName("payload_json").HasColumnType("nvarchar(max)").IsRequired();
        builder.HasIndex(x => new { x.ExerciseId, x.MuscleId, x.Role }).IsUnique().HasDatabaseName("UQ_seed_exercise_muscles");
        builder.HasOne(x => x.Exercise).WithMany(x => x.Muscles).HasForeignKey(x => x.ExerciseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Muscle).WithMany().HasForeignKey(x => x.MuscleId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureExerciseEquipment(EntityTypeBuilder<ExerciseEquipmentEntity> builder)
    {
        builder.ToTable("exercise_equipment", "library");
        builder.HasKey(x => x.ExerciseEquipmentId).HasName("PK_seed_exercise_equipment");
        builder.Property(x => x.ExerciseEquipmentId).HasColumnName("exercise_equipment_id").HasDefaultValueSql("NEWID()");
        builder.Property(x => x.ExerciseId).HasColumnName("exercise_id").IsRequired();
        builder.Property(x => x.EquipmentId).HasColumnName("equipment_id").IsRequired();
        builder.Property(x => x.IsRequired).HasColumnName("is_required");
        builder.Property(x => x.PayloadJson).HasColumnName("payload_json").HasColumnType("nvarchar(max)").IsRequired();
        builder.HasIndex(x => new { x.ExerciseId, x.EquipmentId }).IsUnique().HasDatabaseName("UQ_seed_exercise_equipment");
        builder.HasOne(x => x.Exercise).WithMany(x => x.Equipment).HasForeignKey(x => x.ExerciseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Equipment).WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureAnatomyMapping(EntityTypeBuilder<AnatomyMappingEntity> builder)
    {
        builder.ToTable("anatomy_mappings", "library");
        ConfigureLibraryReference(builder);
        builder.HasKey(x => x.AnatomyMappingId).HasName("PK_seed_anatomy_mappings");
        builder.Property(x => x.AnatomyMappingId).HasColumnName("anatomy_mapping_id").HasDefaultValueSql("NEWID()");
        builder.Property(x => x.MuscleId).HasColumnName("muscle_id").IsRequired();
        builder.Property(x => x.BodyRegion).HasColumnName("body_region").HasMaxLength(120).IsRequired();
        builder.Property(x => x.ViewCode).HasColumnName("view_code").HasMaxLength(80).IsRequired();
        builder.Property(x => x.AssetKey).HasColumnName("asset_key").HasMaxLength(240).IsRequired();
        builder.HasIndex(x => x.SeedKey).IsUnique().HasDatabaseName("UQ_seed_anatomy_mappings_seed");
        builder.HasOne(x => x.Muscle).WithMany().HasForeignKey(x => x.MuscleId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureFoodCategory(EntityTypeBuilder<FoodCategoryEntity> builder)
    {
        builder.ToTable("food_categories", "library");
        ConfigureLibraryReference(builder);
        builder.HasKey(x => x.FoodCategoryId).HasName("PK_seed_food_categories");
        builder.Property(x => x.FoodCategoryId).HasColumnName("food_category_id").HasDefaultValueSql("NEWID()");
        builder.HasIndex(x => x.SeedKey).IsUnique().HasDatabaseName("UQ_seed_food_categories_seed");
    }

    private static void ConfigureFoodUnit(EntityTypeBuilder<FoodUnitEntity> builder)
    {
        builder.ToTable("food_units", "library");
        ConfigureLibraryReference(builder);
        builder.HasKey(x => x.FoodUnitId).HasName("PK_seed_food_units");
        builder.Property(x => x.FoodUnitId).HasColumnName("food_unit_id").HasDefaultValueSql("NEWID()");
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(40).IsRequired();
        builder.Property(x => x.Dimension).HasColumnName("dimension").HasMaxLength(40).IsRequired();
        builder.Property(x => x.BaseQuantity).HasColumnName("base_quantity").HasColumnType("decimal(12,3)").IsRequired();
        builder.Property(x => x.BaseUnitCode).HasColumnName("base_unit_code").HasMaxLength(40).IsRequired();
        builder.HasIndex(x => x.SeedKey).IsUnique().HasDatabaseName("UQ_seed_food_units_seed");
        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UQ_seed_food_units_code");
    }

    private static void ConfigureFood(EntityTypeBuilder<FoodEntity> builder)
    {
        builder.ToTable("foods", "library");
        ConfigureLibraryReference(builder);
        builder.HasKey(x => x.FoodId).HasName("PK_seed_foods");
        builder.Property(x => x.FoodId).HasColumnName("food_id").HasDefaultValueSql("NEWID()");
        builder.Property(x => x.FoodCategoryId).HasColumnName("food_category_id").IsRequired();
        builder.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(300).IsRequired();
        builder.Property(x => x.ServingQuantity).HasColumnName("serving_quantity").HasColumnType("decimal(12,3)").IsRequired();
        builder.Property(x => x.ServingUnitId).HasColumnName("serving_unit_id").IsRequired();
        builder.Property(x => x.CalculationQuantity).HasColumnName("calculation_quantity").HasColumnType("decimal(12,3)").IsRequired();
        builder.Property(x => x.CalculationUnitId).HasColumnName("calculation_unit_id").IsRequired();
        builder.Property(x => x.Calories).HasColumnName("calories").HasColumnType("decimal(12,3)").IsRequired();
        builder.Property(x => x.Protein).HasColumnName("protein").HasColumnType("decimal(12,3)").IsRequired();
        builder.Property(x => x.Carbs).HasColumnName("carbs").HasColumnType("decimal(12,3)").IsRequired();
        builder.Property(x => x.Fat).HasColumnName("fat").HasColumnType("decimal(12,3)").IsRequired();
        builder.Property(x => x.Fiber).HasColumnName("fiber").HasColumnType("decimal(12,3)");
        builder.Property(x => x.Sugar).HasColumnName("sugar").HasColumnType("decimal(12,3)");
        builder.Property(x => x.Sodium).HasColumnName("sodium").HasColumnType("decimal(12,3)");
        builder.HasIndex(x => x.SeedKey).IsUnique().HasDatabaseName("UQ_seed_foods_seed");
        builder.HasIndex(x => x.Slug).IsUnique().HasDatabaseName("UQ_seed_foods_slug");
        builder.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.FoodCategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ServingUnit).WithMany().HasForeignKey(x => x.ServingUnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CalculationUnit).WithMany().HasForeignKey(x => x.CalculationUnitId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureLibraryReference<T>(EntityTypeBuilder<T> builder)
        where T : class, ILibraryReference
    {
        builder.Property(x => x.SeedKey).HasColumnName("seed_key").HasMaxLength(160).IsRequired();
        builder.Property(x => x.NameAr).HasColumnName("name_ar").HasMaxLength(200);
        builder.Property(x => x.NameEn).HasColumnName("name_en").HasMaxLength(300).IsRequired();
        builder.Property(x => x.Active).HasColumnName("active").IsRequired();
        builder.Property(x => x.RecordScope).HasColumnName("record_scope").HasMaxLength(40).IsRequired();
        builder.Property(x => x.Source).HasColumnName("source").HasMaxLength(80).IsRequired();
        builder.Property(x => x.SourceVersion).HasColumnName("source_version").HasMaxLength(120).IsRequired();
        builder.Property(x => x.PayloadJson).HasColumnName("payload_json").HasColumnType("nvarchar(max)").IsRequired();
    }

    private static void ConfigureAudit<T>(
        EntityTypeBuilder<T> builder,
        Expression<Func<T, DateTime>> created,
        Expression<Func<T, DateTime>> updated)
        where T : class
    {
        builder.Property(created).HasColumnName("created_at_utc").HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
        builder.Property(updated).HasColumnName("updated_at_utc").HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
    }
}
