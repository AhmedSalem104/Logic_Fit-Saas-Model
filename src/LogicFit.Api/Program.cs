using LogicFit.Application;
using LogicFit.Infrastructure;
using LogicFit.Infrastructure.Persistence;
using LogicFit.Shared;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options => options.JsonWriterOptions = new System.Text.Json.JsonWriterOptions { Indented = false });
builder.Services.AddHttpContextAccessor();
builder.Services.AddLogicFitApplication();
builder.Services.AddLogicFitInfrastructure(builder.Configuration);
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
    options.AddPolicy("foundation", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
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
app.UseAuthorization();

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
