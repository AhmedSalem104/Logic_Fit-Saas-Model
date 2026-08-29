using LogicFit.Application;
using LogicFit.Api.Authentication;
using LogicFit.Infrastructure;
using LogicFit.Infrastructure.Persistence;
using LogicFit.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options => options.JsonWriterOptions = new System.Text.Json.JsonWriterOptions { Indented = false });
// SQL command text is diagnostic noise for the running API and can expose
// implementation details in structured logs. Keep application and migration
// failures visible while suppressing per-command EF output.
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
builder.Services.AddHttpContextAccessor();
builder.Services.AddLogicFitApplication();
builder.Services.AddLogicFitInfrastructure(builder.Configuration);
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var fieldErrors = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .Select(entry => new ApiFieldError(entry.Key, "invalid"))
                .ToArray();
            var requestId = context.HttpContext.Request.Headers[LogicFit.Api.RequestIdMiddleware.HeaderName].FirstOrDefault() ?? "missing";
            return new BadRequestObjectResult(new ApiErrorResponse(
                new ApiError("VALIDATION_ERROR", "The request could not be validated.", fieldErrors),
                new ApiMeta(requestId)));
        };
    });
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "LogicFitSession";
    options.DefaultChallengeScheme = "LogicFitSession";
})
.AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>("LogicFitSession", _ => { });
builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    var configuredOrigins = builder.Configuration["LogicFit:Runtime:CorsOrigins"] ?? "http://localhost:5173";
    var origins = configuredOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    options.AddPolicy("web", policy => policy
        .WithOrigins(origins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        var requestId = context.HttpContext.Request.Headers[LogicFit.Api.RequestIdMiddleware.HeaderName].FirstOrDefault() ?? "missing";
        await context.HttpContext.Response.WriteAsJsonAsync(new ApiErrorResponse(
            new ApiError("RATE_LIMITED", "Too many requests. Please try again later."),
            new ApiMeta(requestId)), cancellationToken);
    };
    options.AddPolicy("foundation", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        $"auth:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("mfa", context => RateLimitPartition.GetFixedWindowLimiter(
        $"mfa:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("admin", context => RateLimitPartition.GetFixedWindowLimiter(
        $"admin:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 60,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

var app = builder.Build();

app.UseMiddleware<LogicFit.Api.RequestIdMiddleware>();
app.UseMiddleware<LogicFit.Api.ExceptionHandlingMiddleware>();
app.UseMiddleware<LogicFit.Api.SecurityHeadersMiddleware>();
app.UseMiddleware<LogicFit.Api.AccessLogMiddleware>();
app.UseCors("web");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/api/v1/health", (HttpContext context, IOptions<LogicFitRuntimeOptions> options) =>
{
    var response = new ApiResponse<HealthData>(
        new HealthData("healthy", "api", options.Value.Environment, options.Value.Version),
        ApiMetadata(context, options.Value));
    return Results.Ok(response);
}).RequireRateLimiting("foundation");

app.MapGet("/api/v1/readiness", async (HttpContext context, DatabaseFoundationService databases, IOptions<LogicFitRuntimeOptions> options, CancellationToken cancellationToken) =>
{
    try
    {
        var connections = await databases.CanConnectAsync(cancellationToken);
        var ready = connections.ControlPlane && connections.Gym;
        var response = new ApiResponse<ReadinessData>(
            new ReadinessData(ready ? "ready" : "not_ready", "api", $"control-plane={connections.ControlPlane};gym={connections.Gym}", options.Value.Version),
            ApiMetadata(context, options.Value));
        return ready ? Results.Ok(response) : Results.Json(response, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch
    {
        var response = new ApiResponse<ReadinessData>(
            new ReadinessData("not_ready", "api", "control-plane=false;gym=false", options.Value.Version),
            ApiMetadata(context, options.Value));
        return Results.Json(response, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}).RequireRateLimiting("foundation");

app.MapGet("/api/v1/version", (HttpContext context, IOptions<LogicFitRuntimeOptions> options) =>
{
    var runtime = options.Value;
    return Results.Ok(new ApiResponse<VersionData>(
        new VersionData(runtime.Version, LogicFitApi.Version, runtime.Environment),
        ApiMetadata(context, runtime)));
}).RequireRateLimiting("foundation");

if (args.Contains("--migrate", StringComparer.OrdinalIgnoreCase)
    || args.Contains("--seed", StringComparer.OrdinalIgnoreCase)
    || args.Contains("--verify-seed", StringComparer.OrdinalIgnoreCase))
{
    await RunFoundationCommandAsync(app, args);
    return;
}

app.Run();

static ApiMeta ApiMetadata(HttpContext context, LogicFitRuntimeOptions options)
    => new(context.Request.Headers[LogicFit.Api.RequestIdMiddleware.HeaderName].FirstOrDefault() ?? "missing", options.Version);

static async Task RunFoundationCommandAsync(WebApplication app, string[] args)
{
    await using var scope = app.Services.CreateAsyncScope();
    if (args.Contains("--migrate", StringComparer.OrdinalIgnoreCase))
    {
        await scope.ServiceProvider.GetRequiredService<DatabaseFoundationService>().MigrateAsync();
    }

    if (args.Contains("--seed", StringComparer.OrdinalIgnoreCase))
    {
        var result = await scope.ServiceProvider.GetRequiredService<ISeedCoordinator>().ApplyAsync();
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(result));
    }

    if (args.Contains("--verify-seed", StringComparer.OrdinalIgnoreCase))
    {
        var result = await scope.ServiceProvider.GetRequiredService<ISeedCoordinator>().VerifyAsync();
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(result));
        if (!result.ValidationPassed)
        {
            Environment.ExitCode = 1;
        }
    }
}

public partial class Program;
