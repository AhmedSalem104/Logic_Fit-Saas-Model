using LogicFit.Shared;

namespace LogicFit.Api;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            var requestId = context.Request.Headers[RequestIdMiddleware.HeaderName].FirstOrDefault() ?? "missing";
            logger.LogError(exception, "Unhandled request failure for {RequestId}", requestId);
            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            var response = new ApiErrorResponse(
                new ApiError("internal_error", "An unexpected server error occurred.", null),
                new ApiMeta(requestId, LogicFitApi.Version));
            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
