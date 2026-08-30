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

public sealed class MembersApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string ControlPlaneConnectionString = "Server=localhost;Database=LogicFit_ControlPlane_Local;Integrated Security=True;TrustServerCertificate=True;Encrypt=False";
    private const string GymDatabase = "LogicFit_Gym_001_Local";
    private const string GymConnectionString = "Server=localhost;Database=LogicFit_Gym_001_Local;Integrated Security=True;TrustServerCertificate=True;Encrypt=False";
    private const string Password = "Local Test Password 123!";
    private readonly WebApplicationFactory<Program> factory;

    public MembersApiTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task MembersCrudIsIdempotentAndArchivesWithoutDeleting()
    {
        var admin = await CreateIdentityAsync("Gym Security Admin");
        var memberId = Guid.Empty;
        var idempotencyKey = $"phase8-create-{Guid.NewGuid():N}";
        try
        {
            using var client = factory.CreateClient();
            var login = await LoginAsync(client, admin.Email);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

            var request = new
            {
                fullName = "Phase 8 Member Test",
                phone = "+20 (100) 000-0000",
                email = "Phase8.Member@Example.Test",
                registrationDate = "2026-08-30",
                notes = "API contract test"
            };

            using var create = await SendCreateAsync(client, admin.GymId, request, idempotencyKey);
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            var created = (await create.Content.ReadFromJsonAsync<ApiResponse<MemberData>>())!.Data;
            memberId = created.MemberId;
            Assert.Equal("ACTIVE", created.Status);
            Assert.NotEmpty(created.MemberCode);
            Assert.Equal("+201000000000", created.Phone);

            using var repeated = await SendCreateAsync(client, admin.GymId, request, idempotencyKey);
            Assert.Equal(HttpStatusCode.Created, repeated.StatusCode);
            var repeatedData = (await repeated.Content.ReadFromJsonAsync<ApiResponse<MemberData>>())!.Data;
            Assert.Equal(memberId, repeatedData.MemberId);

            using var conflict = await SendCreateAsync(client, admin.GymId, new
            {
                fullName = "Different Member",
                phone = "+201000000001",
                email = (string?)null,
                registrationDate = "2026-08-30",
                notes = (string?)null
            }, idempotencyKey);
            Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
            Assert.Equal("DUPLICATE_RESOURCE", (await conflict.Content.ReadFromJsonAsync<ApiErrorResponse>())!.Error.Code);

            using var list = await client.GetAsync($"/api/v1/gyms/{admin.GymId:D}/members?page=1&pageSize=25&search=Phase%208%20Member");
            Assert.Equal(HttpStatusCode.OK, list.StatusCode);
            var listed = (await list.Content.ReadFromJsonAsync<ApiCollectionResponse<MemberData>>())!;
            Assert.Contains(listed.Data, item => item.MemberId == memberId);

            using var noMatch = await client.GetAsync($"/api/v1/gyms/{admin.GymId:D}/members?page=1&pageSize=25&search=No%20such%20member");
            Assert.Equal(HttpStatusCode.OK, noMatch.StatusCode);
            var noMatchData = (await noMatch.Content.ReadFromJsonAsync<ApiCollectionResponse<MemberData>>())!;
            Assert.Empty(noMatchData.Data);

            using var detail = await client.GetAsync($"/api/v1/gyms/{admin.GymId:D}/members/{memberId:D}");
            Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
            var detailed = (await detail.Content.ReadFromJsonAsync<ApiResponse<MemberData>>())!.Data;
            Assert.Equal("phase8.member@example.test", detailed.Email);

            using var updateRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/gyms/{admin.GymId:D}/members/{memberId:D}")
            {
                Content = JsonContent.Create(new
                {
                    fullName = "Phase 8 Member Updated",
                    phone = "+201000000002",
                    email = "phase8.updated@example.test",
                    registrationDate = "2026-08-30",
                    notes = "updated",
                    status = "INACTIVE"
                })
            };
            updateRequest.Headers.TryAddWithoutValidation("If-Match", $"\"{created.Version}\"");
            using var update = await client.SendAsync(updateRequest);
            Assert.Equal(HttpStatusCode.OK, update.StatusCode);
            var updated = (await update.Content.ReadFromJsonAsync<ApiResponse<MemberData>>())!.Data;
            Assert.Equal("INACTIVE", updated.Status);
            Assert.NotEqual(created.Version, updated.Version);

            using var staleUpdate = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/gyms/{admin.GymId:D}/members/{memberId:D}")
            {
                Content = JsonContent.Create(new
                {
                    fullName = "Stale Update",
                    phone = "+201000000003",
                    email = (string?)null,
                    registrationDate = "2026-08-30",
                    notes = (string?)null,
                    status = "ACTIVE"
                })
            };
            staleUpdate.Headers.TryAddWithoutValidation("If-Match", $"\"{created.Version}\"");
            using var stale = await client.SendAsync(staleUpdate);
            Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
            Assert.Equal("CONCURRENCY_CONFLICT", (await stale.Content.ReadFromJsonAsync<ApiErrorResponse>())!.Error.Code);

            using var staleArchiveRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/gyms/{admin.GymId:D}/members/{memberId:D}");
            staleArchiveRequest.Headers.TryAddWithoutValidation("If-Match", $"\"{created.Version}\"");
            using var staleArchive = await client.SendAsync(staleArchiveRequest);
            Assert.Equal(HttpStatusCode.Conflict, staleArchive.StatusCode);

            using var archiveRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/gyms/{admin.GymId:D}/members/{memberId:D}");
            archiveRequest.Headers.TryAddWithoutValidation("If-Match", $"\"{updated.Version}\"");
            using var archive = await client.SendAsync(archiveRequest);
            Assert.Equal(HttpStatusCode.OK, archive.StatusCode);
            var archived = (await archive.Content.ReadFromJsonAsync<ApiResponse<ArchiveData>>())!.Data;
            Assert.Equal("ARCHIVED", archived.Status);

            using var repeatedArchive = await client.DeleteAsync($"/api/v1/gyms/{admin.GymId:D}/members/{memberId:D}");
            Assert.Equal(HttpStatusCode.OK, repeatedArchive.StatusCode);

            using var archivedList = await client.GetAsync($"/api/v1/gyms/{admin.GymId:D}/members?status=ARCHIVED");
            Assert.Equal(HttpStatusCode.OK, archivedList.StatusCode);
            var archivedItems = (await archivedList.Content.ReadFromJsonAsync<ApiCollectionResponse<MemberData>>())!.Data;
            Assert.Contains(archivedItems, item => item.MemberId == memberId && item.Status == "ARCHIVED");

            using var timeline = await client.GetAsync($"/api/v1/gyms/{admin.GymId:D}/members/{memberId:D}/timeline");
            Assert.Equal(HttpStatusCode.OK, timeline.StatusCode);
            var events = (await timeline.Content.ReadFromJsonAsync<ApiCollectionResponse<TimelineData>>())!.Data;
            Assert.Contains(events, item => item.EventType == "MEMBER_CREATED");
            Assert.Contains(events, item => item.EventType == "MEMBER_UPDATED");
            Assert.Contains(events, item => item.EventType == "MEMBER_ARCHIVED");
            Assert.Contains(events, item => item.EventType == "MEMBER_STATUS_CHANGED");
        }
        finally
        {
            await DeleteMemberAsync(memberId);
            await DeleteIdentityAsync(admin.UserId);
        }
    }

    [Fact]
    public async Task MembersAuthorizationAndGymIsolationAreEnforced()
    {
        var admin = await CreateIdentityAsync("Gym Security Admin");
        var reader = await CreateIdentityAsync("Gym Authenticated User");
        var memberId = Guid.Empty;
        try
        {
            using var adminClient = factory.CreateClient();
            var adminLogin = await LoginAsync(adminClient, admin.Email);
            adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminLogin.AccessToken);
            using var create = await SendCreateAsync(adminClient, admin.GymId, new
            {
                fullName = "Phase 8 Isolation Member",
                phone = "+201000000004",
                email = (string?)null,
                registrationDate = "2026-08-30",
                notes = (string?)null
            }, $"phase8-isolation-{Guid.NewGuid():N}");
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            memberId = (await create.Content.ReadFromJsonAsync<ApiResponse<MemberData>>())!.Data.MemberId;

            using var readerClient = factory.CreateClient();
            var readerLogin = await LoginAsync(readerClient, reader.Email);
            readerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", readerLogin.AccessToken);
            using var read = await readerClient.GetAsync($"/api/v1/gyms/{reader.GymId:D}/members/{memberId:D}");
            Assert.Equal(HttpStatusCode.OK, read.StatusCode);

            using var deniedCreate = await SendCreateAsync(readerClient, reader.GymId, new
            {
                fullName = "Denied Member",
                phone = "+201000000005",
                email = (string?)null,
                registrationDate = "2026-08-30",
                notes = (string?)null
            }, $"phase8-denied-{Guid.NewGuid():N}");
            Assert.Equal(HttpStatusCode.Forbidden, deniedCreate.StatusCode);

            var otherGymId = await FindOtherGymOrNewIdAsync(reader.GymId);
            using var crossGym = await readerClient.GetAsync($"/api/v1/gyms/{otherGymId:D}/members/{memberId:D}");
            Assert.Equal(HttpStatusCode.Forbidden, crossGym.StatusCode);

            using var unauthenticated = factory.CreateClient();
            using var unauthenticatedResponse = await unauthenticated.GetAsync($"/api/v1/gyms/{reader.GymId:D}/members");
            Assert.Equal(HttpStatusCode.Unauthorized, unauthenticatedResponse.StatusCode);
        }
        finally
        {
            await DeleteMemberAsync(memberId);
            await DeleteIdentityAsync(admin.UserId);
            await DeleteIdentityAsync(reader.UserId);
        }
    }

    [Fact]
    public async Task InvalidMemberRequestUsesTheStandardValidationEnvelope()
    {
        var admin = await CreateIdentityAsync("Gym Security Admin");
        try
        {
            using var client = factory.CreateClient();
            var login = await LoginAsync(client, admin.Email);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
            using var response = await SendCreateAsync(client, admin.GymId, new
            {
                fullName = "",
                phone = "bad",
                email = "not-an-email",
                registrationDate = (string?)null,
                notes = (string?)null
            }, $"phase8-invalid-{Guid.NewGuid():N}");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var error = (await response.Content.ReadFromJsonAsync<ApiErrorResponse>())!.Error;
            Assert.Equal("VALIDATION_ERROR", error.Code);
            Assert.Contains(error.FieldErrors!, field => field.Field == "fullName");
            Assert.Contains(error.FieldErrors!, field => field.Field == "phone");
            Assert.Contains(error.FieldErrors!, field => field.Field == "email");
        }
        finally
        {
            await DeleteIdentityAsync(admin.UserId);
        }
    }

    private static async Task<HttpResponseMessage> SendCreateAsync<T>(HttpClient client, Guid gymId, T request, string idempotencyKey)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/gyms/{gymId:D}/members")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
        return await client.SendAsync(message);
    }

    private static async Task<LoginData> LoginAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<LoginData>>())!.Data;
    }

    private static async Task<TestIdentity> CreateIdentityAsync(string roleName)
    {
        await using var db = CreateControlPlaneDb();
        var gym = await db.Gyms.AsNoTracking().OrderBy(x => x.GymId).FirstAsync();
        var role = await db.Roles.SingleAsync(x => x.Name == roleName);
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var email = $"phase8-members-{userId:N}@example.test";
        var user = new UserEntity
        {
            UserId = userId,
            Email = email,
            DisplayName = $"Phase 8 {roleName}",
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
            GymId = gym.GymId,
            RoleId = role.RoleId,
            ScopeType = "gym",
            Status = "active",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return new TestIdentity(userId, email, gym.GymId);
    }

    private static async Task<Guid> FindOtherGymOrNewIdAsync(Guid ownGymId)
    {
        await using var db = CreateControlPlaneDb();
        return await db.Gyms.AsNoTracking()
            .Where(x => x.GymId != ownGymId)
            .Select(x => (Guid?)x.GymId)
            .FirstOrDefaultAsync() ?? Guid.NewGuid();
    }

    private static async Task DeleteMemberAsync(Guid memberId)
    {
        if (memberId == Guid.Empty) return;
        await using var db = new GymDbContext(new DbContextOptionsBuilder<GymDbContext>()
            .UseSqlServer(GymConnectionString)
            .Options);
        await db.MemberTimelineEvents.Where(x => x.MemberId == memberId).ExecuteDeleteAsync();
        await db.Members.Where(x => x.MemberId == memberId).ExecuteDeleteAsync();
        await using var controlPlane = CreateControlPlaneDb();
        await controlPlane.AuditEvents.Where(x => x.TargetId == memberId).ExecuteDeleteAsync();
    }

    private static async Task DeleteIdentityAsync(Guid userId)
    {
        await using var db = CreateControlPlaneDb();
        await db.MfaRecoveryCodes.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        await db.MfaFactors.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        await db.PasswordResetTokens.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        await db.Sessions.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        await db.Credentials.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        await db.UserGymRoles.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        await db.AuditEvents.Where(x => x.ActorUserId == userId || x.TargetId == userId).ExecuteDeleteAsync();
        await db.Users.Where(x => x.UserId == userId).ExecuteDeleteAsync();
    }

    private static ControlPlaneDbContext CreateControlPlaneDb()
        => new(new DbContextOptionsBuilder<ControlPlaneDbContext>()
            .UseSqlServer(ControlPlaneConnectionString)
            .Options);

    private sealed record TestIdentity(Guid UserId, string Email, Guid GymId);
    private sealed record LoginData(string AccessToken, Guid SessionId, bool RequiresMfa, string? Challenge, bool MfaVerified, DateTime ExpiresAtUtc, DateTime IdleExpiresAtUtc, DateTime AbsoluteExpiresAtUtc, UserData User);
    private sealed record UserData(Guid UserId, string Email, string DisplayName, string Status, DateTime? LastLoginAtUtc, string Version);
    private sealed record MemberData(Guid MemberId, Guid? GymId, string MemberCode, string FullName, string Phone, string? Email, string RegistrationDate, string? Notes, string Status, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, string Version);
    private sealed record ArchiveData(Guid MemberId, string Status, DateTime ArchivedAtUtc, string Version);
    private sealed record TimelineData(Guid EventId, Guid MemberId, Guid GymId, string EventType, DateTime OccurredAt, Guid? ActorId, IReadOnlyDictionary<string, string?> Metadata);
}
