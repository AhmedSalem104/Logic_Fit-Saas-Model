namespace LogicFit.Api;

public sealed class RequestIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Request-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(requestId) || requestId.Length > 80)
        {
            requestId = Guid.NewGuid().ToString("N");
        }

        context.Request.Headers[HeaderName] = requestId;
        context.Response.Headers[HeaderName] = requestId;
        await next(context);
    }
}
