using System.Security.Claims;
using System.Text.Encodings.Web;
using LogicFit.Application;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using AuthApplicationService = LogicFit.Application.IAuthenticationService;

namespace LogicFit.Api.Authentication;

public sealed class SessionAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    AuthApplicationService authentication,
    ICurrentUserAccessor currentUser) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authorization)
            || !authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var token = authorization.ToString()["Bearer ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(token) || token.Length > 512)
        {
            return AuthenticateResult.NoResult();
        }

        var resolution = await authentication.ResolveSessionAsync(token, Context.RequestAborted);
        if (resolution is null)
        {
            return AuthenticateResult.Fail("SESSION_INVALID");
        }

        currentUser.Current = resolution.User;
        var identity = new ClaimsIdentity(Scheme.Name);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, resolution.User.UserId.ToString("D")));
        identity.AddClaim(new Claim("session_id", resolution.Session.SessionId.ToString("D")));
        if (resolution.Session.GymId.HasValue)
        {
            identity.AddClaim(new Claim("gym_id", resolution.Session.GymId.Value.ToString("D")));
        }
        identity.AddClaim(new Claim("mfa_verified", resolution.Session.MfaVerified ? "true" : "false"));
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        if (Response.HasStarted)
        {
            return;
        }

        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.ContentType = "application/json";
        Response.Headers.WWWAuthenticate = "Bearer";
        var requestId = Request.Headers[RequestIdMiddleware.HeaderName].FirstOrDefault() ?? "missing";
        await Response.WriteAsJsonAsync(new LogicFit.Shared.ApiErrorResponse(
            new LogicFit.Shared.ApiError("AUTHENTICATION_REQUIRED", "A valid authenticated session is required."),
            new LogicFit.Shared.ApiMeta(requestId)), Request.HttpContext.RequestAborted);
    }
}
