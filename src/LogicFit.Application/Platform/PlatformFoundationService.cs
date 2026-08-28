namespace LogicFit.Application;

public sealed class PlatformFoundationService(
    IPlatformRepository repository,
    IAuthenticationService authentication,
    IAuthRepository auditRepository) : IPlatformFoundationService
{
    private const string PlatformViewPermission = "platform.view";
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 25;
    private const int MaximumPageSize = 100;

    public async Task<AuthResult<PlatformOverviewDto>> GetOverviewAsync(
        AuthenticatedUser currentUser,
        PlatformHealthDto health,
        AuthRequestContext context,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizePlatformAsync<PlatformOverviewDto>(currentUser, context, cancellationToken);
        if (authorization is not null)
        {
            return authorization;
        }

        try
        {
            var counts = await repository.GetRegistryCountsAsync(cancellationToken);
            return AuthResult<PlatformOverviewDto>.Success(new PlatformOverviewDto(
                DateTime.UtcNow,
                health,
                counts.OrganizationCount,
                new PlatformCountsDto(counts.GymCount, counts.GymByStatus),
                new PlatformCountsDto(counts.DatabaseCount, counts.DatabaseByStatus)));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Unavailable<PlatformOverviewDto>();
        }
    }

    public async Task<AuthResult<PlatformPage<OrganizationSummaryDto>>> ListOrganizationsAsync(
        AuthenticatedUser currentUser,
        OrganizationListQuery query,
        AuthRequestContext context,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizePlatformAsync<PlatformPage<OrganizationSummaryDto>>(currentUser, context, cancellationToken);
        if (authorization is not null)
        {
            return authorization;
        }

        if (IsInvalidPage(query.Page, query.PageSize, query.SortField, ["name", "slug", "createdAtUtc", "updatedAtUtc"]))
        {
            return AuthResult<PlatformPage<OrganizationSummaryDto>>.Failure(400, "INVALID_FILTER", "The requested platform filters are not valid.");
        }

        try
        {
            return AuthResult<PlatformPage<OrganizationSummaryDto>>.Success(await repository.ListOrganizationsAsync(query, cancellationToken));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Unavailable<PlatformPage<OrganizationSummaryDto>>();
        }
    }

    public async Task<AuthResult<OrganizationSummaryDto>> GetOrganizationAsync(
        AuthenticatedUser currentUser,
        Guid organizationId,
        AuthRequestContext context,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizePlatformAsync<OrganizationSummaryDto>(currentUser, context, cancellationToken);
        if (authorization is not null)
        {
            return authorization;
        }

        try
        {
            var organization = await repository.GetOrganizationAsync(organizationId, cancellationToken);
            return organization is null
                ? NotFound<OrganizationSummaryDto>()
                : AuthResult<OrganizationSummaryDto>.Success(organization);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Unavailable<OrganizationSummaryDto>();
        }
    }

    public async Task<AuthResult<PlatformPage<GymSummaryDto>>> ListGymsAsync(
        AuthenticatedUser currentUser,
        GymListQuery query,
        AuthRequestContext context,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizePlatformAsync<PlatformPage<GymSummaryDto>>(currentUser, context, cancellationToken);
        if (authorization is not null)
        {
            return authorization;
        }

        if (IsInvalidPage(query.Page, query.PageSize, query.SortField, ["name", "slug", "createdAtUtc", "updatedAtUtc"]))
        {
            return AuthResult<PlatformPage<GymSummaryDto>>.Failure(400, "INVALID_FILTER", "The requested platform filters are not valid.");
        }

        try
        {
            return AuthResult<PlatformPage<GymSummaryDto>>.Success(await repository.ListGymsAsync(query, cancellationToken));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Unavailable<PlatformPage<GymSummaryDto>>();
        }
    }

    public async Task<AuthResult<GymDetailDto>> GetGymAsync(
        AuthenticatedUser currentUser,
        Guid gymId,
        AuthRequestContext context,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizePlatformAsync<GymDetailDto>(currentUser, context, cancellationToken);
        if (authorization is not null)
        {
            return authorization;
        }

        try
        {
            var gym = await repository.GetGymAsync(gymId, cancellationToken);
            return gym is null
                ? NotFound<GymDetailDto>()
                : AuthResult<GymDetailDto>.Success(gym);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Unavailable<GymDetailDto>();
        }
    }

    public async Task<AuthResult<PlatformPage<DatabaseRegistrySummaryDto>>> ListDatabasesAsync(
        AuthenticatedUser currentUser,
        DatabaseListQuery query,
        AuthRequestContext context,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizePlatformAsync<PlatformPage<DatabaseRegistrySummaryDto>>(currentUser, context, cancellationToken);
        if (authorization is not null)
        {
            return authorization;
        }

        if (IsInvalidPage(query.Page, query.PageSize, query.SortField, ["databaseName", "status", "lastHealthAtUtc"]))
        {
            return AuthResult<PlatformPage<DatabaseRegistrySummaryDto>>.Failure(400, "INVALID_FILTER", "The requested platform filters are not valid.");
        }

        try
        {
            return AuthResult<PlatformPage<DatabaseRegistrySummaryDto>>.Success(await repository.ListDatabasesAsync(query, cancellationToken));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Unavailable<PlatformPage<DatabaseRegistrySummaryDto>>();
        }
    }

    public async Task<AuthResult<DatabaseRegistrySummaryDto>> GetDatabaseAsync(
        AuthenticatedUser currentUser,
        Guid databaseId,
        AuthRequestContext context,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizePlatformAsync<DatabaseRegistrySummaryDto>(currentUser, context, cancellationToken);
        if (authorization is not null)
        {
            return authorization;
        }

        try
        {
            var database = await repository.GetDatabaseAsync(databaseId, cancellationToken);
            return database is null
                ? NotFound<DatabaseRegistrySummaryDto>()
                : AuthResult<DatabaseRegistrySummaryDto>.Success(database);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Unavailable<DatabaseRegistrySummaryDto>();
        }
    }

    public async Task<AuthResult<PlatformMonitoringSnapshotDto>> GetMonitoringAsync(
        AuthenticatedUser currentUser,
        PlatformHealthDto health,
        AuthRequestContext context,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizePlatformAsync<PlatformMonitoringSnapshotDto>(currentUser, context, cancellationToken);
        if (authorization is not null)
        {
            return authorization;
        }

        try
        {
            var databases = await repository.ListRegisteredDatabasesAsync(cancellationToken);
            return AuthResult<PlatformMonitoringSnapshotDto>.Success(new PlatformMonitoringSnapshotDto(DateTime.UtcNow, health, databases));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Unavailable<PlatformMonitoringSnapshotDto>();
        }
    }

    private async Task<AuthResult<T>?> AuthorizePlatformAsync<T>(AuthenticatedUser currentUser, AuthRequestContext context, CancellationToken cancellationToken)
    {
        if (!currentUser.IsMfaVerified)
        {
            return AuthResult<T>.Failure(403, "MFA_REQUIRED", "Complete the security challenge before accessing platform information.");
        }

        bool hasPermission;
        try
        {
            hasPermission = !currentUser.GymId.HasValue
                && await authentication.HasPermissionAsync(currentUser, PlatformViewPermission, cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Unavailable<T>();
        }

        if (!hasPermission)
        {
            await auditRepository.WriteAuditAsync(new AuditEntry(
                context.RequestId,
                currentUser.UserId,
                "iam.authorization",
                null,
                "authz.permission_denied",
                "failure",
                PlatformViewPermission,
                "platform"), cancellationToken);
            return AuthResult<T>.Failure(403, "PERMISSION_DENIED", "The authenticated user is not authorized for platform information.");
        }

        return null;
    }

    private static bool IsInvalidPage(int page, int pageSize, string? sortField, IReadOnlyCollection<string> validSortFields)
        => page < DefaultPage
            || pageSize < 1
            || pageSize > MaximumPageSize
            || (sortField is not null && !validSortFields.Contains(sortField, StringComparer.Ordinal));

    private static AuthResult<T> NotFound<T>()
        => AuthResult<T>.Failure(404, "RESOURCE_NOT_FOUND", "The requested platform resource was not found.");

    private static AuthResult<T> Unavailable<T>()
        => AuthResult<T>.Failure(503, "DEPENDENCY_UNAVAILABLE", "The platform registry is temporarily unavailable.");
}
