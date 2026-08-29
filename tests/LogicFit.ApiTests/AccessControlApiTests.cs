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

public sealed class AccessControlApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string ConnectionString = "Server=localhost;Database=LogicFit_ControlPlane_Local;Integrated Security=True;TrustServerCertificate=True;Encrypt=False";
    private const string Password = "Local Test Password 123!";
    private readonly WebApplicationFactory<Program> factory;

    public AccessControlApiTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task SecurityAdminCanManageUsersAndRolesWithinTheAuthorizedGym()
    {
        var admin = await CreateIdentityAsync("Gym Security Admin");
        var target = await CreateIdentityAsync("Gym Authenticated User");
        var gymRole = await FindRoleAsync("Gym Security Admin");
        var basicRole = await FindRoleAsync("Gym Authenticated User");

        try
        {
            using var client = factory.CreateClient();
            var login = await LoginAsync(client, admin.Email);
            client.DefaultRequestHeaders.Authorization = Bearer(login.AccessToken);

            using var catalogResponse = await client.GetAsync("/api/v1/platform/access/catalog");
            Assert.Equal(HttpStatusCode.OK, catalogResponse.StatusCode);
            var catalog = (await catalogResponse.Content.ReadFromJsonAsync<ApiResponse<AccessCatalogData>>())!.Data;
            Assert.Equal(16, catalog.Permissions.Count);
            Assert.Equal(3, catalog.Roles.Count);
            Assert.Equal(15, catalog.RolePermissionAssignmentCount);
            Assert.Contains(catalog.Permissions, permission => permission.Key == "platform.security.manage");

            using var listResponse = await client.GetAsync($"/api/v1/platform/access/users?gymId={admin.GymId:D}&pageSize=100");
            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
            var listed = (await listResponse.Content.ReadFromJsonAsync<ApiCollectionResponse<AccessUserData>>())!.Data;
            Assert.Contains(listed, item => item.UserId == target.UserId);

            var createdEmail = $"phase5b-admin-created-{Guid.NewGuid():N}@example.test";
            using var createResponse = await client.PostAsJsonAsync("/api/v1/platform/access/users", new
            {
                email = createdEmail,
                displayName = "Created by Security Admin",
                initialPassword = Password,
                roleId = basicRole.RoleId,
                gymId = admin.GymId
            });
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<AccessUserData>>())!.Data;
            Assert.Equal(createdEmail, created.Email);
            Assert.Single(created.Assignments);
            Assert.Equal("active", created.Assignments[0].Status);

            using var assignmentResponse = await client.PutAsJsonAsync($"/api/v1/platform/access/users/{target.UserId:D}/role-assignments/{gymRole.RoleId:D}?gymId={admin.GymId:D}", new { reason = "security-admin-test-assignment" });
            Assert.Equal(HttpStatusCode.OK, assignmentResponse.StatusCode);
            var assignment = (await assignmentResponse.Content.ReadFromJsonAsync<ApiResponse<AccessAssignmentData>>())!.Data;
            Assert.Equal(target.UserId, assignment.UserId);
            Assert.Equal(gymRole.RoleId, assignment.RoleId);
            Assert.Equal("active", assignment.Status);

            using var repeatedAssignmentResponse = await client.PutAsJsonAsync($"/api/v1/platform/access/users/{target.UserId:D}/role-assignments/{gymRole.RoleId:D}?gymId={admin.GymId:D}", new { reason = "security-admin-test-idempotent-assignment" });
            Assert.Equal(HttpStatusCode.OK, repeatedAssignmentResponse.StatusCode);
            var repeatedAssignment = (await repeatedAssignmentResponse.Content.ReadFromJsonAsync<ApiResponse<AccessAssignmentData>>())!.Data;
            Assert.Equal(assignment.AssignmentId, repeatedAssignment.AssignmentId);

            using var revokeRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/platform/access/users/{target.UserId:D}/role-assignments/{assignment.AssignmentId:D}/revoke")
            {
                Content = JsonContent.Create(new { reason = "security-admin-test-revocation" })
            };
            revokeRequest.Headers.TryAddWithoutValidation("If-Match", $"\"{assignment.Version}\"");
            using var revokeResponse = await client.SendAsync(revokeRequest);
            Assert.Equal(HttpStatusCode.OK, revokeResponse.StatusCode);
            var revoked = (await revokeResponse.Content.ReadFromJsonAsync<ApiResponse<RevocationData>>())!.Data;
            Assert.True(revoked.Revoked);

            var targetVersion = await FindUserVersionAsync(target.UserId);
            using var disableRequest = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/platform/access/users/{target.UserId:D}/status")
            {
                Content = JsonContent.Create(new { status = "disabled", reason = "security-admin-test-disable" })
            };
            disableRequest.Headers.TryAddWithoutValidation("If-Match", $"\"{targetVersion}\"");
            using var disableResponse = await client.SendAsync(disableRequest);
            Assert.Equal(HttpStatusCode.OK, disableResponse.StatusCode);
            var disabled = (await disableResponse.Content.ReadFromJsonAsync<ApiResponse<StatusData>>())!.Data;
            Assert.Equal("disabled", disabled.Status);
            Assert.True(disabled.SessionsRevoked);

            using var repeatedDisable = await client.PatchAsJsonAsync($"/api/v1/platform/access/users/{target.UserId:D}/status", new
            {
                status = "disabled",
                reason = "security-admin-test-idempotent-status"
            });
            Assert.Equal(HttpStatusCode.OK, repeatedDisable.StatusCode);
            var repeatedDisabled = (await repeatedDisable.Content.ReadFromJsonAsync<ApiResponse<StatusData>>())!.Data;
            Assert.Equal("disabled", repeatedDisabled.Status);
            Assert.False(repeatedDisabled.SessionsRevoked);

            using var targetLogin = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = target.Email, password = Password });
            Assert.Equal(HttpStatusCode.Unauthorized, targetLogin.StatusCode);

            using var selfRoleRequest = await client.PutAsJsonAsync($"/api/v1/platform/access/users/{admin.UserId:D}/role-assignments/{gymRole.RoleId:D}?gymId={admin.GymId:D}", new { reason = "self-role-test" });
            Assert.Equal(HttpStatusCode.UnprocessableEntity, selfRoleRequest.StatusCode);
        }
        finally
        {
            await DeleteIdentityAsync(admin.UserId);
            await DeleteIdentityAsync(target.UserId);
            await DeleteIdentityByEmailAsync($"phase5b-admin-created-", createdPrefix: true);
        }
    }

    [Fact]
    public async Task NormalUserIsDeniedAccessAdministrationAndOtherGymScope()
    {
        var identity = await CreateIdentityAsync("Gym Authenticated User");
        try
        {
            var otherGymId = await FindOtherGymOrNewIdAsync(identity.GymId);
            using var client = factory.CreateClient();
            var login = await LoginAsync(client, identity.Email);
            client.DefaultRequestHeaders.Authorization = Bearer(login.AccessToken);

            using var catalogResponse = await client.GetAsync("/api/v1/platform/access/catalog");
            Assert.Equal(HttpStatusCode.Forbidden, catalogResponse.StatusCode);

            using var sessionResponse = await client.GetAsync($"/api/v1/auth/sessions?gymId={otherGymId:D}");
            Assert.Equal(HttpStatusCode.Forbidden, sessionResponse.StatusCode);

            using var usersResponse = await client.GetAsync($"/api/v1/platform/access/users?gymId={otherGymId:D}");
            Assert.Equal(HttpStatusCode.Forbidden, usersResponse.StatusCode);

            await using var db = CreateDb();
            var deniedAudit = await db.AuditEvents.AsNoTracking()
                .Where(x => x.ActorUserId == identity.UserId && x.Action == "authz.permission_denied")
                .ToListAsync();
            Assert.NotEmpty(deniedAudit);
            Assert.All(deniedAudit, entry => Assert.DoesNotContain("secret", $"{entry.Reason}{entry.MetadataJson}", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            await DeleteIdentityAsync(identity.UserId);
        }
    }

    [Fact]
    public async Task GymSecurityAdminCannotCreateAssignOrRevokePlatformScopedAccess()
    {
        var admin = await CreateIdentityAsync("Gym Security Admin");
        var target = await CreateIdentityAsync("Gym Authenticated User");
        var platformTarget = await CreateIdentityAsync("Platform Security Admin");
        var platformRole = await FindRoleAsync("Platform Security Admin");
        var platformAssignment = await FindAssignmentAsync(platformTarget.UserId, platformRole.RoleId);

        try
        {
            using var client = factory.CreateClient();
            var login = await LoginAsync(client, admin.Email);
            client.DefaultRequestHeaders.Authorization = Bearer(login.AccessToken);

            var platformEmail = $"phase5b-platform-attempt-{Guid.NewGuid():N}@example.test";
            using var createPlatform = await client.PostAsJsonAsync("/api/v1/platform/access/users", new
            {
                email = platformEmail,
                displayName = "Platform scope attempt",
                initialPassword = Password,
                roleId = platformRole.RoleId,
                gymId = (Guid?)null
            });
            Assert.Equal(HttpStatusCode.Forbidden, createPlatform.StatusCode);

            using var assignPlatform = await client.PutAsJsonAsync($"/api/v1/platform/access/users/{target.UserId:D}/role-assignments/{platformRole.RoleId:D}", new { reason = "gym-admin-platform-scope-test" });
            Assert.Equal(HttpStatusCode.Forbidden, assignPlatform.StatusCode);

            using var revokePlatform = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/platform/access/users/{platformTarget.UserId:D}/role-assignments/{platformAssignment.AssignmentId:D}/revoke")
            {
                Content = JsonContent.Create(new { reason = "gym-admin-platform-revoke-test" })
            };
            revokePlatform.Headers.TryAddWithoutValidation("If-Match", $"\"{platformAssignment.Version}\"");
            using var revokeResponse = await client.SendAsync(revokePlatform);
            Assert.Equal(HttpStatusCode.Forbidden, revokeResponse.StatusCode);
        }
        finally
        {
            await DeleteIdentityAsync(admin.UserId);
            await DeleteIdentityAsync(target.UserId);
            await DeleteIdentityAsync(platformTarget.UserId);
        }
    }

    private static async Task<LoginData> LoginAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<LoginData>>())!.Data;
    }

    private static async Task<TestIdentity> CreateIdentityAsync(string roleName)
    {
        await using var db = CreateDb();
        var gym = await db.Gyms.AsNoTracking().OrderBy(x => x.GymId).FirstAsync();
        var role = await db.Roles.SingleAsync(x => x.Name == roleName);
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var user = new UserEntity
        {
            UserId = userId,
            Email = $"phase5b-access-{userId:N}@example.test",
            DisplayName = $"Phase 5B {roleName}",
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
            GymId = role.ScopeType == "gym" ? gym.GymId : null,
            RoleId = role.RoleId,
            ScopeType = role.ScopeType,
            Status = "active",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return new TestIdentity(userId, user.Email, gym.GymId);
    }

    private static async Task<AccessRole> FindRoleAsync(string roleName)
    {
        await using var db = CreateDb();
        return await db.Roles.AsNoTracking().Where(x => x.Name == roleName).Select(x => new AccessRole(x.RoleId, x.ScopeType)).SingleAsync();
    }

    private static async Task<AccessAssignmentData> FindAssignmentAsync(Guid userId, Guid roleId)
    {
        await using var db = CreateDb();
        var assignment = await db.UserGymRoles.AsNoTracking()
            .Where(x => x.UserId == userId && x.RoleId == roleId && x.Status == "active")
            .Select(x => new AccessAssignmentData(x.AssignmentId, x.UserId, x.RoleId, x.Role!.Name, x.GymId, x.ScopeType, x.Status, Convert.ToBase64String(x.RowVersion)))
            .SingleAsync();
        return assignment;
    }

    private static async Task<string> FindUserVersionAsync(Guid userId)
    {
        await using var db = CreateDb();
        var user = await db.Users.AsNoTracking().SingleAsync(x => x.UserId == userId);
        return Convert.ToBase64String(user.RowVersion);
    }

    private static async Task<Guid> FindOtherGymOrNewIdAsync(Guid ownGymId)
    {
        await using var db = CreateDb();
        return await db.Gyms.AsNoTracking().Where(x => x.GymId != ownGymId).Select(x => (Guid?)x.GymId).FirstOrDefaultAsync() ?? Guid.NewGuid();
    }

    private static ControlPlaneDbContext CreateDb()
        => new(new DbContextOptionsBuilder<ControlPlaneDbContext>().UseSqlServer(ConnectionString).Options);

    private static async Task DeleteIdentityAsync(Guid userId)
    {
        await using var db = CreateDb();
        await db.MfaRecoveryCodes.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        await db.MfaFactors.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        await db.PasswordResetTokens.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        await db.Sessions.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        await db.Credentials.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        await db.UserGymRoles.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        await db.AuditEvents.Where(x => x.ActorUserId == userId || x.TargetId == userId).ExecuteDeleteAsync();
        await db.Users.Where(x => x.UserId == userId).ExecuteDeleteAsync();
    }

    private static async Task DeleteIdentityByEmailAsync(string prefix, bool createdPrefix)
    {
        await using var db = CreateDb();
        var ids = await db.Users.Where(x => createdPrefix ? x.Email.StartsWith(prefix) : x.Email == prefix).Select(x => x.UserId).ToArrayAsync();
        foreach (var id in ids)
        {
            await DeleteIdentityAsync(id);
        }
    }

    private static AuthenticationHeaderValue Bearer(string token) => new("Bearer", token);

    private sealed record TestIdentity(Guid UserId, string Email, Guid GymId);
    private sealed record AccessRole(Guid RoleId, string ScopeType);
    private sealed record LoginData(string AccessToken, Guid SessionId, bool RequiresMfa, string? Challenge, bool MfaVerified, DateTime ExpiresAtUtc, DateTime IdleExpiresAtUtc, DateTime AbsoluteExpiresAtUtc, UserData User);
    private sealed record UserData(Guid UserId, string Email, string DisplayName, string Status, DateTime? LastLoginAtUtc, string Version);
    private sealed record AccessPermissionData(Guid PermissionId, string Key, string Domain, string Action, string RiskLevel, string Description);
    private sealed record AccessRoleData(Guid RoleId, string ScopeType, string Name, string Status, IReadOnlyList<AccessPermissionData> Permissions);
    private sealed record AccessCatalogData(IReadOnlyList<AccessPermissionData> Permissions, IReadOnlyList<AccessRoleData> Roles, int RolePermissionAssignmentCount);
    private sealed record AccessAssignmentData(Guid AssignmentId, Guid UserId, Guid RoleId, string RoleName, Guid? GymId, string ScopeType, string Status, string Version);
    private sealed record AccessUserData(Guid UserId, string Email, string DisplayName, string Status, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, string Version, IReadOnlyList<AccessAssignmentData> Assignments);
    private sealed record RevocationData(Guid AssignmentId, bool Revoked);
    private sealed record StatusData(Guid UserId, string Status, bool SessionsRevoked, string Version);
}
