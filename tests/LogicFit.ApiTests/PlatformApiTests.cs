using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LogicFit.Infrastructure.Persistence;
using LogicFit.Infrastructure.Persistence.Entities;
using LogicFit.Infrastructure.Security;
using LogicFit.Shared;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.ApiTests;

public sealed class PlatformApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string ConnectionString = "Server=localhost;Database=LogicFit_ControlPlane_Local;Integrated Security=True;TrustServerCertificate=True;Encrypt=False";
    private const string Password = "Local Test Password 123!";
    private readonly WebApplicationFactory<Program> factory;

    public PlatformApiTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task PlatformUserCanReadAllEightApprovedPlatformEndpoints()
    {
        var identity = await CreateIdentityAsync("Platform Security Admin");
        var registry = await ReadRegistryAsync();
        try
        {
            using var client = factory.CreateClient();
            var login = await LoginAsync(client, identity.Email);
            client.DefaultRequestHeaders.Authorization = Bearer(login.AccessToken);

            using var overviewResponse = await client.GetAsync("/api/v1/platform/overview");
            Assert.Equal(HttpStatusCode.OK, overviewResponse.StatusCode);
            var overview = (await overviewResponse.Content.ReadFromJsonAsync<ApiResponse<PlatformOverviewData>>())!.Data;
            Assert.Equal("api", overview.PlatformHealth.Service);
            Assert.Equal(registry.OrganizationCount, overview.OrganizationCount);
            Assert.Equal(registry.GymCount, overview.GymCounts.Total);
            Assert.Equal(registry.DatabaseCount, overview.DatabaseCounts.Total);

            using var organizationsResponse = await client.GetAsync("/api/v1/platform/organizations?page=1&pageSize=100&sort=name:asc");
            Assert.Equal(HttpStatusCode.OK, organizationsResponse.StatusCode);
            var organizations = await organizationsResponse.Content.ReadFromJsonAsync<ApiCollectionResponse<OrganizationData>>();
            Assert.NotNull(organizations);
            Assert.Equal(registry.OrganizationCount, organizations.Meta.Total);
            Assert.Contains(organizations.Data, item => item.OrganizationId == registry.OrganizationId);

            using var organizationResponse = await client.GetAsync($"/api/v1/platform/organizations/{registry.OrganizationId:D}");
            Assert.Equal(HttpStatusCode.OK, organizationResponse.StatusCode);
            var organization = (await organizationResponse.Content.ReadFromJsonAsync<ApiResponse<OrganizationData>>())!.Data;
            Assert.Equal(registry.OrganizationId, organization.OrganizationId);

            using var gymsResponse = await client.GetAsync($"/api/v1/gyms?organizationId={registry.OrganizationId:D}&page=1&pageSize=100");
            Assert.Equal(HttpStatusCode.OK, gymsResponse.StatusCode);
            var gyms = await gymsResponse.Content.ReadFromJsonAsync<ApiCollectionResponse<GymData>>();
            Assert.NotNull(gyms);
            Assert.Contains(gyms.Data, item => item.GymId == registry.GymId);

            using var gymResponse = await client.GetAsync($"/api/v1/gyms/{registry.GymId:D}");
            Assert.Equal(HttpStatusCode.OK, gymResponse.StatusCode);
            var gym = (await gymResponse.Content.ReadFromJsonAsync<ApiResponse<GymDetailData>>())!.Data;
            Assert.Equal(registry.GymId, gym.GymId);
            Assert.Contains(gym.Databases, item => item.GymDatabaseId == registry.DatabaseId);

            using var databasesResponse = await client.GetAsync($"/api/v1/platform/databases?gymId={registry.GymId:D}&page=1&pageSize=100");
            Assert.Equal(HttpStatusCode.OK, databasesResponse.StatusCode);
            var databases = await databasesResponse.Content.ReadFromJsonAsync<ApiCollectionResponse<DatabaseData>>();
            Assert.NotNull(databases);
            Assert.Contains(databases.Data, item => item.GymDatabaseId == registry.DatabaseId);
            var databaseJson = await databasesResponse.Content.ReadAsStringAsync();
            Assert.DoesNotContain("connectionSecretRef", databaseJson, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("secret", databaseJson, StringComparison.OrdinalIgnoreCase);

            using var databaseResponse = await client.GetAsync($"/api/v1/platform/databases/{registry.DatabaseId:D}");
            Assert.Equal(HttpStatusCode.OK, databaseResponse.StatusCode);
            var database = (await databaseResponse.Content.ReadFromJsonAsync<ApiResponse<DatabaseData>>())!.Data;
            Assert.Equal(registry.DatabaseId, database.GymDatabaseId);

            using var monitoringResponse = await client.GetAsync("/api/v1/platform/monitoring");
            Assert.Equal(HttpStatusCode.OK, monitoringResponse.StatusCode);
            var monitoring = (await monitoringResponse.Content.ReadFromJsonAsync<ApiResponse<MonitoringData>>())!.Data;
            Assert.Equal("healthy", monitoring.PlatformHealth.Status);
            Assert.Contains(monitoring.RegisteredDatabases, item => item.GymDatabaseId == registry.DatabaseId);
        }
        finally
        {
            await DeleteIdentityAsync(identity.UserId);
        }
    }

    [Theory]
    [InlineData("/api/v1/platform/overview")]
    [InlineData("/api/v1/platform/organizations")]
    [InlineData("/api/v1/platform/organizations/00000000-0000-0000-0000-000000000000")]
    [InlineData("/api/v1/gyms")]
    [InlineData("/api/v1/gyms/00000000-0000-0000-0000-000000000000")]
    [InlineData("/api/v1/platform/databases")]
    [InlineData("/api/v1/platform/databases/00000000-0000-0000-0000-000000000000")]
    [InlineData("/api/v1/platform/monitoring")]
    public async Task PlatformEndpointsRequireAuthentication(string route)
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("AUTHENTICATION_REQUIRED", body.Error.Code);
    }

    [Fact]
    public async Task GymScopedUserCannotReadPlatformEndpointsWithoutPlatformViewPermission()
    {
        var identity = await CreateIdentityAsync("Gym Authenticated User");
        try
        {
            using var client = factory.CreateClient();
            var login = await LoginAsync(client, identity.Email);
            client.DefaultRequestHeaders.Authorization = Bearer(login.AccessToken);

            using var response = await client.GetAsync("/api/v1/platform/overview");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
            Assert.NotNull(body);
            Assert.Equal("PERMISSION_DENIED", body.Error.Code);
        }
        finally
        {
            await DeleteIdentityAsync(identity.UserId);
        }
    }

    [Fact]
    public async Task PlatformCollectionRejectsInvalidFiltersAndDetailsReturnNotFound()
    {
        var identity = await CreateIdentityAsync("Platform Security Admin");
        try
        {
            using var client = factory.CreateClient();
            var login = await LoginAsync(client, identity.Email);
            client.DefaultRequestHeaders.Authorization = Bearer(login.AccessToken);

            using var invalidPage = await client.GetAsync("/api/v1/platform/organizations?page=0&pageSize=101");
            Assert.Equal(HttpStatusCode.BadRequest, invalidPage.StatusCode);
            var invalidBody = await invalidPage.Content.ReadFromJsonAsync<ApiErrorResponse>();
            Assert.NotNull(invalidBody);
            Assert.Equal("INVALID_FILTER", invalidBody.Error.Code);

            using var invalidSort = await client.GetAsync("/api/v1/platform/databases?sort=connectionString:asc");
            Assert.Equal(HttpStatusCode.BadRequest, invalidSort.StatusCode);

            using var missingOrganization = await client.GetAsync("/api/v1/platform/organizations/00000000-0000-0000-0000-000000000000");
            Assert.Equal(HttpStatusCode.NotFound, missingOrganization.StatusCode);

            using var missingGym = await client.GetAsync("/api/v1/gyms/00000000-0000-0000-0000-000000000000");
            Assert.Equal(HttpStatusCode.NotFound, missingGym.StatusCode);
        }
        finally
        {
            await DeleteIdentityAsync(identity.UserId);
        }
    }

    private async Task<LoginData> LoginAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<LoginData>>())!.Data;
    }

    private static async Task<TestIdentity> CreateIdentityAsync(string roleName)
    {
        await using var db = CreateDb();
        var role = await db.Roles.SingleAsync(item => item.Name == roleName);
        var gymId = await db.Gyms.AsNoTracking().OrderBy(item => item.GymId).Select(item => (Guid?)item.GymId).FirstOrDefaultAsync();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var user = new UserEntity
        {
            UserId = userId,
            Email = $"phase6-platform-{userId:N}@example.test",
            DisplayName = $"Phase 6 {roleName}",
            Status = "active",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        user.Credentials.Add(new CredentialEntity
        {
            CredentialId = Guid.NewGuid(),
            UserId = userId,
            CredentialType = "password",
            SecretHash = new Pbkdf2PasswordHasher().Hash(Password),
            SecretVersion = "lf-pbkdf2-sha256-v1",
            LastRotatedAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        user.GymRoles.Add(new UserGymRoleEntity
        {
            AssignmentId = Guid.NewGuid(),
            UserId = userId,
            GymId = role.ScopeType == "gym" ? gymId : null,
            RoleId = role.RoleId,
            ScopeType = role.ScopeType,
            Status = "active",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return new TestIdentity(userId, user.Email);
    }

    private static async Task<RegistryIdentity> ReadRegistryAsync()
    {
        await using var db = CreateDb();
        var organization = await db.Organizations.AsNoTracking().OrderBy(item => item.OrganizationId).FirstAsync();
        var gym = await db.Gyms.AsNoTracking().OrderBy(item => item.GymId).FirstAsync();
        var database = await db.GymDatabases.AsNoTracking().Where(item => item.GymId == gym.GymId).OrderBy(item => item.GymDatabaseId).FirstAsync();
        return new RegistryIdentity(organization.OrganizationId, gym.GymId, database.GymDatabaseId, await db.Organizations.CountAsync(), await db.Gyms.CountAsync(), await db.GymDatabases.CountAsync());
    }

    private static ControlPlaneDbContext CreateDb()
        => new(new DbContextOptionsBuilder<ControlPlaneDbContext>().UseSqlServer(ConnectionString).Options);

    private static async Task DeleteIdentityAsync(Guid userId)
    {
        await using var db = CreateDb();
        await db.MfaRecoveryCodes.Where(item => item.UserId == userId).ExecuteDeleteAsync();
        await db.MfaFactors.Where(item => item.UserId == userId).ExecuteDeleteAsync();
        await db.PasswordResetTokens.Where(item => item.UserId == userId).ExecuteDeleteAsync();
        await db.Sessions.Where(item => item.UserId == userId).ExecuteDeleteAsync();
        await db.Credentials.Where(item => item.UserId == userId).ExecuteDeleteAsync();
        await db.UserGymRoles.Where(item => item.UserId == userId).ExecuteDeleteAsync();
        await db.AuditEvents.Where(item => item.ActorUserId == userId || item.TargetId == userId).ExecuteDeleteAsync();
        await db.Users.Where(item => item.UserId == userId).ExecuteDeleteAsync();
    }

    private static AuthenticationHeaderValue Bearer(string token) => new("Bearer", token);

    private sealed record TestIdentity(Guid UserId, string Email);
    private sealed record RegistryIdentity(Guid OrganizationId, Guid GymId, Guid DatabaseId, int OrganizationCount, int GymCount, int DatabaseCount);
    private sealed record LoginData(string AccessToken, Guid SessionId, bool RequiresMfa, string? Challenge, bool MfaVerified, DateTime ExpiresAtUtc, DateTime IdleExpiresAtUtc, DateTime AbsoluteExpiresAtUtc, UserData User);
    private sealed record UserData(Guid UserId, string Email, string DisplayName, string Status, DateTime? LastLoginAtUtc, string Version);
    private sealed record PlatformHealthData(string Status, string Service, string Version, string Environment);
    private sealed record PlatformCountsData(int Total, IReadOnlyList<StatusCountData> ByStatus);
    private sealed record StatusCountData(string Status, int Count);
    private sealed record PlatformOverviewData(DateTime ObservedAtUtc, PlatformHealthData PlatformHealth, int OrganizationCount, PlatformCountsData GymCounts, PlatformCountsData DatabaseCounts);
    private sealed record OrganizationData(Guid OrganizationId, string Name, string Slug, string Status, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
    private sealed record GymData(Guid GymId, Guid OrganizationId, string Name, string Slug, string Status, string TimezoneName, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
    private sealed record DatabaseData(Guid GymDatabaseId, Guid GymId, string DatabaseName, string Environment, string? SchemaVersion, string? SeedVersion, string Status, DateTime? LastHealthAtUtc);
    private sealed record GymDetailData(Guid GymId, Guid OrganizationId, string Name, string Slug, string Status, string TimezoneName, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, IReadOnlyList<DatabaseData> Databases);
    private sealed record MonitoringData(DateTime ObservedAtUtc, PlatformHealthData PlatformHealth, IReadOnlyList<DatabaseData> RegisteredDatabases);
}
