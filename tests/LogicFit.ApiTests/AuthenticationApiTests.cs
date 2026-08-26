using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using LogicFit.Infrastructure.Persistence;
using LogicFit.Infrastructure.Persistence.Entities;
using LogicFit.Infrastructure.Security;
using LogicFit.Shared;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.ApiTests;

public sealed class AuthenticationApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string ConnectionString = "Server=localhost;Database=LogicFit_ControlPlane_Local;Integrated Security=True;TrustServerCertificate=True;Encrypt=False";
    private const string Password = "Local Test Password 123!";
    private readonly WebApplicationFactory<Program> factory;

    public AuthenticationApiTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task ValidLoginCreatesOpaqueSessionAndResolvesCurrentUser()
    {
        var identity = await CreateIdentityAsync();
        try
        {
            using var client = factory.CreateClient();
            using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = identity.Email, password = Password });

            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            Assert.True(login.Headers.Contains("X-Request-Id"));
            var loginBody = await login.Content.ReadFromJsonAsync<ApiResponse<LoginData>>();
            Assert.NotNull(loginBody);
            Assert.Equal(identity.UserId, loginBody.Data.User.UserId);
            Assert.False(loginBody.Data.RequiresMfa);
            Assert.False(string.IsNullOrWhiteSpace(loginBody.Data.AccessToken));

            await using (var db = CreateDb())
            {
                var session = await db.Sessions.SingleAsync(x => x.SessionId == loginBody.Data.SessionId);
                Assert.Equal(identity.UserId, session.UserId);
                Assert.NotEqual(loginBody.Data.AccessToken, session.TokenHash);
                Assert.Equal(64, session.TokenHash.Length);
                Assert.Equal("staff", session.SessionKind);
            }

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody.Data.AccessToken);
            using var me = await client.GetAsync("/api/v1/auth/me");
            Assert.Equal(HttpStatusCode.OK, me.StatusCode);
            var meBody = await me.Content.ReadFromJsonAsync<ApiResponse<MeData>>();
            Assert.NotNull(meBody);
            Assert.Equal(identity.UserId, meBody.Data.User.UserId);
            Assert.Contains("auth.logout", meBody.Data.Permissions);
        }
        finally
        {
            await DeleteIdentityAsync(identity.UserId);
        }
    }

    [Fact]
    public async Task InvalidCredentialsReturnSafeUnauthorizedResult()
    {
        var identity = await CreateIdentityAsync();
        try
        {
            using var client = factory.CreateClient();
            using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = identity.Email, password = "Wrong password 123!" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
            Assert.NotNull(body);
            Assert.Equal("AUTHENTICATION_FAILED", body.Error.Code);
            Assert.DoesNotContain("Wrong", body.Error.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await DeleteIdentityAsync(identity.UserId);
        }
    }

    [Fact]
    public async Task InactiveUserAndRevokedScopeCannotStartASession()
    {
        var inactive = await CreateIdentityAsync(status: "disabled");
        var revokedScope = await CreateIdentityAsync(assignmentStatus: "revoked");
        try
        {
            using var client = factory.CreateClient();

            using var inactiveResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = inactive.Email, password = Password });
            Assert.Equal(HttpStatusCode.Unauthorized, inactiveResponse.StatusCode);

            using var revokedResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = revokedScope.Email, password = Password });
            Assert.Equal(HttpStatusCode.Unauthorized, revokedResponse.StatusCode);
        }
        finally
        {
            await DeleteIdentityAsync(inactive.UserId);
            await DeleteIdentityAsync(revokedScope.UserId);
        }
    }

    [Fact]
    public async Task CurrentUserEndpointRequiresAnAuthenticatedSession()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("AUTHENTICATION_REQUIRED", body.Error.Code);
    }

    [Fact]
    public async Task LoginRateLimitReturnsTheApprovedErrorEnvelope()
    {
        await using var isolatedFactory = new WebApplicationFactory<Program>();
        using var client = isolatedFactory.CreateClient();
        HttpResponseMessage? limited = null;
        for (var attempt = 0; attempt < 21; attempt++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = $"missing-{Guid.NewGuid():N}@example.test", password = Password });
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                limited = response;
                break;
            }

            response.Dispose();
        }

        Assert.NotNull(limited);
        using (limited)
        {
            var body = await limited.Content.ReadFromJsonAsync<ApiErrorResponse>();
            Assert.NotNull(body);
            Assert.Equal("RATE_LIMITED", body.Error.Code);
        }
    }

    [Fact]
    public async Task MalformedLoginRequestUsesTheStandardValidationEnvelope()
    {
        using var client = factory.CreateClient();
        using var content = new StringContent("{", Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("/api/v1/auth/login", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("VALIDATION_ERROR", body.Error.Code);
        Assert.NotEmpty(body.Error.FieldErrors!);
    }

    private static ControlPlaneDbContext CreateDb()
        => new(new DbContextOptionsBuilder<ControlPlaneDbContext>()
            .UseSqlServer(ConnectionString)
            .Options);

    private static async Task<TestIdentity> CreateIdentityAsync(string status = "active", string assignmentStatus = "active")
    {
        await using var db = CreateDb();
        var gym = await db.Gyms.AsNoTracking().OrderBy(x => x.GymId).FirstAsync();
        var role = await db.Roles.SingleAsync(x => x.ScopeType == "gym" && x.Name == "Gym Authenticated User");
        var userId = Guid.NewGuid();
        var email = $"phase5b-{userId:N}@example.test";
        var now = DateTime.UtcNow;
        var user = new UserEntity
        {
            UserId = userId,
            Email = email,
            DisplayName = "Phase 5B Test User",
            Status = status,
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
            GymId = gym.GymId,
            RoleId = role.RoleId,
            ScopeType = "gym",
            Status = assignmentStatus,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return new TestIdentity(userId, email);
    }

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

    private sealed record TestIdentity(Guid UserId, string Email);
    private sealed record LoginData(string AccessToken, Guid SessionId, bool RequiresMfa, string? Challenge, bool MfaVerified, DateTime ExpiresAtUtc, DateTime IdleExpiresAtUtc, DateTime AbsoluteExpiresAtUtc, UserData User);
    private sealed record UserData(Guid UserId, string Email, string DisplayName, string Status, DateTime? LastLoginAtUtc, string Version);
    private sealed record MeData(UserData User, IReadOnlyList<ScopeData> Scopes, IReadOnlyList<string> Permissions);
    private sealed record ScopeData(Guid? GymId, string ScopeType, IReadOnlyList<string> Permissions);
}
