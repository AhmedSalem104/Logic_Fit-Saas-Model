using LogicFit.Shared;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LogicFit.Infrastructure.Persistence;

public interface ISqlServerConnectionFactory
{
    string Build(string databaseName);
}

public sealed class SqlServerConnectionFactory(IOptions<SqlServerOptions> options) : ISqlServerConnectionFactory
{
    private readonly SqlServerOptions _options = options.Value;

    public string Build(string databaseName)
    {
        if (string.IsNullOrWhiteSpace(databaseName) || databaseName.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException("SQL Server database name is required and may not contain whitespace.", nameof(databaseName));
        }

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = _options.Server,
            InitialCatalog = databaseName,
            IntegratedSecurity = _options.IntegratedSecurity,
            TrustServerCertificate = _options.TrustServerCertificate,
            Encrypt = _options.Encrypt,
            ApplicationName = "LogicFit.Api"
        };

        if (!_options.IntegratedSecurity)
        {
            builder.UserID = _options.User ?? throw new InvalidOperationException("SQL user is required when integrated security is disabled.");
            builder.Password = _options.Password ?? throw new InvalidOperationException("SQL password is required when integrated security is disabled.");
        }

        return builder.ConnectionString;
    }
}

public interface IGymDbContextFactory
{
    GymDbContext Create(string databaseName);
}

public sealed class GymDbContextFactory(ISqlServerConnectionFactory connectionFactory) : IGymDbContextFactory
{
    public GymDbContext Create(string databaseName)
    {
        var options = new DbContextOptionsBuilder<GymDbContext>()
            .UseSqlServer(connectionFactory.Build(databaseName), sql =>
            {
                sql.MigrationsAssembly(typeof(GymDbContext).Assembly.GetName().Name);
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "dbo");
            })
            .Options;

        return new GymDbContext(options);
    }
}
