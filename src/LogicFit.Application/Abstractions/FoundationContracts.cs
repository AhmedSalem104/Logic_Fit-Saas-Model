using LogicFit.Domain.ValueObjects;

namespace LogicFit.Application;

public sealed record GymDatabaseRoute(Guid GymId, string DatabaseName, string Status);
public sealed record SeedRunResult(string SeedVersion, int Permissions, int Roles, int RolePermissions, int CanonicalLibraryRecords, bool ValidationPassed);

public interface IGymDatabaseResolver
{
    Task<GymDatabaseRoute?> ResolveAsync(Guid gymId, CancellationToken cancellationToken = default);
}

public interface IGymContextAccessor
{
    GymScope? Current { get; set; }
}

public interface ISeedCoordinator
{
    Task<SeedRunResult> ApplyAsync(CancellationToken cancellationToken = default);
    Task<SeedRunResult> VerifyAsync(CancellationToken cancellationToken = default);
}
