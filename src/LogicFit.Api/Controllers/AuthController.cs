using LogicFit.Application;
using LogicFit.Api.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LogicFit.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    IAuthenticationService authentication,
    ICurrentUserAccessor currentUser) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
        => AuthApiResults.ToActionResult(await authentication.LoginAsync(command, AuthApiResults.RequestContext(HttpContext), cancellationToken), HttpContext);

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Refresh([FromBody] RefreshCommand command, CancellationToken cancellationToken)
        => AuthApiResults.ToActionResult(await authentication.RefreshAsync(command, AuthApiResults.RequestContext(HttpContext), cancellationToken), HttpContext);

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutCommand command, CancellationToken cancellationToken)
        => AuthApiResults.ToActionResult(await authentication.LogoutAsync(RequiredUser(), command, AuthApiResults.RequestContext(HttpContext), cancellationToken), HttpContext);

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
        => AuthApiResults.ToActionResult(await authentication.GetMeAsync(RequiredUser(), cancellationToken), HttpContext);

    [HttpPost("mfa/verify")]
    [AllowAnonymous]
    [EnableRateLimiting("mfa")]
    public async Task<IActionResult> VerifyMfa([FromBody] MfaVerifyCommand command, CancellationToken cancellationToken)
        => AuthApiResults.ToActionResult(await authentication.VerifyMfaAsync(currentUser.Current, command, AuthApiResults.RequestContext(HttpContext), cancellationToken), HttpContext);

    [HttpPost("mfa/enroll")]
    [Authorize]
    public async Task<IActionResult> EnrollMfa(CancellationToken cancellationToken)
        => AuthApiResults.ToActionResult(await authentication.EnrollMfaAsync(RequiredUser(), AuthApiResults.RequestContext(HttpContext), cancellationToken), HttpContext);

    [HttpPost("mfa/disable")]
    [Authorize]
    [EnableRateLimiting("mfa")]
    public async Task<IActionResult> DisableMfa([FromBody] MfaDisableCommand command, CancellationToken cancellationToken)
        => AuthApiResults.ToActionResult(await authentication.DisableMfaAsync(RequiredUser(), command, AuthApiResults.RequestContext(HttpContext), cancellationToken), HttpContext);

    [HttpPost("mfa/recovery-codes/regenerate")]
    [Authorize]
    [EnableRateLimiting("mfa")]
    public async Task<IActionResult> RegenerateRecoveryCodes([FromBody] RecoveryCodesRegenerateCommand command, CancellationToken cancellationToken)
        => AuthApiResults.ToActionResult(await authentication.RegenerateRecoveryCodesAsync(RequiredUser(), command, AuthApiResults.RequestContext(HttpContext), cancellationToken), HttpContext);

    [HttpPost("password-reset/request")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetRequestCommand command, CancellationToken cancellationToken)
        => AuthApiResults.ToActionResult(await authentication.RequestPasswordResetAsync(command, AuthApiResults.RequestContext(HttpContext), cancellationToken), HttpContext);

    [HttpPost("password-reset/complete")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> CompletePasswordReset([FromBody] PasswordResetCompleteCommand command, CancellationToken cancellationToken)
        => AuthApiResults.ToActionResult(await authentication.CompletePasswordResetAsync(command, AuthApiResults.RequestContext(HttpContext), cancellationToken), HttpContext);

    [HttpPost("password/change")]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ChangePassword([FromBody] PasswordChangeCommand command, CancellationToken cancellationToken)
        => AuthApiResults.ToActionResult(await authentication.ChangePasswordAsync(RequiredUser(), command, AuthApiResults.RequestContext(HttpContext), cancellationToken), HttpContext);

    [HttpGet("sessions")]
    [Authorize]
    public async Task<IActionResult> Sessions([FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] Guid? gymId = null, [FromQuery] string? sort = null, CancellationToken cancellationToken = default)
    {
        var (sortField, descending, validSort) = ParseSort(sort);
        if (!validSort)
        {
            return AuthApiResults.ToActionResult(AuthResult<SessionPageDto>.Failure(400, "INVALID_FILTER", "The requested session filters are not valid."), HttpContext);
        }

        var response = AuthApiResults.ToActionResult(await authentication.ListSessionsAsync(RequiredUser(), gymId, page, pageSize, sortField, descending, AuthApiResults.RequestContext(HttpContext), cancellationToken), HttpContext);
        return response is ObjectResult result ? ConvertSessionPageResult(result, HttpContext) : response;
    }

    [HttpPost("sessions/{sessionId:guid}/revoke")]
    [Authorize]
    public async Task<IActionResult> RevokeSession(Guid sessionId, [FromBody] RevokeSessionRequest? request, CancellationToken cancellationToken)
        => AuthApiResults.ToActionResult(await authentication.RevokeOwnedSessionAsync(RequiredUser(), sessionId, request?.Reason, AuthApiResults.RequestContext(HttpContext), cancellationToken), HttpContext);

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
        return (field, descending, validDirection && field is "createdAtUtc" or "lastSeenAtUtc" or "expiresAtUtc");
    }

    private static IActionResult ConvertSessionPageResult(ObjectResult result, HttpContext context)
    {
        if (result.Value is not LogicFit.Shared.ApiResponse<SessionPageDto> response || response.Data is null)
        {
            return result;
        }

        var requestId = context.Request.Headers[RequestIdMiddleware.HeaderName].FirstOrDefault() ?? "missing";
        return new ObjectResult(new LogicFit.Shared.ApiCollectionResponse<SessionListItemDto>(
            response.Data.Items,
            new LogicFit.Shared.ApiCollectionMeta(requestId, response.Data.Page, response.Data.PageSize, response.Data.Total, response.Data.HasNext)))
        {
            StatusCode = result.StatusCode
        };
    }

    public sealed record RevokeSessionRequest(string? Reason);
}
