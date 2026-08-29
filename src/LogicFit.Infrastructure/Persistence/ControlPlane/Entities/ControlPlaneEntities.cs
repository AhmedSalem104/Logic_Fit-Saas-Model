namespace LogicFit.Infrastructure.Persistence.Entities;

public sealed class OrganizationEntity
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public ICollection<GymEntity> Gyms { get; } = new List<GymEntity>();
}

public sealed class GymEntity
{
    public Guid GymId { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Status { get; set; } = "provisioning";
    public string TimezoneName { get; set; } = "Africa/Cairo";
    public Guid? OwnerUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public OrganizationEntity? Organization { get; set; }
    public ICollection<GymDatabaseEntity> Databases { get; } = new List<GymDatabaseEntity>();
}

public sealed class GymDatabaseEntity
{
    public Guid GymDatabaseId { get; set; }
    public Guid GymId { get; set; }
    public Guid ServerId { get; set; }
    public string DatabaseName { get; set; } = string.Empty;
    public string Environment { get; set; } = "local";
    public string? SchemaVersion { get; set; }
    public string? SeedVersion { get; set; }
    public string Status { get; set; } = "pending";
    public string? ConnectionSecretRef { get; set; }
    public DateTime? LastHealthAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public GymEntity? Gym { get; set; }
    public ServerEntity? Server { get; set; }
}

public sealed class ServerEntity
{
    public Guid ServerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string ProviderKey { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public string HealthStatus { get; set; } = "healthy";
    public string? EndpointRef { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public ICollection<GymDatabaseEntity> GymDatabases { get; } = new List<GymDatabaseEntity>();
}

public sealed class FeatureFlagEntity
{
    public Guid FeatureFlagId { get; set; }
    public string FlagKey { get; set; } = string.Empty;
    public string ScopeType { get; set; } = "platform";
    public Guid? ScopeId { get; set; }
    public bool Enabled { get; set; }
    public string? ConfigJson { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class UserEntity
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public DateTime? LastLoginAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public ICollection<CredentialEntity> Credentials { get; } = new List<CredentialEntity>();
    public ICollection<SessionEntity> Sessions { get; } = new List<SessionEntity>();
    public ICollection<MfaFactorEntity> MfaFactors { get; } = new List<MfaFactorEntity>();
    public ICollection<PasswordResetTokenEntity> PasswordResetTokens { get; } = new List<PasswordResetTokenEntity>();
    public ICollection<MfaRecoveryCodeEntity> MfaRecoveryCodes { get; } = new List<MfaRecoveryCodeEntity>();
    public ICollection<UserGymRoleEntity> GymRoles { get; } = new List<UserGymRoleEntity>();
}

public sealed class CredentialEntity
{
    public Guid CredentialId { get; set; }
    public Guid UserId { get; set; }
    public string CredentialType { get; set; } = "password";
    public string SecretHash { get; set; } = string.Empty;
    public string SecretVersion { get; set; } = string.Empty;
    public DateTime? LastRotatedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public UserEntity? User { get; set; }
}

public sealed class SessionEntity
{
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public Guid? GymId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public DateTime? LastSeenAtUtc { get; set; }
    public string SessionKind { get; set; } = "staff";
    public bool MfaVerified { get; set; }
    public DateTime? IdleExpiresAtUtc { get; set; }
    public DateTime? AbsoluteExpiresAtUtc { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public UserEntity? User { get; set; }
    public GymEntity? Gym { get; set; }
}

public sealed class MfaFactorEntity
{
    public Guid MfaFactorId { get; set; }
    public Guid UserId { get; set; }
    public string FactorType { get; set; } = "totp";
    public string SecretRef { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public DateTime? VerifiedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public UserEntity? User { get; set; }
}

public sealed class PasswordResetTokenEntity
{
    public Guid PasswordResetTokenId { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? RequestedIp { get; set; }
    public string? RequestId { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public UserEntity? User { get; set; }
}

public sealed class MfaRecoveryCodeEntity
{
    public Guid RecoveryCodeId { get; set; }
    public Guid UserId { get; set; }
    public Guid? MfaFactorId { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public DateTime? UsedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public UserEntity? User { get; set; }
    public MfaFactorEntity? MfaFactor { get; set; }
}

public sealed class RoleEntity
{
    public Guid RoleId { get; set; }
    public string ScopeType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<RolePermissionEntity> RolePermissions { get; } = new List<RolePermissionEntity>();
    public ICollection<UserGymRoleEntity> UserGymRoles { get; } = new List<UserGymRoleEntity>();
}

public sealed class PermissionEntity
{
    public Guid PermissionId { get; set; }
    public string PermissionKey { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = "normal";
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<RolePermissionEntity> RolePermissions { get; } = new List<RolePermissionEntity>();
}

public sealed class RolePermissionEntity
{
    public Guid RolePermissionId { get; set; }
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
    public string? ScopeRuleJson { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public RoleEntity? Role { get; set; }
    public PermissionEntity? Permission { get; set; }
}

public sealed class UserGymRoleEntity
{
    public Guid AssignmentId { get; set; }
    public Guid UserId { get; set; }
    public Guid? GymId { get; set; }
    public Guid RoleId { get; set; }
    public string ScopeType { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public UserEntity? User { get; set; }
    public GymEntity? Gym { get; set; }
    public RoleEntity? Role { get; set; }
}

public sealed class MigrationDefinitionEntity
{
    public Guid MigrationDefinitionId { get; set; }
    public string MigrationKey { get; set; } = string.Empty;
    public string? FromVersion { get; set; }
    public string ToVersion { get; set; } = string.Empty;
    public string ChecksumSha256 { get; set; } = string.Empty;
    public string Status { get; set; } = "approved";
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class MigrationRunEntity
{
    public Guid MigrationRunId { get; set; }
    public string MigrationKey { get; set; } = string.Empty;
    public string Status { get; set; } = "running";
    public DateTime RequestedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? ErrorCode { get; set; }
}

public sealed class ProvisioningRunEntity
{
    public Guid ProvisioningRunId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid GymId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public Guid? OwnerUserId { get; set; }
    public string Status { get; set; } = "Requested";
    public string? CurrentStep { get; set; }
    public int AttemptNo { get; set; } = 1;
    public string IdempotencyKeyHash { get; set; } = string.Empty;
    public string RequestFingerprint { get; set; } = string.Empty;
    public Guid? ServerId { get; set; }
    public Guid? GymDatabaseId { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? FailureCategory { get; set; }
    public string? ErrorCode { get; set; }
    public string? SafeErrorMetadataJson { get; set; }
    // Internal retry replay markers. They contain hashes and step metadata only;
    // no request secret or credential is persisted here.
    public string? LastRetryIdempotencyKeyHash { get; set; }
    public string? LastRetryFingerprint { get; set; }
    public string? LastRetryFailedStep { get; set; }
    public string? LastRetryNextStep { get; set; }
    public int? LastRetryAttemptNo { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public OrganizationEntity? Organization { get; set; }
    public GymEntity? Gym { get; set; }
    public UserEntity? RequestedByUser { get; set; }
    public UserEntity? OwnerUser { get; set; }
    public ServerEntity? Server { get; set; }
    public GymDatabaseEntity? GymDatabase { get; set; }
    public ICollection<ProvisioningStepEntity> Steps { get; } = new List<ProvisioningStepEntity>();
}

public sealed class ProvisioningStepEntity
{
    public Guid ProvisioningStepId { get; set; }
    public Guid ProvisioningRunId { get; set; }
    public string StepKey { get; set; } = string.Empty;
    public int AttemptNo { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public bool Retryable { get; set; }
    public string? FailureCategory { get; set; }
    public string? ErrorCode { get; set; }
    public string? SafeMetadataJson { get; set; }

    public ProvisioningRunEntity? ProvisioningRun { get; set; }
}

public sealed class AuditEventEntity
{
    public Guid AuditEventId { get; set; }
    public string? RequestId { get; set; }
    public Guid? ActorUserId { get; set; }
    public string? ScopeType { get; set; }
    public Guid? ScopeId { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public Guid? TargetId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime OccurredAtUtc { get; set; }
}
