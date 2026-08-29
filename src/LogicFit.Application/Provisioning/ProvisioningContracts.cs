namespace LogicFit.Application;

public sealed record ProvisioningOrganizationRequest(string? Name, string? Slug);
public sealed record ProvisioningGymRequest(string? Name, string? Slug, string? TimezoneName);
public sealed record ProvisioningServerTargetRequest(Guid ServerId);
public sealed record ProvisioningOwnerRequest(string? Email, string? DisplayName, string? InitialPassword);
public sealed record ProvisioningRequest(
    ProvisioningOrganizationRequest? Organization,
    ProvisioningGymRequest? Gym,
    ProvisioningServerTargetRequest? ServerTarget,
    ProvisioningOwnerRequest? Owner);

public sealed record ProvisioningRetryRequest(string? Reason);

public sealed record ProvisioningAcceptedDto(
    Guid OperationId,
    Guid OrganizationId,
    Guid GymId,
    string Status,
    string? CurrentStep,
    DateTime RequestedAtUtc,
    string StatusUrl);

public sealed record ProvisioningServerDto(
    Guid ServerId,
    string Environment,
    string Status);

public sealed record ProvisioningDatabaseDto(
    Guid DatabaseId,
    string DatabaseName,
    string Status,
    string? SchemaVersion,
    string? SeedVersion);

public sealed record ProvisioningFailureDto(
    string FailureCategory,
    string ErrorCode,
    string FailedStep,
    DateTime OccurredAtUtc,
    bool Retryable);

public sealed record ProvisioningStepDto(
    string StepKey,
    string Status,
    int AttemptNo,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    bool Retryable,
    string? FailureCategory);

public sealed record ProvisioningStatusDto(
    Guid OperationId,
    Guid OrganizationId,
    Guid GymId,
    string Status,
    string? CurrentStep,
    int AttemptNo,
    DateTime RequestedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    ProvisioningServerDto? Server,
    ProvisioningDatabaseDto? Database,
    bool OwnerInitialized,
    bool Retryable,
    ProvisioningFailureDto? Failure,
    IReadOnlyList<ProvisioningStepDto> Steps);

public sealed record ProvisioningRetryAcceptedDto(
    Guid OperationId,
    string Status,
    bool RetryAccepted,
    string FailedStep,
    string NextStep,
    int NextAttemptNo,
    bool Retryable);

public static class ProvisioningContract
{
    public const string Permission = "platform.provision";
    public const string PlatformScope = "platform";
    public const string Requested = "Requested";
    public const string Provisioning = "Provisioning";
    public const string Migrating = "Migrating";
    public const string Seeding = "Seeding";
    public const string Verifying = "Verifying";
    public const string Active = "Active";
    public const string ProvisioningFailed = "ProvisioningFailed";
    public const string MigrationFailed = "MigrationFailed";
    public const string SeedingFailed = "SeedingFailed";
    public const string VerificationFailed = "VerificationFailed";

    public static readonly IReadOnlySet<string> RetryableFailureStates =
        new HashSet<string>(StringComparer.Ordinal)
        {
            ProvisioningFailed,
            MigrationFailed,
            SeedingFailed,
            VerificationFailed
        };

    public static readonly IReadOnlyList<string> StepOrder =
    [
        "RequestValidation",
        "OrganizationCreation",
        "GymRegistryCreation",
        "ServerPlacement",
        "DatabaseCreation",
        "EfCoreMigrations",
        "CanonicalSeeding",
        "Verification",
        "OwnerInitialization",
        "Activation"
    ];
}

public interface IProvisioningService
{
    Task<AuthResult<ProvisioningAcceptedDto>> RequestAsync(
        AuthenticatedUser currentUser,
        ProvisioningRequest? request,
        string? idempotencyKey,
        AuthRequestContext context,
        CancellationToken cancellationToken = default);

    Task<AuthResult<ProvisioningStatusDto>> GetStatusAsync(
        AuthenticatedUser currentUser,
        Guid operationId,
        AuthRequestContext context,
        CancellationToken cancellationToken = default);

    Task<AuthResult<ProvisioningRetryAcceptedDto>> RetryAsync(
        AuthenticatedUser currentUser,
        Guid operationId,
        ProvisioningRetryRequest? request,
        string? idempotencyKey,
        AuthRequestContext context,
        CancellationToken cancellationToken = default);
}

public interface IProvisioningWorkflow
{
    Task ProcessAsync(Guid operationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetRecoverableRunIdsAsync(CancellationToken cancellationToken = default);
}

public interface IProvisioningQueue
{
    ValueTask EnqueueAsync(Guid operationId, CancellationToken cancellationToken = default);
    IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken = default);
}
