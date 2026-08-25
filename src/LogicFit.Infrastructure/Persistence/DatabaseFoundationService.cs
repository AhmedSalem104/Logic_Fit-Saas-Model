using Microsoft.EntityFrameworkCore;

namespace LogicFit.Infrastructure.Persistence;

public sealed class DatabaseFoundationService(
    ControlPlaneDbContext controlPlane,
    GymDbContext defaultGym)
{
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        await controlPlane.Database.MigrateAsync(cancellationToken);
        await defaultGym.Database.MigrateAsync(cancellationToken);
    }

    public async Task<(bool ControlPlane, bool Gym)> CanConnectAsync(CancellationToken cancellationToken = default)
    {
        var controlPlaneConnected = await controlPlane.Database.CanConnectAsync(cancellationToken);
        var gymConnected = await defaultGym.Database.CanConnectAsync(cancellationToken);
        return (controlPlaneConnected, gymConnected);
    }
}
