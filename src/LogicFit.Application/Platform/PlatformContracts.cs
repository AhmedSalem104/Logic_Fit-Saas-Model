namespace LogicFit.Application;

public sealed record PlatformHealthDto(
    string Status,
    string Service,
    string Version,
    string Environment);

public sealed record PlatformStatusCountDto(string Status, int Count);

public sealed record PlatformCountsDto(
    int Total,
    IReadOnlyList<PlatformStatusCountDto> ByStatus);

public sealed record OrganizationSummaryDto(
    Guid OrganizationId,
    string Name,
    string Slug,
    string Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record GymSummaryDto(
    Guid GymId,
    Guid OrganizationId,
    string Name,
    string Slug,
    string Status,
    string TimezoneName,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record DatabaseRegistrySummaryDto(
    Guid GymDatabaseId,
    Guid GymId,
    string DatabaseName,
    string Environment,
    string? SchemaVersion,
    string? SeedVersion,
    string Status,
    DateTime? LastHealthAtUtc);

public sealed record PlatformOverviewDto(
    DateTime ObservedAtUtc,
    PlatformHealthDto PlatformHealth,
    int OrganizationCount,
    PlatformCountsDto GymCounts,
    PlatformCountsDto DatabaseCounts);

public sealed record GymDetailDto(
    Guid GymId,
    Guid OrganizationId,
    string Name,
    string Slug,
    string Status,
    string TimezoneName,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<DatabaseRegistrySummaryDto> Databases);

public sealed record PlatformMonitoringSnapshotDto(
    DateTime ObservedAtUtc,
    PlatformHealthDto PlatformHealth,
    IReadOnlyList<DatabaseRegistrySummaryDto> RegisteredDatabases);

public sealed record PlatformPage<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int Total,
    bool HasNext);

public sealed record OrganizationListQuery(
    int Page,
    int PageSize,
    string? Search,
    string? Status,
    string? SortField,
    bool SortDescending);

public sealed record GymListQuery(
    int Page,
    int PageSize,
    Guid? OrganizationId,
    string? Search,
    string? Status,
    string? SortField,
    bool SortDescending);

public sealed record DatabaseListQuery(
    int Page,
    int PageSize,
    Guid? GymId,
    string? Environment,
    string? Status,
    string? SortField,
    bool SortDescending);

public interface IPlatformRepository
{
    Task<PlatformRegistryCounts> GetRegistryCountsAsync(CancellationToken cancellationToken = default);
    Task<PlatformPage<OrganizationSummaryDto>> ListOrganizationsAsync(OrganizationListQuery query, CancellationToken cancellationToken = default);
    Task<OrganizationSummaryDto?> GetOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<PlatformPage<GymSummaryDto>> ListGymsAsync(GymListQuery query, CancellationToken cancellationToken = default);
    Task<GymDetailDto?> GetGymAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<PlatformPage<DatabaseRegistrySummaryDto>> ListDatabasesAsync(DatabaseListQuery query, CancellationToken cancellationToken = default);
    Task<DatabaseRegistrySummaryDto?> GetDatabaseAsync(Guid databaseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DatabaseRegistrySummaryDto>> ListRegisteredDatabasesAsync(CancellationToken cancellationToken = default);
}

public sealed record PlatformRegistryCounts(
    int OrganizationCount,
    int GymCount,
    IReadOnlyList<PlatformStatusCountDto> GymByStatus,
    int DatabaseCount,
    IReadOnlyList<PlatformStatusCountDto> DatabaseByStatus);

public interface IPlatformFoundationService
{
    Task<AuthResult<PlatformOverviewDto>> GetOverviewAsync(AuthenticatedUser currentUser, PlatformHealthDto health, AuthRequestContext context, CancellationToken cancellationToken = default);
    Task<AuthResult<PlatformPage<OrganizationSummaryDto>>> ListOrganizationsAsync(AuthenticatedUser currentUser, OrganizationListQuery query, AuthRequestContext context, CancellationToken cancellationToken = default);
    Task<AuthResult<OrganizationSummaryDto>> GetOrganizationAsync(AuthenticatedUser currentUser, Guid organizationId, AuthRequestContext context, CancellationToken cancellationToken = default);
    Task<AuthResult<PlatformPage<GymSummaryDto>>> ListGymsAsync(AuthenticatedUser currentUser, GymListQuery query, AuthRequestContext context, CancellationToken cancellationToken = default);
    Task<AuthResult<GymDetailDto>> GetGymAsync(AuthenticatedUser currentUser, Guid gymId, AuthRequestContext context, CancellationToken cancellationToken = default);
    Task<AuthResult<PlatformPage<DatabaseRegistrySummaryDto>>> ListDatabasesAsync(AuthenticatedUser currentUser, DatabaseListQuery query, AuthRequestContext context, CancellationToken cancellationToken = default);
    Task<AuthResult<DatabaseRegistrySummaryDto>> GetDatabaseAsync(AuthenticatedUser currentUser, Guid databaseId, AuthRequestContext context, CancellationToken cancellationToken = default);
    Task<AuthResult<PlatformMonitoringSnapshotDto>> GetMonitoringAsync(AuthenticatedUser currentUser, PlatformHealthDto health, AuthRequestContext context, CancellationToken cancellationToken = default);
}
