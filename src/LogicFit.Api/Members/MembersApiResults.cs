using LogicFit.Application;
using LogicFit.Api.Authentication;
using LogicFit.Shared;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.Api.Members;

public static class MembersApiResults
{
    public static IActionResult ToActionResult<T>(AuthResult<T> result, HttpContext context)
        => AuthApiResults.ToActionResult(result, context);

    public static IActionResult ToCollectionResult(AuthResult<MemberPageDto> result, HttpContext context)
    {
        if (!result.Succeeded)
        {
            return ToActionResult(result, context);
        }

        var page = result.Data!;
        var requestId = context.Request.Headers[RequestIdMiddleware.HeaderName].FirstOrDefault() ?? "missing";
        return new ObjectResult(new ApiCollectionResponse<MemberSummaryDto>(
            page.Items,
            new ApiCollectionMeta(requestId, page.Page, page.PageSize, page.Total, page.HasNext)))
        {
            StatusCode = result.StatusCode
        };
    }

    public static IActionResult ToTimelineCollectionResult(AuthResult<MemberTimelinePageDto> result, HttpContext context)
    {
        if (!result.Succeeded)
        {
            return ToActionResult(result, context);
        }

        var page = result.Data!;
        var requestId = context.Request.Headers[RequestIdMiddleware.HeaderName].FirstOrDefault() ?? "missing";
        return new ObjectResult(new ApiCollectionResponse<MemberTimelineItemDto>(
            page.Items,
            new ApiCollectionMeta(requestId, page.Page, page.PageSize, page.Total, page.HasNext)))
        {
            StatusCode = result.StatusCode
        };
    }
}
