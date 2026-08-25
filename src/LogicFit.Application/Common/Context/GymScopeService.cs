using LogicFit.Domain.ValueObjects;

namespace LogicFit.Application;

public sealed class GymScopeService(IGymDatabaseResolver resolver) : IGymScopeService
{
    public async Task<GymScope?> ResolveAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        var route = await resolver.ResolveAsync(gymId, cancellationToken);
        return route is null ? null : new GymScope(route.GymId, route.DatabaseName, route.Status);
    }
}
