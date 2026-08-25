using System.Diagnostics;

namespace LogicFit.Api;

public sealed class AccessLogMiddleware(RequestDelegate next, ILogger<AccessLogMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            logger.LogInformation(
                "HTTP request completed {Method} {Path} {StatusCode} in {ElapsedMilliseconds}ms",
                context.Request.Method,
                context.Request.Path.Value,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
