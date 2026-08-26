using LogicFit.Application;
using LogicFit.Shared;
using LogicFit.Infrastructure.Persistence;
using LogicFit.Infrastructure.Services.Seeding;
using LogicFit.Infrastructure.Security;
using LogicFit.Infrastructure.Identity;
using LogicFit.Domain.Constants;
using LogicFit.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LogicFit.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddLogicFitInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<SqlServerOptions>().Configure(options =>
        {
            options.Server = configuration["LogicFit:SqlServer:Server"] ?? options.Server;
            options.ControlPlaneDatabase = configuration["LogicFit:SqlServer:ControlPlaneDatabase"] ?? options.ControlPlaneDatabase;
            options.DefaultGymDatabase = configuration["LogicFit:SqlServer:DefaultGymDatabase"] ?? options.DefaultGymDatabase;
            options.IntegratedSecurity = ParseBool(configuration["LogicFit:SqlServer:IntegratedSecurity"], options.IntegratedSecurity);
            options.User = configuration["LogicFit:SqlServer:User"] ?? options.User;
            options.Password = configuration["LogicFit:SqlServer:Password"] ?? options.Password;
            options.TrustServerCertificate = ParseBool(configuration["LogicFit:SqlServer:TrustServerCertificate"], options.TrustServerCertificate);
            options.Encrypt = ParseBool(configuration["LogicFit:SqlServer:Encrypt"], options.Encrypt);
        })
        .Validate(options => !string.IsNullOrWhiteSpace(options.Server), "LogicFit SQL Server name is required.")
        .Validate(options => !string.IsNullOrWhiteSpace(options.ControlPlaneDatabase), "LogicFit Control Plane database name is required.")
        .Validate(options => !string.IsNullOrWhiteSpace(options.DefaultGymDatabase), "LogicFit default Gym database name is required.")
        .Validate(options => options.IntegratedSecurity || (!string.IsNullOrWhiteSpace(options.User) && !string.IsNullOrWhiteSpace(options.Password)), "SQL credentials are required when IntegratedSecurity is false.")
        .ValidateOnStart();
        services.AddOptions<LogicFitRuntimeOptions>().Configure(options =>
        {
            options.Environment = configuration["LogicFit:Runtime:Environment"] ?? options.Environment;
            options.Version = configuration["LogicFit:Runtime:Version"] ?? options.Version;
            options.CorsOrigins = configuration["LogicFit:Runtime:CorsOrigins"] ?? options.CorsOrigins;
            options.MfaIssuer = configuration["LogicFit:Runtime:MfaIssuer"] ?? options.MfaIssuer;
            options.MfaProtectionKeyBase64 = configuration["LogicFit:Runtime:MfaProtectionKeyBase64"] ?? options.MfaProtectionKeyBase64;
        })
        .Validate(options => options.SessionIdleTimeoutSeconds > 0, "Session idle timeout must be positive.")
        .Validate(options => options.SessionAbsoluteLifetimeSeconds >= options.SessionIdleTimeoutSeconds, "Session absolute lifetime must be at least the idle timeout.")
        .Validate(options => options.MfaChallengeSeconds > 0, "MFA challenge lifetime must be positive.")
        .Validate(options => options.PasswordResetSeconds > 0, "Password reset lifetime must be positive.")
        .ValidateOnStart();

        services.AddSingleton<ISqlServerConnectionFactory, SqlServerConnectionFactory>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITotpService, TotpService>();
        services.AddSingleton<IRecoveryCodeGenerator, RecoveryCodeGenerator>();
        services.AddSingleton<ISecretProtector, AesGcmSecretProtector>();
        services.AddSingleton(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<LogicFitRuntimeOptions>>().Value;
            return new SessionPolicy(
                TimeSpan.FromSeconds(options.SessionIdleTimeoutSeconds),
                TimeSpan.FromSeconds(options.SessionAbsoluteLifetimeSeconds),
                TimeSpan.FromSeconds(options.MfaChallengeSeconds),
                TimeSpan.FromSeconds(options.PasswordResetSeconds));
        });
        services.AddDbContext<ControlPlaneDbContext>((serviceProvider, options) =>
        {
            var factory = serviceProvider.GetRequiredService<ISqlServerConnectionFactory>();
            var settings = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SqlServerOptions>>().Value;
            options.UseSqlServer(factory.Build(settings.ControlPlaneDatabase), sql =>
            {
                sql.MigrationsAssembly(typeof(ControlPlaneDbContext).Assembly.GetName().Name);
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "dbo");
            });
        });

        services.AddDbContext<GymDbContext>((serviceProvider, options) =>
        {
            var factory = serviceProvider.GetRequiredService<ISqlServerConnectionFactory>();
            var settings = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SqlServerOptions>>().Value;
            options.UseSqlServer(factory.Build(settings.DefaultGymDatabase), sql =>
            {
                sql.MigrationsAssembly(typeof(GymDbContext).Assembly.GetName().Name);
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "dbo");
            });
        });

        services.AddSingleton<IGymDbContextFactory, GymDbContextFactory>();
        services.AddSingleton<CanonicalSeedManifestReader>();
        services.AddScoped<CanonicalLibrarySeeder>();
        services.AddScoped<IGymDatabaseResolver, GymDatabaseResolver>();
        services.AddScoped<ISessionStore, SqlSessionStore>();
        services.AddScoped<IAuthRepository, SqlAuthRepository>();
        services.AddScoped<ISeedCoordinator, SeedCoordinator>();
        services.AddScoped<DatabaseFoundationService>();
        return services;
    }

    private static bool ParseBool(string? value, bool fallback)
        => bool.TryParse(value, out var parsed) ? parsed : fallback;
}
