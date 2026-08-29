using LogicFit.Infrastructure.Persistence.Entities;

namespace LogicFit.Infrastructure.Persistence;

public static class PlatformServerDefaults
{
    public static readonly Guid LocalServerId = Guid.Parse("5e5f5f7e-31d2-4f0c-9ec4-0fcf3fdbac73");
    public const string LocalServerName = "LogicFit Local SQL Server";
    public const string LocalEnvironment = "local";
    public const string LocalProviderKey = "sql-server-local";
    public const string LocalEndpointRef = "configured-local-sql-server";

    public static ServerEntity CreateLocal(DateTime nowUtc)
        => new()
        {
            ServerId = LocalServerId,
            Name = LocalServerName,
            Environment = LocalEnvironment,
            ProviderKey = LocalProviderKey,
            Status = "active",
            HealthStatus = "healthy",
            EndpointRef = LocalEndpointRef,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };
}
