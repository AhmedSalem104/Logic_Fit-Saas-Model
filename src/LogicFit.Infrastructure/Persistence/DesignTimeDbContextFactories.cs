using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Data.SqlClient;

namespace LogicFit.Infrastructure.Persistence;

public sealed class DesignTimeControlPlaneDbContextFactory : IDesignTimeDbContextFactory<ControlPlaneDbContext>
{
    public ControlPlaneDbContext CreateDbContext(string[] args)
    {
        var database = Environment.GetEnvironmentVariable("LOGICFIT__SQLSERVER__CONTROLPLANEDATABASE")
            ?? "LogicFit_ControlPlane_Local";
        var options = new DbContextOptionsBuilder<ControlPlaneDbContext>()
            .UseSqlServer(BuildConnectionString(database), sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "dbo"))
            .Options;
        return new ControlPlaneDbContext(options);
    }

    internal static string BuildConnectionString(string database)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = Environment.GetEnvironmentVariable("LOGICFIT__SQLSERVER__SERVER") ?? "localhost",
            InitialCatalog = database,
            IntegratedSecurity = true,
            TrustServerCertificate = true,
            Encrypt = false,
            ApplicationName = "LogicFit.EF.Design"
        };
        return builder.ConnectionString;
    }
}

public sealed class DesignTimeGymDbContextFactory : IDesignTimeDbContextFactory<GymDbContext>
{
    public GymDbContext CreateDbContext(string[] args)
    {
        var database = Environment.GetEnvironmentVariable("LOGICFIT__SQLSERVER__DEFAULTGYMDATABASE")
            ?? "LogicFit_Gym_001_Local";
        var options = new DbContextOptionsBuilder<GymDbContext>()
            .UseSqlServer(DesignTimeControlPlaneDbContextFactory.BuildConnectionString(database), sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "dbo"))
            .Options;
        return new GymDbContext(options);
    }
}
