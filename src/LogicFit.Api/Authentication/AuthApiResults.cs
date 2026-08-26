using LogicFit.Application;
using LogicFit.Shared;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.Api.Authentication;

public static class AuthApiResults
{
    public static IActionResult ToActionResult<T>(AuthResult<T> result, HttpContext context)
    {
        var requestId = context.Request.Headers[RequestIdMiddleware.HeaderName].FirstOrDefault() ?? "missing";
        if (result.Succeeded)
        {
            return new ObjectResult(new ApiResponse<T>(result.Data!, new ApiMeta(requestId)))
            {
                StatusCode = result.StatusCode
            };
        }

        return new ObjectResult(new ApiErrorResponse(
            new ApiError(result.ErrorCode, result.Message, result.FieldErrors),
            new ApiMeta(requestId)))
        {
            StatusCode = result.StatusCode
        };
    }

    public static IActionResult ToCollectionResult<T>(AuthResult<IReadOnlyList<T>> result, HttpContext context, int page, int pageSize, string? totalHeader = null)
    {
        var requestId = context.Request.Headers[RequestIdMiddleware.HeaderName].FirstOrDefault() ?? "missing";
        if (!result.Succeeded)
        {
            return ToActionResult(result, context);
        }

        var total = result.Data?.Count ?? 0;
        if (int.TryParse(totalHeader, out var parsedTotal))
        {
            total = parsedTotal;
        }

        return new ObjectResult(new ApiCollectionResponse<T>(
            result.Data ?? [],
            new ApiCollectionMeta(requestId, page, pageSize, total, page * pageSize < total)))
        {
            StatusCode = result.StatusCode
        };
    }

    public static string? BearerToken(HttpRequest request)
    {
        var value = request.Headers["Authorization"].ToString();
        return value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? value["Bearer ".Length..].Trim() : null;
    }

    public static AuthRequestContext RequestContext(HttpContext context)
        => new(
            context.Request.Headers[RequestIdMiddleware.HeaderName].FirstOrDefault() ?? "missing",
            context.Request.Headers["User-Agent"].FirstOrDefault(),
            context.Connection.RemoteIpAddress?.ToString(),
            BearerToken(context.Request));

    public static bool TryReadVersion(HttpRequest request, out byte[]? version)
    {
        version = null;
        var value = request.Headers["If-Match"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(value) || value == "*")
        {
            return false;
        }

        value = value.Trim().Trim('"');
        try
        {
            version = Convert.FromBase64String(value);
            return version.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
