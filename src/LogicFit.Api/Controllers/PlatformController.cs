using LogicFit.Application;
using LogicFit.Api.Authentication;
using LogicFit.Api.Platform;
using LogicFit.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace LogicFit.Api.Controllers;

[ApiController]
[Route("api/v1/platform")]
[Authorize]
[EnableRateLimiting("foundation")]
public sealed class PlatformController(
    IPlatformFoundationService platform,
    ICurrentUserAccessor currentUser,
    IOptions<LogicFitRuntimeOptions> runtimeOptions) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<IActionResult> Overview(CancellationToken cancellationToken)
        => PlatformApiResults.ToActionResult(
            await platform.GetOverviewAsync(RequiredUser(), Health(), AuthApiResults.RequestContext(HttpContext), cancellationToken),
            HttpContext);

    [HttpGet("organizations")]
    public async Task<IActionResult> Organizations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default)
    {
        var parsedSort = ParseSort(sort, "name", "slug", "createdAtUtc", "updatedAtUtc");
        if (!parsedSort.Valid)
        {
            return PlatformApiResults.ToActionResult(
                AuthResult<PlatformPage<OrganizationSummaryDto>>.Failure(400, "INVALID_FILTER", "The requested platform filters are not valid."),
                HttpContext);
        }

        var query = new OrganizationListQuery(page, pageSize, search, status, parsedSort.Field, parsedSort.Descending);
        var result = await platform.ListOrganizationsAsync(RequiredUser(), query, AuthApiResults.RequestContext(HttpContext), cancellationToken);
        return PlatformApiResults.ToCollectionResult(result, HttpContext);
    }

    [HttpGet("organizations/{organizationId:guid}")]
    public async Task<IActionResult> Organization(Guid organizationId, CancellationToken cancellationToken)
        => PlatformApiResults.ToActionResult(
            await platform.GetOrganizationAsync(RequiredUser(), organizationId, AuthApiResults.RequestContext(HttpContext), cancellationToken),
            HttpContext);

    [HttpGet("databases")]
    public async Task<IActionResult> Databases(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] Guid? gymId = null,
        [FromQuery] string? environment = null,
        [FromQuery] string? status = null,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default)
    {
        var parsedSort = ParseSort(sort, "databaseName", "status", "lastHealthAtUtc");
        if (!parsedSort.Valid)
        {
            return PlatformApiResults.ToActionResult(
                AuthResult<PlatformPage<DatabaseRegistrySummaryDto>>.Failure(400, "INVALID_FILTER", "The requested platform filters are not valid."),
                HttpContext);
        }

        var query = new DatabaseListQuery(page, pageSize, gymId, environment, status, parsedSort.Field, parsedSort.Descending);
        var result = await platform.ListDatabasesAsync(RequiredUser(), query, AuthApiResults.RequestContext(HttpContext), cancellationToken);
        return PlatformApiResults.ToCollectionResult(result, HttpContext);
    }

    [HttpGet("databases/{databaseId:guid}")]
    public async Task<IActionResult> Database(Guid databaseId, CancellationToken cancellationToken)
        => PlatformApiResults.ToActionResult(
            await platform.GetDatabaseAsync(RequiredUser(), databaseId, AuthApiResults.RequestContext(HttpContext), cancellationToken),
            HttpContext);

    [HttpGet("monitoring")]
    public async Task<IActionResult> Monitoring(CancellationToken cancellationToken)
        => PlatformApiResults.ToActionResult(
            await platform.GetMonitoringAsync(RequiredUser(), Health(), AuthApiResults.RequestContext(HttpContext), cancellationToken),
            HttpContext);

    private AuthenticatedUser RequiredUser()
        => currentUser.Current ?? throw new InvalidOperationException("Authenticated user context was not established.");

    private PlatformHealthDto Health()
        => new("healthy", "api", runtimeOptions.Value.Version, runtimeOptions.Value.Environment);

    private static (string? Field, bool Descending, bool Valid) ParseSort(string? sort, params string[] validFields)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return (null, true, true);
        }

        var parts = sort.Split(':', 2, StringSplitOptions.TrimEntries);
        var field = parts[0];
        var validField = validFields.Contains(field, StringComparer.Ordinal);
        var validDirection = parts.Length == 1
            || parts[1].Equals("asc", StringComparison.OrdinalIgnoreCase)
            || parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);
        var descending = parts.Length == 1 || parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);
        return (field, descending, validField && validDirection);
    }
}
