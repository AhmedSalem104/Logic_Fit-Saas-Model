using LogicFit.Application;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Infrastructure.Persistence;

public sealed class GymDatabaseResolver(ControlPlaneDbContext controlPlane) : IGymDatabaseResolver
{
    public async Task<GymDatabaseRoute?> ResolveAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        return await controlPlane.GymDatabases
            .AsNoTracking()
            .Where(x => x.GymId == gymId && x.Environment == "local" && x.Status != "disabled")
            .OrderByDescending(x => x.Status == "healthy")
            .Select(x => new GymDatabaseRoute(x.GymId, x.DatabaseName, x.Status))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
