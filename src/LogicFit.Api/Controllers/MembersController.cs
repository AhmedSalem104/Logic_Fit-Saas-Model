using LogicFit.Api.Authentication;
using LogicFit.Api.Members;
using LogicFit.Application;
using LogicFit.Domain.Members;
using LogicFit.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LogicFit.Api.Controllers;

[ApiController]
[Route("api/v1/gyms/{gymId:guid}/members")]
[Authorize]
[EnableRateLimiting("foundation")]
public sealed class MembersController(
    IMembersService members,
    ICurrentUserAccessor currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        Guid gymId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = MembersContract.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] string[]? status = null,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default)
    {
        if (!HasOnlyQueryKeys("page", "pageSize", "search", "status", "sort"))
        {
            return MembersApiResults.ToActionResult(
                AuthResult<MemberPageDto>.Failure(400, "INVALID_FILTER", "The requested Member filters are not valid."),
                HttpContext);
        }

        var statuses = ParseStatuses(status);
        if (statuses is null)
        {
            return MembersApiResults.ToActionResult(
                AuthResult<MemberPageDto>.Failure(400, "INVALID_FILTER", "The requested Member statuses are not valid."),
                HttpContext);
        }

        var parsedSort = ParseSort(sort);
        if (parsedSort is null)
        {
            return MembersApiResults.ToActionResult(
                AuthResult<MemberPageDto>.Failure(400, "INVALID_FILTER", "The requested Member sort is not valid."),
                HttpContext);
        }

        var result = await members.ListAsync(
            RequiredUser(),
            gymId,
            new MemberListQuery(page, pageSize, search, statuses, parsedSort.Value.Field, parsedSort.Value.Descending),
            AuthApiResults.RequestContext(HttpContext),
            cancellationToken);
        return MembersApiResults.ToCollectionResult(result, HttpContext);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        Guid gymId,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] CreateMemberCommand? command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
        {
            return MembersApiResults.ToActionResult(
                AuthResult<MemberDetailDto>.Failure(400, "VALIDATION_ERROR", "The request body is required."),
                HttpContext);
        }

        var result = await members.CreateAsync(
            RequiredUser(),
            gymId,
            command,
            idempotencyKey,
            AuthApiResults.RequestContext(HttpContext),
            cancellationToken);
        return MembersApiResults.ToActionResult(result, HttpContext);
    }

    [HttpGet("{memberId:guid}")]
    public async Task<IActionResult> Get(Guid gymId, Guid memberId, CancellationToken cancellationToken = default)
    {
        var result = await members.GetAsync(
            RequiredUser(),
            gymId,
            memberId,
            AuthApiResults.RequestContext(HttpContext),
            cancellationToken);
        return MembersApiResults.ToActionResult(result, HttpContext);
    }

    [HttpPut("{memberId:guid}")]
    public async Task<IActionResult> Update(
        Guid gymId,
        Guid memberId,
        [FromBody] UpdateMemberCommand? command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
        {
            return MembersApiResults.ToActionResult(
                AuthResult<MemberDetailDto>.Failure(400, "VALIDATION_ERROR", "The request body is required."),
                HttpContext);
        }

        if (!AuthApiResults.TryReadVersion(Request, out var expectedVersion) || expectedVersion is null)
        {
            return MembersApiResults.ToActionResult(
                AuthResult<MemberDetailDto>.Failure(400, "CONCURRENCY_VERSION_REQUIRED", "If-Match is required to update a Member."),
                HttpContext);
        }

        var result = await members.UpdateAsync(
            RequiredUser(),
            gymId,
            memberId,
            command,
            expectedVersion,
            AuthApiResults.RequestContext(HttpContext),
            cancellationToken);
        return MembersApiResults.ToActionResult(result, HttpContext);
    }

    [HttpDelete("{memberId:guid}")]
    public async Task<IActionResult> Archive(Guid gymId, Guid memberId, CancellationToken cancellationToken = default)
    {
        byte[]? expectedVersion = null;
        if (Request.Headers.ContainsKey("If-Match")
            && !AuthApiResults.TryReadVersion(Request, out expectedVersion))
        {
            return MembersApiResults.ToActionResult(
                AuthResult<MemberArchiveDto>.Failure(400, "CONCURRENCY_VERSION_INVALID", "If-Match is not a valid Member version."),
                HttpContext);
        }

        var result = await members.ArchiveAsync(
            RequiredUser(),
            gymId,
            memberId,
            expectedVersion,
            AuthApiResults.RequestContext(HttpContext),
            cancellationToken);
        return MembersApiResults.ToActionResult(result, HttpContext);
    }

    [HttpGet("{memberId:guid}/timeline")]
    public async Task<IActionResult> Timeline(
        Guid gymId,
        Guid memberId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = MembersContract.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        if (!HasOnlyQueryKeys("page", "pageSize"))
        {
            return MembersApiResults.ToActionResult(
                AuthResult<MemberTimelinePageDto>.Failure(400, "INVALID_FILTER", "The requested timeline filters are not valid."),
                HttpContext);
        }

        var result = await members.TimelineAsync(
            RequiredUser(),
            gymId,
            memberId,
            page,
            pageSize,
            AuthApiResults.RequestContext(HttpContext),
            cancellationToken);
        return MembersApiResults.ToTimelineCollectionResult(result, HttpContext);
    }

    private AuthenticatedUser RequiredUser()
        => currentUser.Current ?? throw new InvalidOperationException("Authenticated user context was not established.");

    private bool HasOnlyQueryKeys(params string[] allowed)
        => Request.Query.Keys.All(key => allowed.Contains(key, StringComparer.Ordinal));

    private static IReadOnlySet<string>? ParseStatuses(IEnumerable<string>? rawValues)
    {
        var raw = rawValues?.ToArray() ?? [];
        if (raw.Length == 0)
        {
            return MembersContract.DefaultStatuses;
        }

        var values = raw
            .SelectMany(value => value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .ToArray();
        if (values.Length == 0 || values.Any(value => !MemberStatuses.All.Contains(value)))
        {
            return null;
        }

        return values.ToHashSet(StringComparer.Ordinal);
    }

    private static (string Field, bool Descending)? ParseSort(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return (MembersContract.DefaultSortField, true);
        }

        var parts = raw.Split(':', StringSplitOptions.TrimEntries);
        if (parts.Length is < 1 or > 2 || parts[0] is not ("createdAt" or "updatedAt"))
        {
            return null;
        }

        if (parts.Length == 1)
        {
            return (parts[0], true);
        }

        return parts[1] switch
        {
            "asc" => (parts[0], false),
            "desc" => (parts[0], true),
            _ => null
        };
    }
}
