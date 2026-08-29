using System.Security.Cryptography;
using System.Text;
using LogicFit.Application;
using LogicFit.Domain.Constants;
using LogicFit.Infrastructure.Persistence;
using LogicFit.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Infrastructure.Services.Seeding;

public sealed class SeedCoordinator(
    ControlPlaneDbContext controlPlane,
    GymDbContext defaultGym,
    CanonicalSeedManifestReader manifestReader,
    CanonicalLibrarySeeder librarySeeder) : ISeedCoordinator
{
    public async Task<SeedRunResult> ApplyAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = await controlPlane.Database.BeginTransactionAsync(cancellationToken);
        await ApplyPlatformServerCatalogAsync(cancellationToken);
        await ApplyPermissionCatalogAsync(cancellationToken);
        await controlPlane.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await librarySeeder.ApplyAsync(cancellationToken);

        return await VerifyAsync(cancellationToken);
    }

    public async Task<SeedRunResult> VerifyAsync(CancellationToken cancellationToken = default)
    {
        var manifest = manifestReader.Read();
        var permissionCount = await controlPlane.Permissions.CountAsync(cancellationToken);
        var roleCount = await controlPlane.Roles.CountAsync(cancellationToken);
        var rolePermissionCount = await controlPlane.RolePermissions.CountAsync(cancellationToken);

        var expectedLibraryCounts = manifest.Datasets.ToDictionary(x => x.Dataset, x => x.RecordCount, StringComparer.OrdinalIgnoreCase);
        var actualLibraryCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["muscle-groups"] = await defaultGym.MuscleGroups.CountAsync(cancellationToken),
            ["muscles"] = await defaultGym.Muscles.CountAsync(cancellationToken),
            ["equipment"] = await defaultGym.Equipment.CountAsync(cancellationToken),
            ["exercise-categories"] = await defaultGym.ExerciseCategories.CountAsync(cancellationToken),
            ["levels"] = await defaultGym.Levels.CountAsync(cancellationToken),
            ["exercises"] = await defaultGym.Exercises.CountAsync(cancellationToken),
            ["anatomy-mappings"] = await defaultGym.AnatomyMappings.CountAsync(cancellationToken),
            ["food-categories"] = await defaultGym.FoodCategories.CountAsync(cancellationToken),
            ["units"] = await defaultGym.FoodUnits.CountAsync(cancellationToken),
            ["foods"] = await defaultGym.Foods.CountAsync(cancellationToken)
        };

        var libraryValid = expectedLibraryCounts
            .Where(x => actualLibraryCounts.ContainsKey(x.Key))
            .All(x => actualLibraryCounts[x.Key] == x.Value);
        var authValid = permissionCount == PermissionCatalog.Permissions.Count
            && roleCount == PermissionCatalog.Roles.Count
            && rolePermissionCount == PermissionCatalog.RolePermissions.Count;
        var allValid = libraryValid && authValid;

        return new SeedRunResult(
            manifest.SeedVersion,
            permissionCount,
            roleCount,
            rolePermissionCount,
            manifest.TotalRecordCount,
            allValid);
    }

    private async Task ApplyPermissionCatalogAsync(CancellationToken cancellationToken)
    {
        foreach (var definition in PermissionCatalog.Permissions)
        {
            var entity = await controlPlane.Permissions.FirstOrDefaultAsync(x => x.PermissionKey == definition.Key, cancellationToken);
            if (entity is null)
            {
                entity = new PermissionEntity { PermissionId = StableGuid.For("permission", definition.Key), PermissionKey = definition.Key };
                controlPlane.Permissions.Add(entity);
            }

            entity.Domain = definition.Domain;
            entity.Action = definition.Action;
            entity.RiskLevel = definition.RiskLevel;
            entity.Description = definition.Description;
            entity.UpdatedAtUtc = DateTime.UtcNow;
        }

        foreach (var definition in PermissionCatalog.Roles)
        {
            var entity = await controlPlane.Roles.FirstOrDefaultAsync(x => x.ScopeType == definition.ScopeType && x.Name == definition.Name, cancellationToken);
            if (entity is null)
            {
                entity = new RoleEntity { RoleId = StableGuid.For("role", definition.Key), ScopeType = definition.ScopeType, Name = definition.Name };
                controlPlane.Roles.Add(entity);
            }

            entity.Status = "active";
            entity.UpdatedAtUtc = DateTime.UtcNow;
        }

        await controlPlane.SaveChangesAsync(cancellationToken);

        foreach (var definition in PermissionCatalog.RolePermissions)
        {
            var roleDefinition = PermissionCatalog.Roles.First(x => x.Key == definition.RoleKey);
            var role = await controlPlane.Roles.FirstAsync(x => x.ScopeType == roleDefinition.ScopeType && x.Name == roleDefinition.Name, cancellationToken);
            var permission = await controlPlane.Permissions.FirstAsync(x => x.PermissionKey == definition.PermissionKey, cancellationToken);
            var exists = await controlPlane.RolePermissions.AnyAsync(x => x.RoleId == role.RoleId && x.PermissionId == permission.PermissionId, cancellationToken);
            if (!exists)
            {
                controlPlane.RolePermissions.Add(new RolePermissionEntity
                {
                    RolePermissionId = StableGuid.For("role-permission", $"{role.RoleId:N}:{permission.PermissionId:N}"),
                    RoleId = role.RoleId,
                    PermissionId = permission.PermissionId,
                    ScopeRuleJson = null
                });
            }
        }
    }

    private async Task ApplyPlatformServerCatalogAsync(CancellationToken cancellationToken)
    {
        var server = await controlPlane.Servers.FirstOrDefaultAsync(x => x.ServerId == PlatformServerDefaults.LocalServerId, cancellationToken);
        if (server is null)
        {
            server = new ServerEntity
            {
                ServerId = PlatformServerDefaults.LocalServerId,
                Name = PlatformServerDefaults.LocalServerName,
                Environment = PlatformServerDefaults.LocalEnvironment,
                ProviderKey = PlatformServerDefaults.LocalProviderKey,
                Status = "active",
                HealthStatus = "healthy",
                EndpointRef = PlatformServerDefaults.LocalEndpointRef,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            controlPlane.Servers.Add(server);
        }
        else
        {
            server.Name = PlatformServerDefaults.LocalServerName;
            server.Environment = PlatformServerDefaults.LocalEnvironment;
            server.ProviderKey = PlatformServerDefaults.LocalProviderKey;
            server.Status = "active";
            server.HealthStatus = "healthy";
            server.EndpointRef = PlatformServerDefaults.LocalEndpointRef;
            server.UpdatedAtUtc = DateTime.UtcNow;
        }

        await controlPlane.SaveChangesAsync(cancellationToken);
    }

    private static class StableGuid
    {
        public static Guid For(string namespaceName, string value)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"logicfit:{namespaceName}:{value}"));
            var guidBytes = bytes[..16];
            guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | 0x50);
            guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);
            return new Guid(guidBytes);
        }
    }
}
