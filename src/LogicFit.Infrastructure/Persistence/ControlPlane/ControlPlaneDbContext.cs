using System.Linq.Expressions;
using LogicFit.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence;

public sealed class ControlPlaneDbContext(DbContextOptions<ControlPlaneDbContext> options) : DbContext(options)
{
    public DbSet<OrganizationEntity> Organizations => Set<OrganizationEntity>();
    public DbSet<GymEntity> Gyms => Set<GymEntity>();
    public DbSet<GymDatabaseEntity> GymDatabases => Set<GymDatabaseEntity>();
    public DbSet<FeatureFlagEntity> FeatureFlags => Set<FeatureFlagEntity>();
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<CredentialEntity> Credentials => Set<CredentialEntity>();
    public DbSet<SessionEntity> Sessions => Set<SessionEntity>();
    public DbSet<MfaFactorEntity> MfaFactors => Set<MfaFactorEntity>();
    public DbSet<PasswordResetTokenEntity> PasswordResetTokens => Set<PasswordResetTokenEntity>();
    public DbSet<MfaRecoveryCodeEntity> MfaRecoveryCodes => Set<MfaRecoveryCodeEntity>();
    public DbSet<RoleEntity> Roles => Set<RoleEntity>();
    public DbSet<PermissionEntity> Permissions => Set<PermissionEntity>();
    public DbSet<RolePermissionEntity> RolePermissions => Set<RolePermissionEntity>();
    public DbSet<UserGymRoleEntity> UserGymRoles => Set<UserGymRoleEntity>();
    public DbSet<MigrationDefinitionEntity> MigrationDefinitions => Set<MigrationDefinitionEntity>();
    public DbSet<MigrationRunEntity> MigrationRuns => Set<MigrationRunEntity>();
    public DbSet<AuditEventEntity> AuditEvents => Set<AuditEventEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureOrganization(modelBuilder.Entity<OrganizationEntity>());
        ConfigureGym(modelBuilder.Entity<GymEntity>());
        ConfigureGymDatabase(modelBuilder.Entity<GymDatabaseEntity>());
        ConfigureFeatureFlag(modelBuilder.Entity<FeatureFlagEntity>());
        ConfigureUser(modelBuilder.Entity<UserEntity>());
        ConfigureCredential(modelBuilder.Entity<CredentialEntity>());
        ConfigureSession(modelBuilder.Entity<SessionEntity>());
        ConfigureMfaFactor(modelBuilder.Entity<MfaFactorEntity>());
        ConfigurePasswordResetToken(modelBuilder.Entity<PasswordResetTokenEntity>());
        ConfigureMfaRecoveryCode(modelBuilder.Entity<MfaRecoveryCodeEntity>());
        ConfigureRole(modelBuilder.Entity<RoleEntity>());
        ConfigurePermission(modelBuilder.Entity<PermissionEntity>());
        ConfigureRolePermission(modelBuilder.Entity<RolePermissionEntity>());
        ConfigureUserGymRole(modelBuilder.Entity<UserGymRoleEntity>());
        ConfigureMigrationDefinition(modelBuilder.Entity<MigrationDefinitionEntity>());
        ConfigureMigrationRun(modelBuilder.Entity<MigrationRunEntity>());
        ConfigureAuditEvent(modelBuilder.Entity<AuditEventEntity>());
    }

    private static void ConfigureOrganization(EntityTypeBuilder<OrganizationEntity> builder)
    {
        builder.ToTable("organizations", "platform");
        builder.HasKey(x => x.OrganizationId).HasName("PK_platform_organizations");
        builder.Property(x => x.OrganizationId).HasColumnName("organization_id").HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(160).IsRequired();
        builder.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(120).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("active").IsRequired();
        ConfigureAudit(builder, x => x.CreatedAtUtc, x => x.UpdatedAtUtc);
        builder.Property(x => x.RowVersion).HasColumnName("row_version").IsRowVersion();
        builder.HasIndex(x => x.Slug).IsUnique().HasDatabaseName("UQ_platform_organizations_slug");
    }

    private static void ConfigureGym(EntityTypeBuilder<GymEntity> builder)
    {
        builder.ToTable("gyms", "platform");
        builder.HasKey(x => x.GymId).HasName("PK_platform_gyms");
        builder.Property(x => x.GymId).HasColumnName("gym_id").HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(160).IsRequired();
        builder.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(120).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("provisioning").IsRequired();
        builder.Property(x => x.TimezoneName).HasColumnName("timezone_name").HasMaxLength(80).HasDefaultValue("Africa/Cairo").IsRequired();
        ConfigureAudit(builder, x => x.CreatedAtUtc, x => x.UpdatedAtUtc);
        builder.Property(x => x.RowVersion).HasColumnName("row_version").IsRowVersion();
        builder.HasIndex(x => new { x.OrganizationId, x.Slug }).IsUnique().HasDatabaseName("UQ_platform_gyms_org_slug");
        builder.HasOne(x => x.Organization).WithMany(x => x.Gyms).HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureGymDatabase(EntityTypeBuilder<GymDatabaseEntity> builder)
    {
        builder.ToTable("gym_databases", "platform");
        builder.HasKey(x => x.GymDatabaseId).HasName("PK_platform_gym_databases");
        builder.Property(x => x.GymDatabaseId).HasColumnName("gym_database_id").HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.GymId).HasColumnName("gym_id").IsRequired();
        builder.Property(x => x.DatabaseName).HasColumnName("database_name").HasMaxLength(128).IsRequired();
        builder.Property(x => x.Environment).HasColumnName("environment").HasMaxLength(30).HasDefaultValue("local").IsRequired();
        builder.Property(x => x.SchemaVersion).HasColumnName("schema_version").HasMaxLength(80);
        builder.Property(x => x.SeedVersion).HasColumnName("seed_version").HasMaxLength(80);
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("pending").IsRequired();
        builder.Property(x => x.ConnectionSecretRef).HasColumnName("connection_secret_ref").HasMaxLength(240);
        builder.Property(x => x.LastHealthAtUtc).HasColumnName("last_health_at_utc").HasColumnType("datetime2(3)");
        ConfigureAudit(builder, x => x.CreatedAtUtc, x => x.UpdatedAtUtc);
        builder.Property(x => x.RowVersion).HasColumnName("row_version").IsRowVersion();
        builder.HasIndex(x => new { x.Environment, x.DatabaseName }).IsUnique().HasDatabaseName("UQ_platform_gym_databases_name");
        builder.HasOne(x => x.Gym).WithMany(x => x.Databases).HasForeignKey(x => x.GymId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureFeatureFlag(EntityTypeBuilder<FeatureFlagEntity> builder)
    {
        builder.ToTable("feature_flags", "platform");
        builder.HasKey(x => x.FeatureFlagId).HasName("PK_platform_feature_flags");
        builder.Property(x => x.FeatureFlagId).HasColumnName("feature_flag_id").HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.FlagKey).HasColumnName("flag_key").HasMaxLength(160).IsRequired();
        builder.Property(x => x.ScopeType).HasColumnName("scope_type").HasMaxLength(30).IsRequired();
        builder.Property(x => x.ScopeId).HasColumnName("scope_id");
        builder.Property(x => x.Enabled).HasColumnName("enabled").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.ConfigJson).HasColumnName("config_json").HasColumnType("nvarchar(max)");
        ConfigureAudit(builder, x => x.CreatedAtUtc, x => x.UpdatedAtUtc);
        builder.HasIndex(x => new { x.FlagKey, x.ScopeType, x.ScopeId }).IsUnique().HasDatabaseName("UQ_platform_feature_flags_scope");
    }

    private static void ConfigureUser(EntityTypeBuilder<UserEntity> builder)
    {
        builder.ToTable("users", "iam");
        builder.HasKey(x => x.UserId).HasName("PK_iam_users");
        builder.Property(x => x.UserId).HasColumnName("user_id").HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(320).IsRequired();
        builder.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(160).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("active").IsRequired();
        builder.Property(x => x.LastLoginAtUtc).HasColumnName("last_login_at_utc").HasColumnType("datetime2(3)");
        ConfigureAudit(builder, x => x.CreatedAtUtc, x => x.UpdatedAtUtc);
        builder.Property(x => x.RowVersion).HasColumnName("row_version").IsRowVersion();
        builder.HasIndex(x => x.Email).IsUnique().HasDatabaseName("UQ_iam_users_email");
    }

    private static void ConfigureCredential(EntityTypeBuilder<CredentialEntity> builder)
    {
        builder.ToTable("credentials", "iam");
        builder.HasKey(x => x.CredentialId).HasName("PK_iam_credentials");
        builder.Property(x => x.CredentialId).HasColumnName("credential_id").HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.CredentialType).HasColumnName("credential_type").HasMaxLength(40).IsRequired();
        builder.Property(x => x.SecretHash).HasColumnName("secret_hash").HasMaxLength(255).IsRequired();
        builder.Property(x => x.SecretVersion).HasColumnName("secret_version").HasMaxLength(40).IsRequired();
        builder.Property(x => x.LastRotatedAtUtc).HasColumnName("last_rotated_at_utc").HasColumnType("datetime2(3)");
        ConfigureAudit(builder, x => x.CreatedAtUtc, x => x.UpdatedAtUtc);
        builder.HasIndex(x => new { x.UserId, x.CredentialType }).IsUnique().HasDatabaseName("UQ_iam_credentials_type");
        builder.HasOne(x => x.User).WithMany(x => x.Credentials).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureSession(EntityTypeBuilder<SessionEntity> builder)
    {
        builder.ToTable("sessions", "iam");
        builder.HasKey(x => x.SessionId).HasName("PK_iam_sessions");
        builder.Property(x => x.SessionId).HasColumnName("session_id").HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.GymId).HasColumnName("gym_id");
        builder.Property(x => x.TokenHash).HasColumnName("token_hash").HasColumnType("char(64)").IsRequired();
        builder.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at_utc").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.RevokedAtUtc).HasColumnName("revoked_at_utc").HasColumnType("datetime2(3)");
        builder.Property(x => x.LastSeenAtUtc).HasColumnName("last_seen_at_utc").HasColumnType("datetime2(3)");
        builder.Property(x => x.SessionKind).HasColumnName("session_kind").HasMaxLength(30).HasDefaultValue("staff").IsRequired();
        builder.Property(x => x.MfaVerified).HasColumnName("mfa_verified").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.IdleExpiresAtUtc).HasColumnName("idle_expires_at_utc").HasColumnType("datetime2(3)");
        builder.Property(x => x.AbsoluteExpiresAtUtc).HasColumnName("absolute_expires_at_utc").HasColumnType("datetime2(3)");
        builder.Property(x => x.UserAgent).HasColumnName("user_agent").HasMaxLength(512);
        builder.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(64);
        ConfigureAudit(builder, x => x.CreatedAtUtc, x => x.UpdatedAtUtc);
        builder.HasIndex(x => x.TokenHash).IsUnique().HasDatabaseName("UQ_iam_sessions_token");
        builder.HasIndex(x => new { x.UserId, x.RevokedAtUtc, x.ExpiresAtUtc }).HasDatabaseName("IX_iam_sessions_user_active");
        builder.HasIndex(x => new { x.GymId, x.UserId, x.RevokedAtUtc }).HasDatabaseName("IX_iam_sessions_gym");
        builder.HasOne(x => x.User).WithMany(x => x.Sessions).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Gym).WithMany().HasForeignKey(x => x.GymId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureMfaFactor(EntityTypeBuilder<MfaFactorEntity> builder)
    {
        builder.ToTable("mfa_factors", "iam");
        builder.HasKey(x => x.MfaFactorId).HasName("PK_iam_mfa_factors");
        builder.Property(x => x.MfaFactorId).HasColumnName("mfa_factor_id").HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.FactorType).HasColumnName("factor_type").HasMaxLength(30).HasDefaultValue("totp").IsRequired();
        builder.Property(x => x.SecretRef).HasColumnName("secret_ref").HasMaxLength(240).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("pending").IsRequired();
        builder.Property(x => x.VerifiedAtUtc).HasColumnName("verified_at_utc").HasColumnType("datetime2(3)");
        ConfigureAudit(builder, x => x.CreatedAtUtc, x => x.UpdatedAtUtc);
        builder.HasOne(x => x.User).WithMany(x => x.MfaFactors).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigurePasswordResetToken(EntityTypeBuilder<PasswordResetTokenEntity> builder)
    {
        builder.ToTable("password_reset_tokens", "iam");
        builder.HasKey(x => x.PasswordResetTokenId).HasName("PK_iam_password_reset_tokens");
        builder.Property(x => x.PasswordResetTokenId).HasColumnName("password_reset_token_id").HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.TokenHash).HasColumnName("token_hash").HasColumnType("char(64)").IsRequired();
        builder.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at_utc").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.UsedAtUtc).HasColumnName("used_at_utc").HasColumnType("datetime2(3)");
        builder.Property(x => x.RevokedAtUtc).HasColumnName("revoked_at_utc").HasColumnType("datetime2(3)");
        builder.Property(x => x.RequestedIp).HasColumnName("requested_ip").HasMaxLength(64);
        builder.Property(x => x.RequestId).HasColumnName("request_id").HasMaxLength(80);
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(x => x.TokenHash).IsUnique().HasDatabaseName("UQ_iam_password_reset_tokens_hash");
        builder.HasIndex(x => new { x.UserId, x.ExpiresAtUtc, x.UsedAtUtc, x.RevokedAtUtc }).HasDatabaseName("IX_iam_password_reset_active");
        builder.HasOne(x => x.User).WithMany(x => x.PasswordResetTokens).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureMfaRecoveryCode(EntityTypeBuilder<MfaRecoveryCodeEntity> builder)
    {
        builder.ToTable("mfa_recovery_codes", "iam");
        builder.HasKey(x => x.RecoveryCodeId).HasName("PK_iam_mfa_recovery_codes");
        builder.Property(x => x.RecoveryCodeId).HasColumnName("recovery_code_id").HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.MfaFactorId).HasColumnName("mfa_factor_id");
        builder.Property(x => x.CodeHash).HasColumnName("code_hash").HasColumnType("char(64)").IsRequired();
        builder.Property(x => x.UsedAtUtc).HasColumnName("used_at_utc").HasColumnType("datetime2(3)");
        builder.Property(x => x.RevokedAtUtc).HasColumnName("revoked_at_utc").HasColumnType("datetime2(3)");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(x => x.CodeHash).IsUnique().HasDatabaseName("UQ_iam_mfa_recovery_codes_hash");
        builder.HasIndex(x => new { x.UserId, x.MfaFactorId, x.UsedAtUtc, x.RevokedAtUtc }).HasDatabaseName("IX_iam_recovery_codes_active");
        builder.HasOne(x => x.User).WithMany(x => x.MfaRecoveryCodes).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.MfaFactor).WithMany().HasForeignKey(x => x.MfaFactorId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureRole(EntityTypeBuilder<RoleEntity> builder)
    {
        builder.ToTable("roles", "iam");
        builder.HasKey(x => x.RoleId).HasName("PK_iam_roles");
        builder.Property(x => x.RoleId).HasColumnName("role_id").HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.ScopeType).HasColumnName("scope_type").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("active").IsRequired();
        ConfigureAudit(builder, x => x.CreatedAtUtc, x => x.UpdatedAtUtc);
        builder.HasIndex(x => new { x.ScopeType, x.Name }).IsUnique().HasDatabaseName("UQ_iam_roles_scope_name");
    }

    private static void ConfigurePermission(EntityTypeBuilder<PermissionEntity> builder)
    {
        builder.ToTable("permissions", "iam");
        builder.HasKey(x => x.PermissionId).HasName("PK_iam_permissions");
        builder.Property(x => x.PermissionId).HasColumnName("permission_id").HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.PermissionKey).HasColumnName("permission_key").HasMaxLength(160).IsRequired();
        builder.Property(x => x.Domain).HasColumnName("domain").HasMaxLength(80).IsRequired();
        builder.Property(x => x.Action).HasColumnName("action").HasMaxLength(80).IsRequired();
        builder.Property(x => x.RiskLevel).HasColumnName("risk_level").HasMaxLength(20).HasDefaultValue("normal").IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(500).IsRequired();
        ConfigureAudit(builder, x => x.CreatedAtUtc, x => x.UpdatedAtUtc);
        builder.HasIndex(x => x.PermissionKey).IsUnique().HasDatabaseName("UQ_iam_permissions_key");
    }

    private static void ConfigureRolePermission(EntityTypeBuilder<RolePermissionEntity> builder)
    {
        builder.ToTable("role_permissions", "iam");
        builder.HasKey(x => x.RolePermissionId).HasName("PK_iam_role_permissions");
        builder.Property(x => x.RolePermissionId).HasColumnName("role_permission_id").HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.RoleId).HasColumnName("role_id").IsRequired();
        builder.Property(x => x.PermissionId).HasColumnName("permission_id").IsRequired();
        builder.Property(x => x.ScopeRuleJson).HasColumnName("scope_rule_json").HasColumnType("nvarchar(max)");
        ConfigureAudit(builder, x => x.CreatedAtUtc, x => x.UpdatedAtUtc);
        builder.HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique().HasDatabaseName("UQ_iam_role_permissions_pair");
        builder.HasOne(x => x.Role).WithMany(x => x.RolePermissions).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Permission).WithMany(x => x.RolePermissions).HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureUserGymRole(EntityTypeBuilder<UserGymRoleEntity> builder)
    {
        builder.ToTable("user_gym_roles", "iam");
        builder.HasKey(x => x.AssignmentId).HasName("PK_iam_user_gym_roles");
        builder.Property(x => x.AssignmentId).HasColumnName("assignment_id").HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.GymId).HasColumnName("gym_id");
        builder.Property(x => x.RoleId).HasColumnName("role_id").IsRequired();
        builder.Property(x => x.ScopeType).HasColumnName("scope_type").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("active").IsRequired();
        ConfigureAudit(builder, x => x.CreatedAtUtc, x => x.UpdatedAtUtc);
        builder.Property(x => x.RowVersion).HasColumnName("row_version").IsRowVersion();
        builder.HasIndex(x => new { x.UserId, x.GymId, x.RoleId }).IsUnique().HasDatabaseName("UX_iam_user_gym_roles_gym").HasFilter("[scope_type] = N'gym' AND [status] = N'active'");
        builder.HasIndex(x => new { x.UserId, x.RoleId }).IsUnique().HasDatabaseName("UX_iam_user_gym_roles_platform").HasFilter("[scope_type] = N'platform' AND [status] = N'active'");
        builder.HasIndex(x => new { x.UserId, x.ScopeType, x.GymId, x.Status }).HasDatabaseName("IX_iam_user_gym_roles_scope");
        builder.HasOne(x => x.User).WithMany(x => x.GymRoles).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Gym).WithMany().HasForeignKey(x => x.GymId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Role).WithMany(x => x.UserGymRoles).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureMigrationDefinition(EntityTypeBuilder<MigrationDefinitionEntity> builder)
    {
        builder.ToTable("definitions", "migrations");
        builder.HasKey(x => x.MigrationDefinitionId).HasName("PK_migrations_definitions");
        builder.Property(x => x.MigrationDefinitionId).HasColumnName("migration_definition_id").HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.MigrationKey).HasColumnName("migration_key").HasMaxLength(120).IsRequired();
        builder.Property(x => x.FromVersion).HasColumnName("from_version").HasMaxLength(80);
        builder.Property(x => x.ToVersion).HasColumnName("to_version").HasMaxLength(80).IsRequired();
        builder.Property(x => x.ChecksumSha256).HasColumnName("checksum_sha256").HasColumnType("char(64)").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("approved").IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(x => x.MigrationKey).IsUnique().HasDatabaseName("UQ_migrations_definitions_key");
    }

    private static void ConfigureMigrationRun(EntityTypeBuilder<MigrationRunEntity> builder)
    {
        builder.ToTable("runs", "migrations");
        builder.HasKey(x => x.MigrationRunId).HasName("PK_migrations_runs");
        builder.Property(x => x.MigrationRunId).HasColumnName("migration_run_id").HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.MigrationKey).HasColumnName("migration_key").HasMaxLength(120).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("running").IsRequired();
        builder.Property(x => x.RequestedAtUtc).HasColumnName("requested_at_utc").HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.CompletedAtUtc).HasColumnName("completed_at_utc").HasColumnType("datetime2(3)");
        builder.Property(x => x.ErrorCode).HasColumnName("error_code").HasMaxLength(80);
    }

    private static void ConfigureAuditEvent(EntityTypeBuilder<AuditEventEntity> builder)
    {
        builder.ToTable("events", "audit");
        builder.HasKey(x => x.AuditEventId).HasName("PK_audit_events");
        builder.Property(x => x.AuditEventId).HasColumnName("audit_event_id").HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.RequestId).HasColumnName("request_id").HasMaxLength(80);
        builder.Property(x => x.ActorUserId).HasColumnName("actor_user_id");
        builder.Property(x => x.ScopeType).HasColumnName("scope_type").HasMaxLength(30);
        builder.Property(x => x.ScopeId).HasColumnName("scope_id");
        builder.Property(x => x.TargetType).HasColumnName("target_type").HasMaxLength(120).IsRequired();
        builder.Property(x => x.TargetId).HasColumnName("target_id");
        builder.Property(x => x.Action).HasColumnName("action").HasMaxLength(120).IsRequired();
        builder.Property(x => x.Result).HasColumnName("result").HasMaxLength(30).IsRequired();
        builder.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(500);
        builder.Property(x => x.MetadataJson).HasColumnName("metadata_json").HasColumnType("nvarchar(max)");
        builder.Property(x => x.OccurredAtUtc).HasColumnName("occurred_at_utc").HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
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
