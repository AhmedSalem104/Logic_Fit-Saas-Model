using LogicFit.Shared;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LogicFit.Infrastructure.Persistence;

public interface ISqlServerConnectionFactory
{
    string Build(string databaseName);
    string BuildMaster();
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

    public string BuildMaster()
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = _options.Server,
            InitialCatalog = "master",
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

public interface ISqlServerDatabaseCreator
{
    Task EnsureCreatedAsync(string databaseName, CancellationToken cancellationToken = default);
}

public sealed class SqlServerDatabaseCreator(ISqlServerConnectionFactory connectionFactory) : ISqlServerDatabaseCreator
{
    public async Task EnsureCreatedAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(databaseName, "^LogicFit_Gym_[0-9a-f]{32}_[a-z0-9-]+$", System.Text.RegularExpressions.RegexOptions.CultureInvariant))
        {
            throw new ArgumentException("The generated Gym database name is invalid.", nameof(databaseName));
        }

        await using var connection = new SqlConnection(connectionFactory.BuildMaster());
        await connection.OpenAsync(cancellationToken);

        await using var existsCommand = connection.CreateCommand();
        existsCommand.CommandText = "SELECT DB_ID(@databaseName);";
        existsCommand.Parameters.Add(new SqlParameter("@databaseName", System.Data.SqlDbType.NVarChar, 128) { Value = databaseName });
        var databaseId = await existsCommand.ExecuteScalarAsync(cancellationToken);
        if (databaseId is not null && databaseId != DBNull.Value)
        {
            return;
        }

        await using var createCommand = connection.CreateCommand();
        createCommand.CommandText = $"CREATE DATABASE {QuoteIdentifier(databaseName)};";
        try
        {
            await createCommand.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqlException exception) when (exception.Number == 1801)
        {
            // Another idempotent request won the race to create the same database.
        }
    }

    private static string QuoteIdentifier(string value) => $"[{value.Replace("]", "]]", StringComparison.Ordinal)}]";
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
