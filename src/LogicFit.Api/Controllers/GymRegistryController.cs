using LogicFit.Application;
using LogicFit.Api.Authentication;
using LogicFit.Api.Platform;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LogicFit.Api.Controllers;

[ApiController]
[Route("api/v1/gyms")]
[Authorize]
[EnableRateLimiting("foundation")]
public sealed class GymRegistryController(
    IPlatformFoundationService platform,
    ICurrentUserAccessor currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Gyms(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] Guid? organizationId = null,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default)
    {
        var parsedSort = ParseSort(sort, "name", "slug", "createdAtUtc", "updatedAtUtc");
        if (!parsedSort.Valid)
        {
            return PlatformApiResults.ToActionResult(
                AuthResult<PlatformPage<GymSummaryDto>>.Failure(400, "INVALID_FILTER", "The requested platform filters are not valid."),
                HttpContext);
        }

        var query = new GymListQuery(page, pageSize, organizationId, search, status, parsedSort.Field, parsedSort.Descending);
        var result = await platform.ListGymsAsync(RequiredUser(), query, AuthApiResults.RequestContext(HttpContext), cancellationToken);
        return PlatformApiResults.ToCollectionResult(result, HttpContext);
    }

    [HttpGet("{gymId:guid}")]
    public async Task<IActionResult> Gym(Guid gymId, CancellationToken cancellationToken)
        => PlatformApiResults.ToActionResult(
            await platform.GetGymAsync(RequiredUser(), gymId, AuthApiResults.RequestContext(HttpContext), cancellationToken),
            HttpContext);

    private AuthenticatedUser RequiredUser()
        => currentUser.Current ?? throw new InvalidOperationException("Authenticated user context was not established.");

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
