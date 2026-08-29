using LogicFit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.IntegrationTests;

public sealed class EfFoundationTests
{
    private const string Server = "localhost";
    private const string ControlPlaneDatabase = "LogicFit_ControlPlane_Local";
    private const string GymDatabase = "LogicFit_Gym_001_Local";

    [Fact]
    public async Task LocalDatabasesUseOfficialEfHistoryAndCanonicalSeed()
    {
        await using var controlPlane = new ControlPlaneDbContext(
            new DbContextOptionsBuilder<ControlPlaneDbContext>()
                .UseSqlServer(Connection(ControlPlaneDatabase), sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "dbo"))
                .Options);
        await using var gym = new GymDbContext(
            new DbContextOptionsBuilder<GymDbContext>()
                .UseSqlServer(Connection(GymDatabase), sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "dbo"))
                .Options);

        Assert.True(await controlPlane.Database.CanConnectAsync());
        Assert.True(await gym.Database.CanConnectAsync());
        Assert.Contains("20260825144155_InitialControlPlaneFoundation", await controlPlane.Database.GetAppliedMigrationsAsync());
        Assert.Contains("20260825144011_InitialGymFoundation", await gym.Database.GetAppliedMigrationsAsync());
        Assert.Equal(16, await controlPlane.Permissions.CountAsync());
        Assert.Equal(3, await controlPlane.Roles.CountAsync());
        Assert.Equal(15, await controlPlane.RolePermissions.CountAsync());
        Assert.Equal(1133, await gym.Exercises.CountAsync());
        Assert.Equal(367, await gym.Foods.CountAsync());
        Assert.Equal(297, await gym.Muscles.CountAsync());
        Assert.Equal(194, await gym.AnatomyMappings.CountAsync());
        Assert.Equal(0, await gym.Exercises.GroupBy(x => x.SeedKey).Where(x => x.Count() > 1).CountAsync());
        Assert.Equal(0, await gym.Foods.GroupBy(x => x.SeedKey).Where(x => x.Count() > 1).CountAsync());
    }

    [Fact]
    public void EfModelsExposeTheFoundationContextsWithoutNodeRuntimeTypes()
    {
        var controlPlaneOptions = new DbContextOptionsBuilder<ControlPlaneDbContext>()
            .UseSqlServer(Connection(ControlPlaneDatabase))
            .Options;
        var gymOptions = new DbContextOptionsBuilder<GymDbContext>()
            .UseSqlServer(Connection(GymDatabase))
            .Options;

        using var controlPlane = new ControlPlaneDbContext(controlPlaneOptions);
        using var gym = new GymDbContext(gymOptions);

        Assert.NotNull(controlPlane.Model.FindEntityType("LogicFit.Infrastructure.Persistence.Entities.UserEntity"));
        Assert.NotNull(controlPlane.Model.FindEntityType("LogicFit.Infrastructure.Persistence.Entities.SessionEntity"));
        Assert.NotNull(gym.Model.FindEntityType("LogicFit.Infrastructure.Persistence.Entities.ExerciseEntity"));
        Assert.NotNull(gym.Model.FindEntityType("LogicFit.Infrastructure.Persistence.Entities.FoodEntity"));
    }

    private static string Connection(string database)
        => $"Server={Server};Database={database};Integrated Security=True;TrustServerCertificate=True;Encrypt=False";
}
