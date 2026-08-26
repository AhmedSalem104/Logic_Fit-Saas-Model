using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LogicFit.Infrastructure.Persistence;
using LogicFit.Infrastructure.Persistence.Entities;
using LogicFit.Infrastructure.Security;
using LogicFit.Shared;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LogicFit.ApiTests;

public sealed class AuthContinuationApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string ConnectionString = "Server=localhost;Database=LogicFit_ControlPlane_Local;Integrated Security=True;TrustServerCertificate=True;Encrypt=False";
    private const string Password = "Local Test Password 123!";
    private const string NewPassword = "Local New Password 456!";
    private readonly WebApplicationFactory<Program> factory;

    public AuthContinuationApiTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task SessionsCanBeListedRefreshedAndOwnedSessionsRevoked()
    {
        var identity = await CreateIdentityAsync();
        try
        {
            using var firstClient = factory.CreateClient();
            var first = await LoginAsync(firstClient, identity.Email, Password);
            using var secondClient = factory.CreateClient();
            var second = await LoginAsync(secondClient, identity.Email, Password);

            firstClient.DefaultRequestHeaders.Authorization = Bearer(first.AccessToken);
            using var listResponse = await firstClient.GetAsync("/api/v1/auth/sessions");
            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
            var list = await listResponse.Content.ReadFromJsonAsync<ApiCollectionResponse<SessionItem>>();
            Assert.NotNull(list);
            Assert.True(list.Data.Count >= 2);
            Assert.Contains(list.Data, session => session.IsCurrent && session.SessionId == first.SessionId);
            var listJson = await listResponse.Content.ReadAsStringAsync();
            Assert.DoesNotContain(first.AccessToken, listJson, StringComparison.Ordinal);
            Assert.DoesNotContain(second.AccessToken, listJson, StringComparison.Ordinal);

            using var revokeResponse = await firstClient.PostAsJsonAsync($"/api/v1/auth/sessions/{second.SessionId:D}/revoke", new { reason = "test-owned-session-revocation" });
            Assert.Equal(HttpStatusCode.OK, revokeResponse.StatusCode);

            using var repeatedRevokeResponse = await firstClient.PostAsJsonAsync($"/api/v1/auth/sessions/{second.SessionId:D}/revoke", new { reason = "test-owned-session-revocation-repeat" });
            Assert.Equal(HttpStatusCode.OK, repeatedRevokeResponse.StatusCode);
            var repeatedRevoke = (await repeatedRevokeResponse.Content.ReadFromJsonAsync<ApiResponse<RevocationData>>())!.Data;
            Assert.True(repeatedRevoke.Revoked);

            secondClient.DefaultRequestHeaders.Authorization = Bearer(second.AccessToken);
            using var revokedResponse = await secondClient.GetAsync("/api/v1/auth/me");
            Assert.Equal(HttpStatusCode.Unauthorized, revokedResponse.StatusCode);

            using var refreshResponse = await firstClient.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = first.AccessToken });
            Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
            var refreshed = (await refreshResponse.Content.ReadFromJsonAsync<ApiResponse<LoginData>>())!.Data;
            Assert.NotEqual(first.AccessToken, refreshed.AccessToken);

            using var oldResponse = await firstClient.GetAsync("/api/v1/auth/me");
            Assert.Equal(HttpStatusCode.Unauthorized, oldResponse.StatusCode);
            firstClient.DefaultRequestHeaders.Authorization = Bearer(refreshed.AccessToken);
            using var refreshedMe = await firstClient.GetAsync("/api/v1/auth/me");
            Assert.Equal(HttpStatusCode.OK, refreshedMe.StatusCode);
        }
        finally
        {
            await DeleteIdentityAsync(identity.UserId);
        }
    }

    [Fact]
    public async Task PasswordChangeInvalidatesEveryExistingSession()
    {
        var identity = await CreateIdentityAsync();
        try
        {
            using var client = factory.CreateClient();
            var login = await LoginAsync(client, identity.Email, Password);
            client.DefaultRequestHeaders.Authorization = Bearer(login.AccessToken);

            using var response = await client.PostAsJsonAsync("/api/v1/auth/password/change", new { currentPassword = Password, newPassword = NewPassword });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<ApiResponse<PasswordChangeData>>();
            Assert.NotNull(body);
            Assert.True(body.Data.Changed);
            Assert.True(body.Data.ReauthenticationRequired);

            using var invalidated = await client.GetAsync("/api/v1/auth/me");
            Assert.Equal(HttpStatusCode.Unauthorized, invalidated.StatusCode);

            var newLogin = await LoginAsync(client, identity.Email, NewPassword);
            Assert.False(newLogin.RequiresMfa);
        }
        finally
        {
            await DeleteIdentityAsync(identity.UserId);
        }
    }

    [Fact]
    public async Task PasswordResetUsesTheCanonicalHyphenatedRoutesAndSingleUseToken()
    {
        var identity = await CreateIdentityAsync();
        var resetToken = CreateOpaqueToken();
        try
        {
            await using (var db = CreateDb())
            {
                db.PasswordResetTokens.Add(new PasswordResetTokenEntity
                {
                    PasswordResetTokenId = Guid.NewGuid(),
                    UserId = identity.UserId,
                    TokenHash = HashOpaque(resetToken),
                    ExpiresAtUtc = DateTime.UtcNow.AddMinutes(15),
                    CreatedAtUtc = DateTime.UtcNow,
                    RequestId = "phase5b-test"
                });
                await db.SaveChangesAsync();
            }

            using var client = factory.CreateClient();
            using var complete = await client.PostAsJsonAsync("/api/v1/auth/password-reset/complete", new { token = resetToken, newPassword = NewPassword });
            Assert.Equal(HttpStatusCode.OK, complete.StatusCode);

            using var repeated = await client.PostAsJsonAsync("/api/v1/auth/password-reset/complete", new { token = resetToken, newPassword = Password });
            Assert.Equal(HttpStatusCode.UnprocessableEntity, repeated.StatusCode);

            var login = await LoginAsync(client, identity.Email, NewPassword);
            Assert.False(login.RequiresMfa);

        }
        finally
        {
            await DeleteIdentityAsync(identity.UserId);
        }
    }

    [Fact]
    public async Task PasswordResetRequestIsEnumerationSafeAndExpiredTokensAreRejected()
    {
        var identity = await CreateIdentityAsync();
        var expiredToken = CreateOpaqueToken();
        try
        {
            using var client = factory.CreateClient();
            using var known = await client.PostAsJsonAsync("/api/v1/auth/password-reset/request", new { email = identity.Email });
            Assert.Equal(HttpStatusCode.Accepted, known.StatusCode);
            var knownJson = await known.Content.ReadAsStringAsync();
            Assert.DoesNotContain("token", knownJson, StringComparison.OrdinalIgnoreCase);
            var knownBody = JsonSerializer.Deserialize<ApiResponse<PasswordResetData>>(knownJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(knownBody);
            Assert.True(knownBody.Data.Accepted);

            using var unknown = await client.PostAsJsonAsync("/api/v1/auth/password-reset/request", new { email = $"missing-{Guid.NewGuid():N}@example.test" });
            Assert.Equal(HttpStatusCode.Accepted, unknown.StatusCode);
            var unknownJson = await unknown.Content.ReadAsStringAsync();
            Assert.DoesNotContain("token", unknownJson, StringComparison.OrdinalIgnoreCase);
            var unknownBody = JsonSerializer.Deserialize<ApiResponse<PasswordResetData>>(unknownJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(unknownBody);
            Assert.True(unknownBody.Data.Accepted);

            await using (var db = CreateDb())
            {
                db.PasswordResetTokens.Add(new PasswordResetTokenEntity
                {
                    PasswordResetTokenId = Guid.NewGuid(),
                    UserId = identity.UserId,
                    TokenHash = HashOpaque(expiredToken),
                    ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1),
                    CreatedAtUtc = DateTime.UtcNow,
                    RequestId = "phase5b-expired-test"
                });
                await db.SaveChangesAsync();
            }

            using var expired = await client.PostAsJsonAsync("/api/v1/auth/password-reset/complete", new { token = expiredToken, newPassword = NewPassword });
            Assert.Equal(HttpStatusCode.UnprocessableEntity, expired.StatusCode);
        }
        finally
        {
            await DeleteIdentityAsync(identity.UserId);
        }
    }

    [Fact]
    public async Task ExpiredSessionIsRejected()
    {
        var identity = await CreateIdentityAsync();
        try
        {
            using var client = factory.CreateClient();
            var login = await LoginAsync(client, identity.Email, Password);
            await using (var db = CreateDb())
            {
                await db.Sessions.Where(x => x.SessionId == login.SessionId).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ExpiresAtUtc, DateTime.UtcNow.AddMinutes(-1)));
            }

            client.DefaultRequestHeaders.Authorization = Bearer(login.AccessToken);
            using var expired = await client.GetAsync("/api/v1/auth/me");
            Assert.Equal(HttpStatusCode.Unauthorized, expired.StatusCode);
        }
        finally
        {
            await DeleteIdentityAsync(identity.UserId);
        }
    }

    [Fact]
    public async Task TotpEnrollmentRecoveryCodeAndDisablementUseOneCanonicalVerificationRoute()
    {
        var identity = await CreateIdentityAsync();
        try
        {
            await using var mfaFactory = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LogicFit:Runtime:MfaProtectionKeyBase64"] = Convert.ToBase64String(Enumerable.Range(1, 32).Select(value => (byte)value).ToArray())
            })));
            using var client = mfaFactory.CreateClient();
            var initial = await LoginAsync(client, identity.Email, Password);
            client.DefaultRequestHeaders.Authorization = Bearer(initial.AccessToken);

            using var enrollmentResponse = await client.PostAsync("/api/v1/auth/mfa/enroll", content: null);
            var enrollmentPayload = await enrollmentResponse.Content.ReadAsStringAsync();
            Assert.True(enrollmentResponse.StatusCode == HttpStatusCode.OK, $"{enrollmentResponse.StatusCode}: {enrollmentPayload}");
            var enrollment = JsonSerializer.Deserialize<ApiResponse<MfaEnrollmentData>>(enrollmentPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!.Data;
            Assert.Equal("pending", enrollment.Status);
            Assert.False(string.IsNullOrWhiteSpace(enrollment.Secret));

            await using (var db = CreateDb())
            {
                var factor = await db.MfaFactors.SingleAsync(x => x.MfaFactorId == enrollment.FactorId);
                Assert.NotEqual(enrollment.Secret, factor.SecretRef);
            }

            using var enableResponse = await client.PostAsJsonAsync("/api/v1/auth/mfa/verify", new { challenge = enrollment.FactorId.ToString("D"), method = "totp", code = CreateTotpCode(enrollment.Secret) });
            var enablePayload = await enableResponse.Content.ReadAsStringAsync();
            Assert.True(enableResponse.StatusCode == HttpStatusCode.OK, $"{enableResponse.StatusCode}: {enablePayload}");

            using var recoveryResponse = await client.PostAsJsonAsync("/api/v1/auth/mfa/recovery-codes/regenerate", new { currentPassword = Password, code = (string?)null });
            Assert.Equal(HttpStatusCode.OK, recoveryResponse.StatusCode);
            var recovery = (await recoveryResponse.Content.ReadFromJsonAsync<ApiResponse<RecoveryCodesData>>())!.Data;
            Assert.Equal(10, recovery.Codes.Count);

            using var logout = await client.PostAsJsonAsync("/api/v1/auth/logout", new { sessionId = initial.SessionId });
            Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
            client.DefaultRequestHeaders.Authorization = null;

            var pending = await LoginAsync(client, identity.Email, Password);
            Assert.True(pending.RequiresMfa);
            Assert.NotNull(pending.Challenge);
            await using (var pendingDb = CreateDb())
            {
                var pendingRecord = await pendingDb.Sessions.SingleAsync(x => x.SessionId == pending.SessionId);
                Assert.Equal("mfa_pending", pendingRecord.SessionKind);
                Assert.False(pendingRecord.MfaVerified);
                Assert.InRange(pendingRecord.ExpiresAtUtc - pendingRecord.CreatedAtUtc, TimeSpan.FromSeconds(295), TimeSpan.FromSeconds(305));
                Assert.True(pendingRecord.ExpiresAtUtc < pendingRecord.AbsoluteExpiresAtUtc);
            }
            client.DefaultRequestHeaders.Authorization = Bearer(pending.AccessToken);
            using var pendingMe = await client.GetAsync("/api/v1/auth/me");
            Assert.Equal(HttpStatusCode.Forbidden, pendingMe.StatusCode);
            using var challengeWithoutSession = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/mfa/verify", new { challenge = pending.Challenge, method = "totp", code = CreateTotpCode(enrollment.Secret) });
            Assert.Equal(HttpStatusCode.Unauthorized, challengeWithoutSession.StatusCode);
            using var verified = await client.PostAsJsonAsync("/api/v1/auth/mfa/verify", new { challenge = pending.Challenge, method = "totp", code = CreateTotpCode(enrollment.Secret) });
            Assert.Equal(HttpStatusCode.OK, verified.StatusCode);
            var verifiedData = (await verified.Content.ReadFromJsonAsync<ApiResponse<MfaVerificationData>>())!.Data;
            Assert.True(verifiedData.Verified);
            Assert.NotNull(verifiedData.Session);
            await using (var verifiedDb = CreateDb())
            {
                var verifiedRecord = await verifiedDb.Sessions.SingleAsync(x => x.SessionId == pending.SessionId);
                Assert.Equal("staff", verifiedRecord.SessionKind);
                Assert.True(verifiedRecord.MfaVerified);
                Assert.Equal(verifiedRecord.AbsoluteExpiresAtUtc, verifiedRecord.ExpiresAtUtc);
            }

            client.DefaultRequestHeaders.Authorization = Bearer(verifiedData.Session!.AccessToken);
            using var verifiedLogout = await client.PostAsJsonAsync("/api/v1/auth/logout", new { sessionId = verifiedData.Session.SessionId });
            Assert.Equal(HttpStatusCode.OK, verifiedLogout.StatusCode);
            client.DefaultRequestHeaders.Authorization = null;

            var recoveryPending = await LoginAsync(client, identity.Email, Password);
            Assert.True(recoveryPending.RequiresMfa);
            client.DefaultRequestHeaders.Authorization = Bearer(recoveryPending.AccessToken);
            using var recoveryVerification = await client.PostAsJsonAsync("/api/v1/auth/mfa/verify", new { challenge = recoveryPending.Challenge, method = "recovery_code", code = recovery.Codes[0] });
            Assert.Equal(HttpStatusCode.OK, recoveryVerification.StatusCode);
            var recoveryVerificationData = (await recoveryVerification.Content.ReadFromJsonAsync<ApiResponse<MfaVerificationData>>())!.Data;
            Assert.True(recoveryVerificationData.Verified);
            Assert.NotNull(recoveryVerificationData.Session);

            client.DefaultRequestHeaders.Authorization = Bearer(recoveryVerificationData.Session!.AccessToken);
            using var recoveryLogout = await client.PostAsJsonAsync("/api/v1/auth/logout", new { sessionId = recoveryVerificationData.Session.SessionId });
            Assert.Equal(HttpStatusCode.OK, recoveryLogout.StatusCode);
            client.DefaultRequestHeaders.Authorization = null;

            var reusePending = await LoginAsync(client, identity.Email, Password);
            Assert.True(reusePending.RequiresMfa);
            client.DefaultRequestHeaders.Authorization = Bearer(reusePending.AccessToken);
            using var reusedCode = await client.PostAsJsonAsync("/api/v1/auth/mfa/verify", new { challenge = reusePending.Challenge, method = "recovery_code", code = recovery.Codes[0] });
            Assert.Equal(HttpStatusCode.Unauthorized, reusedCode.StatusCode);

            client.DefaultRequestHeaders.Authorization = null;
            var disablePending = await LoginAsync(client, identity.Email, Password);
            client.DefaultRequestHeaders.Authorization = Bearer(disablePending.AccessToken);
            using var disableVerification = await client.PostAsJsonAsync("/api/v1/auth/mfa/verify", new { challenge = disablePending.Challenge, method = "totp", code = CreateTotpCode(enrollment.Secret) });
            Assert.Equal(HttpStatusCode.OK, disableVerification.StatusCode);
            var disableVerificationData = (await disableVerification.Content.ReadFromJsonAsync<ApiResponse<MfaVerificationData>>())!.Data;
            Assert.NotNull(disableVerificationData.Session);

            client.DefaultRequestHeaders.Authorization = Bearer(disableVerificationData.Session!.AccessToken);
            using var disable = await client.PostAsJsonAsync("/api/v1/auth/mfa/disable", new { currentPassword = Password, code = (string?)null });
            Assert.Equal(HttpStatusCode.OK, disable.StatusCode);

            await using var auditDb = CreateDb();
            var auditText = string.Join('|', await auditDb.AuditEvents.AsNoTracking()
                .Where(x => x.ActorUserId == identity.UserId)
                .Select(x => $"{x.Action}|{x.Result}|{x.Reason}|{x.MetadataJson}")
                .ToListAsync());
            Assert.DoesNotContain(enrollment.Secret, auditText, StringComparison.Ordinal);
            Assert.DoesNotContain(Password, auditText, StringComparison.Ordinal);
            Assert.All(recovery.Codes, code => Assert.DoesNotContain(code, auditText, StringComparison.Ordinal));
        }
        finally
        {
            await DeleteIdentityAsync(identity.UserId);
        }
    }

    private static async Task<LoginData> LoginAsync(HttpClient client, string email, string password)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<LoginData>>();
        Assert.NotNull(body);
        return body.Data;
    }

    private static ControlPlaneDbContext CreateDb()
        => new(new DbContextOptionsBuilder<ControlPlaneDbContext>().UseSqlServer(ConnectionString).Options);

    private static async Task<TestIdentity> CreateIdentityAsync()
    {
        await using var db = CreateDb();
        var gym = await db.Gyms.AsNoTracking().OrderBy(x => x.GymId).FirstAsync();
        var role = await db.Roles.SingleAsync(x => x.ScopeType == "gym" && x.Name == "Gym Authenticated User");
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var user = new UserEntity
        {
            UserId = userId,
            Email = $"phase5b-continuation-{userId:N}@example.test",
            DisplayName = "Phase 5B Continuation User",
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
        return new TestIdentity(userId, user.Email);
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

    private static AuthenticationHeaderValue Bearer(string token) => new("Bearer", token);

    private static string CreateOpaqueToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    private static string HashOpaque(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static string CreateTotpCode(string secret)
    {
        var key = DecodeBase32(secret);
        var counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
        Span<byte> counterBytes = stackalloc byte[8];
        for (var index = 7; index >= 0; index--)
        {
            counterBytes[index] = (byte)(counter & 0xff);
            counter >>= 8;
        }

        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(counterBytes.ToArray());
        var offset = hash[^1] & 0x0f;
        var binary = ((hash[offset] & 0x7f) << 24) | ((hash[offset + 1] & 0xff) << 16) | ((hash[offset + 2] & 0xff) << 8) | (hash[offset + 3] & 0xff);
        return (binary % 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
    }

    private static byte[] DecodeBase32(string value)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var buffer = 0;
        var bits = 0;
        var output = new List<byte>();
        foreach (var character in value.TrimEnd('=').ToUpperInvariant())
        {
            var index = alphabet.IndexOf(character);
            if (index < 0) throw new FormatException("Invalid test Base32 value.");
            buffer = (buffer << 5) | index;
            bits += 5;
            if (bits >= 8)
            {
                output.Add((byte)((buffer >> (bits - 8)) & 0xff));
                bits -= 8;
            }
        }
        return output.ToArray();
    }

    private sealed record TestIdentity(Guid UserId, string Email);
    private sealed record UserData(Guid UserId, string Email, string DisplayName, string Status, DateTime? LastLoginAtUtc, string Version);
    private sealed record LoginData(string AccessToken, Guid SessionId, bool RequiresMfa, string? Challenge, bool MfaVerified, DateTime ExpiresAtUtc, DateTime IdleExpiresAtUtc, DateTime AbsoluteExpiresAtUtc, UserData User);
    private sealed record SessionItem(Guid SessionId, Guid? GymId, string SessionKind, bool MfaVerified, DateTime CreatedAtUtc, DateTime LastSeenAtUtc, DateTime IdleExpiresAtUtc, DateTime AbsoluteExpiresAtUtc, DateTime ExpiresAtUtc, string? UserAgent, bool IsCurrent);
    private sealed record RevocationData(Guid SessionId, bool Revoked);
    private sealed record PasswordChangeData(bool Changed, bool ReauthenticationRequired);
    private sealed record PasswordResetData(bool Accepted);
    private sealed record MfaEnrollmentData(Guid FactorId, string Status, string Secret, string ProvisioningUri);
    private sealed record RecoveryCodesData(IReadOnlyList<string> Codes);
    private sealed record MfaVerificationData(bool Verified, LoginData? Session);
}
