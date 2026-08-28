using LogicFit.Application;
using LogicFit.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LogicFit.Infrastructure.Persistence;

public sealed class SqlPlatformRepository(ControlPlaneDbContext db) : IPlatformRepository
{
    public async Task<PlatformRegistryCounts> GetRegistryCountsAsync(CancellationToken cancellationToken = default)
    {
        var organizationCount = await db.Organizations.AsNoTracking().CountAsync(cancellationToken);
        var gymCount = await db.Gyms.AsNoTracking().CountAsync(cancellationToken);
        var gymByStatusRows = await db.Gyms.AsNoTracking()
            .GroupBy(gym => gym.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .OrderBy(item => item.Status)
            .ToListAsync(cancellationToken);
        var gymByStatus = gymByStatusRows
            .Select(item => new PlatformStatusCountDto(item.Status, item.Count))
            .ToArray();
        var databaseCount = await db.GymDatabases.AsNoTracking().CountAsync(cancellationToken);
        var databaseByStatusRows = await db.GymDatabases.AsNoTracking()
            .GroupBy(database => database.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .OrderBy(item => item.Status)
            .ToListAsync(cancellationToken);
        var databaseByStatus = databaseByStatusRows
            .Select(item => new PlatformStatusCountDto(item.Status, item.Count))
            .ToArray();

        return new PlatformRegistryCounts(organizationCount, gymCount, gymByStatus, databaseCount, databaseByStatus);
    }

    public async Task<PlatformPage<OrganizationSummaryDto>> ListOrganizationsAsync(
        OrganizationListQuery query,
        CancellationToken cancellationToken = default)
    {
        var organizations = db.Organizations.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            organizations = organizations.Where(organization => organization.Name.Contains(search) || organization.Slug.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            organizations = organizations.Where(organization => organization.Status == query.Status);
        }

        organizations = ApplyOrganizationSort(organizations, query.SortField, query.SortDescending);
        var total = await organizations.CountAsync(cancellationToken);
        var items = await organizations
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(organization => new OrganizationSummaryDto(
                organization.OrganizationId,
                organization.Name,
                organization.Slug,
                organization.Status,
                organization.CreatedAtUtc,
                organization.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return Page(items, query.Page, query.PageSize, total);
    }

    public Task<OrganizationSummaryDto?> GetOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
        => db.Organizations.AsNoTracking()
            .Where(organization => organization.OrganizationId == organizationId)
            .Select(organization => new OrganizationSummaryDto(
                organization.OrganizationId,
                organization.Name,
                organization.Slug,
                organization.Status,
                organization.CreatedAtUtc,
                organization.UpdatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<PlatformPage<GymSummaryDto>> ListGymsAsync(
        GymListQuery query,
        CancellationToken cancellationToken = default)
    {
        var gyms = db.Gyms.AsNoTracking();
        if (query.OrganizationId.HasValue)
        {
            gyms = gyms.Where(gym => gym.OrganizationId == query.OrganizationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            gyms = gyms.Where(gym => gym.Name.Contains(search) || gym.Slug.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            gyms = gyms.Where(gym => gym.Status == query.Status);
        }

        gyms = ApplyGymSort(gyms, query.SortField, query.SortDescending);
        var total = await gyms.CountAsync(cancellationToken);
        var items = await gyms
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(gym => new GymSummaryDto(
                gym.GymId,
                gym.OrganizationId,
                gym.Name,
                gym.Slug,
                gym.Status,
                gym.TimezoneName,
                gym.CreatedAtUtc,
                gym.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return Page(items, query.Page, query.PageSize, total);
    }

    public async Task<GymDetailDto?> GetGymAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        var gym = await db.Gyms.AsNoTracking()
            .Where(item => item.GymId == gymId)
            .Select(item => new GymSummaryDto(
                item.GymId,
                item.OrganizationId,
                item.Name,
                item.Slug,
                item.Status,
                item.TimezoneName,
                item.CreatedAtUtc,
                item.UpdatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

        if (gym is null)
        {
            return null;
        }

        var databases = await db.GymDatabases.AsNoTracking()
            .Where(database => database.GymId == gymId)
            .OrderBy(database => database.DatabaseName)
            .ThenBy(database => database.GymDatabaseId)
            .Select(DatabaseSummaryProjection)
            .ToListAsync(cancellationToken);

        return new GymDetailDto(
            gym.GymId,
            gym.OrganizationId,
            gym.Name,
            gym.Slug,
            gym.Status,
            gym.TimezoneName,
            gym.CreatedAtUtc,
            gym.UpdatedAtUtc,
            databases);
    }

    public async Task<PlatformPage<DatabaseRegistrySummaryDto>> ListDatabasesAsync(
        DatabaseListQuery query,
        CancellationToken cancellationToken = default)
    {
        var databases = db.GymDatabases.AsNoTracking();
        if (query.GymId.HasValue)
        {
            databases = databases.Where(database => database.GymId == query.GymId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Environment))
        {
            databases = databases.Where(database => database.Environment == query.Environment);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            databases = databases.Where(database => database.Status == query.Status);
        }

        databases = ApplyDatabaseSort(databases, query.SortField, query.SortDescending);
        var total = await databases.CountAsync(cancellationToken);
        var items = await databases
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(DatabaseSummaryProjection)
            .ToListAsync(cancellationToken);

        return Page(items, query.Page, query.PageSize, total);
    }

    public Task<DatabaseRegistrySummaryDto?> GetDatabaseAsync(Guid databaseId, CancellationToken cancellationToken = default)
        => db.GymDatabases.AsNoTracking()
            .Where(database => database.GymDatabaseId == databaseId)
            .Select(DatabaseSummaryProjection)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<DatabaseRegistrySummaryDto>> ListRegisteredDatabasesAsync(CancellationToken cancellationToken = default)
        => await db.GymDatabases.AsNoTracking()
            .OrderBy(database => database.DatabaseName)
            .ThenBy(database => database.GymDatabaseId)
            .Select(DatabaseSummaryProjection)
            .ToListAsync(cancellationToken);

    private static IQueryable<OrganizationEntity> ApplyOrganizationSort(
        IQueryable<OrganizationEntity> query,
        string? sortField,
        bool descending)
    {
        return (sortField?.ToLowerInvariant(), descending) switch
        {
            ("name", false) => query.OrderBy(item => item.Name).ThenBy(item => item.OrganizationId),
            ("name", true) => query.OrderByDescending(item => item.Name).ThenBy(item => item.OrganizationId),
            ("slug", false) => query.OrderBy(item => item.Slug).ThenBy(item => item.OrganizationId),
            ("slug", true) => query.OrderByDescending(item => item.Slug).ThenBy(item => item.OrganizationId),
            ("updatedatutc", false) => query.OrderBy(item => item.UpdatedAtUtc).ThenBy(item => item.OrganizationId),
            ("updatedatutc", true) => query.OrderByDescending(item => item.UpdatedAtUtc).ThenBy(item => item.OrganizationId),
            ("createdatutc", false) => query.OrderBy(item => item.CreatedAtUtc).ThenBy(item => item.OrganizationId),
            _ => query.OrderByDescending(item => item.CreatedAtUtc).ThenBy(item => item.OrganizationId)
        };
    }

    private static IQueryable<GymEntity> ApplyGymSort(
        IQueryable<GymEntity> query,
        string? sortField,
        bool descending)
    {
        return (sortField?.ToLowerInvariant(), descending) switch
        {
            ("name", false) => query.OrderBy(item => item.Name).ThenBy(item => item.GymId),
            ("name", true) => query.OrderByDescending(item => item.Name).ThenBy(item => item.GymId),
            ("slug", false) => query.OrderBy(item => item.Slug).ThenBy(item => item.GymId),
            ("slug", true) => query.OrderByDescending(item => item.Slug).ThenBy(item => item.GymId),
            ("updatedatutc", false) => query.OrderBy(item => item.UpdatedAtUtc).ThenBy(item => item.GymId),
            ("updatedatutc", true) => query.OrderByDescending(item => item.UpdatedAtUtc).ThenBy(item => item.GymId),
            ("createdatutc", false) => query.OrderBy(item => item.CreatedAtUtc).ThenBy(item => item.GymId),
            _ => query.OrderByDescending(item => item.CreatedAtUtc).ThenBy(item => item.GymId)
        };
    }

    private static IQueryable<GymDatabaseEntity> ApplyDatabaseSort(
        IQueryable<GymDatabaseEntity> query,
        string? sortField,
        bool descending)
    {
        return (sortField?.ToLowerInvariant(), descending) switch
        {
            ("databasename", false) => query.OrderBy(item => item.DatabaseName).ThenBy(item => item.GymDatabaseId),
            ("databasename", true) => query.OrderByDescending(item => item.DatabaseName).ThenBy(item => item.GymDatabaseId),
            ("status", false) => query.OrderBy(item => item.Status).ThenBy(item => item.GymDatabaseId),
            ("status", true) => query.OrderByDescending(item => item.Status).ThenBy(item => item.GymDatabaseId),
            ("lasthealthatutc", false) => query.OrderBy(item => item.LastHealthAtUtc).ThenBy(item => item.GymDatabaseId),
            _ => query.OrderByDescending(item => item.LastHealthAtUtc).ThenBy(item => item.GymDatabaseId)
        };
    }

    private static Expression<Func<GymDatabaseEntity, DatabaseRegistrySummaryDto>> DatabaseSummaryProjection
        => database => new DatabaseRegistrySummaryDto(
            database.GymDatabaseId,
            database.GymId,
            database.DatabaseName,
            database.Environment,
            database.SchemaVersion,
            database.SeedVersion,
            database.Status,
            database.LastHealthAtUtc);

    private static PlatformPage<T> Page<T>(IReadOnlyList<T> items, int page, int pageSize, int total)
        => new(items, page, pageSize, total, (long)page * pageSize < total);
}
