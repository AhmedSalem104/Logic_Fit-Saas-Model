using LogicFit.Application;
using LogicFit.Api.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LogicFit.Api.Controllers;

[ApiController]
[Route("api/v1/platform/access")]
[Authorize]
[EnableRateLimiting("admin")]
public sealed class AccessController(
    IAuthenticationService authentication,
    ICurrentUserAccessor currentUser) : ControllerBase
{
    [HttpGet("catalog")]
    public async Task<IActionResult> Catalog(CancellationToken cancellationToken)
        => AuthApiResults.ToActionResult(await authentication.GetAccessCatalogAsync(RequiredUser(), AuthApiResults.RequestContext(HttpContext), cancellationToken), HttpContext);

    [HttpGet("users")]
    public async Task<IActionResult> Users(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] Guid? gymId = null,
        [FromQuery] string? scopeType = null,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default)
    {
        var (sortField, descending, validSort) = ParseSort(sort);
        if (!validSort)
        {
            return AuthApiResults.ToActionResult(AuthResult<AccessUsersPageDto>.Failure(400, "INVALID_FILTER", "The requested access filters are not valid."), HttpContext);
        }

        var query = new AccessUsersQuery(page, pageSize, gymId, scopeType, status, search, sortField, descending);
        var result = await authentication.ListAccessUsersAsync(RequiredUser(), query, AuthApiResults.RequestContext(HttpContext), cancellationToken);
        var mapped = AuthApiResults.ToActionResult(result, HttpContext);
        if (mapped is not ObjectResult objectResult || result.Data is null || !result.Succeeded)
        {
            return mapped;
        }

        return new ObjectResult(new LogicFit.Shared.ApiCollectionResponse<AccessUserDto>(
            result.Data.Items,
            new LogicFit.Shared.ApiCollectionMeta(
                HttpContext.Request.Headers[RequestIdMiddleware.HeaderName].FirstOrDefault() ?? "missing",
                result.Data.Page,
                result.Data.PageSize,
                result.Data.Total,
                result.Data.HasNext)))
        {
            StatusCode = objectResult.StatusCode
        };
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] AccessUserCreateCommand command, CancellationToken cancellationToken)
        => AuthApiResults.ToActionResult(await authentication.CreateAccessUserAsync(RequiredUser(), command, AuthApiResults.RequestContext(HttpContext), cancellationToken), HttpContext);

    [HttpPatch("users/{userId:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid userId, [FromBody] AccessUserStatusCommand command, CancellationToken cancellationToken)
    {
        var version = AuthApiResults.TryReadVersion(Request, out var parsed) ? parsed : null;
        return AuthApiResults.ToActionResult(await authentication.ChangeUserStatusAsync(RequiredUser(), userId, command, version, AuthApiResults.RequestContext(HttpContext), cancellationToken), HttpContext);
    }

    [HttpPut("users/{userId:guid}/role-assignments/{roleId:guid}")]
    public async Task<IActionResult> EnsureRoleAssignment(Guid userId, Guid roleId, [FromQuery] Guid? gymId, [FromBody] RoleAssignmentCommand command, CancellationToken cancellationToken)
    {
        var version = AuthApiResults.TryReadVersion(Request, out var parsed) ? parsed : null;
        return AuthApiResults.ToActionResult(await authentication.EnsureRoleAssignmentAsync(RequiredUser(), userId, roleId, gymId, version, command, AuthApiResults.RequestContext(HttpContext), cancellationToken), HttpContext);
    }

    [HttpPost("users/{userId:guid}/role-assignments/{assignmentId:guid}/revoke")]
    public async Task<IActionResult> RevokeRoleAssignment(Guid userId, Guid assignmentId, [FromBody] RoleRevocationCommand command, CancellationToken cancellationToken)
    {
        var version = AuthApiResults.TryReadVersion(Request, out var parsed) ? parsed : null;
        return AuthApiResults.ToActionResult(await authentication.RevokeRoleAssignmentAsync(RequiredUser(), userId, assignmentId, version, command, AuthApiResults.RequestContext(HttpContext), cancellationToken), HttpContext);
    }

    private AuthenticatedUser RequiredUser()
        => currentUser.Current ?? throw new InvalidOperationException("Authenticated user context was not established.");

    private static (string? Field, bool Descending, bool Valid) ParseSort(string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return (null, true, true);
        }

        var parts = sort.Split(':', 2, StringSplitOptions.TrimEntries);
        var field = parts[0];
        var descending = parts.Length == 1 || parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);
        var validDirection = parts.Length == 1 || parts[1].Equals("asc", StringComparison.OrdinalIgnoreCase) || parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);
        return (field, descending, validDirection && field is "createdAtUtc" or "updatedAtUtc" or "email");
    }
}
