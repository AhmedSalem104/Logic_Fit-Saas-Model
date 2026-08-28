using LogicFit.Application;
using LogicFit.Api.Authentication;
using LogicFit.Shared;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.Api.Platform;

public static class PlatformApiResults
{
    public static IActionResult ToActionResult<T>(AuthResult<T> result, HttpContext context)
        => AuthApiResults.ToActionResult(result, context);

    public static IActionResult ToCollectionResult<T>(AuthResult<PlatformPage<T>> result, HttpContext context)
    {
        if (!result.Succeeded)
        {
            return ToActionResult(result, context);
        }

        var requestId = context.Request.Headers[RequestIdMiddleware.HeaderName].FirstOrDefault() ?? "missing";
        var page = result.Data!;
        return new ObjectResult(new ApiCollectionResponse<T>(
            page.Items,
            new ApiCollectionMeta(requestId, page.Page, page.PageSize, page.Total, page.HasNext)))
        {
            StatusCode = result.StatusCode
        };
    }
}
