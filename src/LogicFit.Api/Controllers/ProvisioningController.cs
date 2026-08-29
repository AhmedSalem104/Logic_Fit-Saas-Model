using LogicFit.Api.Authentication;
using LogicFit.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LogicFit.Api.Controllers;

[ApiController]
[Route("api/v1/platform/provisioning")]
[Authorize]
[EnableRateLimiting("admin")]
public sealed class ProvisioningController(
    IProvisioningService provisioning,
    ICurrentUserAccessor currentUser) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> RequestProvisioning(
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] ProvisioningRequest? request,
        CancellationToken cancellationToken)
        => AuthApiResults.ToActionResult(
            await provisioning.RequestAsync(RequiredUser(), request, idempotencyKey, AuthApiResults.RequestContext(HttpContext), cancellationToken),
            HttpContext);

    [HttpGet("{runId:guid}")]
    public async Task<IActionResult> Status(Guid runId, CancellationToken cancellationToken)
        => AuthApiResults.ToActionResult(
            await provisioning.GetStatusAsync(RequiredUser(), runId, AuthApiResults.RequestContext(HttpContext), cancellationToken),
            HttpContext);

    [HttpPost("{runId:guid}/retry")]
    public async Task<IActionResult> Retry(
        Guid runId,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] ProvisioningRetryRequest? request,
        CancellationToken cancellationToken)
        => AuthApiResults.ToActionResult(
            await provisioning.RetryAsync(RequiredUser(), runId, request, idempotencyKey, AuthApiResults.RequestContext(HttpContext), cancellationToken),
            HttpContext);

    private AuthenticatedUser RequiredUser()
        => currentUser.Current ?? throw new InvalidOperationException("Authenticated user context was not established.");
}
