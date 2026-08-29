using System.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using LogicFit.Application;
using LogicFit.Infrastructure.Persistence;
using LogicFit.Infrastructure.Persistence.Entities;
using LogicFit.Infrastructure.Security;
using LogicFit.Infrastructure.Services.Seeding;
using LogicFit.Shared;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace LogicFit.ApiTests;

public sealed class ProvisioningApiTests : IAsyncLifetime
{
    private const string Password = "Local Test Password 123!";
    private string controlPlaneDatabaseName = string.Empty;
    private string defaultGymDatabaseName = string.Empty;
    private WebApplicationFactory<Program>? factory;

    public async Task InitializeAsync()
    {
        var suffix = Guid.NewGuid().ToString("N");
        controlPlaneDatabaseName = $"LogicFit_ControlPlane_Phase7Test_{suffix}";
        defaultGymDatabaseName = $"LogicFit_Gym_Phase7Test_{suffix}";
        await CreateDatabaseAsync(controlPlaneDatabaseName);
        await CreateDatabaseAsync(defaultGymDatabaseName);

        await using (var controlPlane = CreateDb())
        {
            await controlPlane.Database.MigrateAsync();
            await using var defaultGym = CreateGymDb(defaultGymDatabaseName);
            await defaultGym.Database.MigrateAsync();
            var environment = new TestHostEnvironment(ProjectRoot());
            var manifestReader = new CanonicalSeedManifestReader(environment);
            var librarySeeder = new CanonicalLibrarySeeder(defaultGym, manifestReader);
            var seed = new SeedCoordinator(controlPlane, defaultGym, manifestReader, librarySeeder);
            var result = await seed.ApplyAsync();
            Assert.True(result.ValidationPassed, JsonSerializer.Serialize(result));
        }

        factory = CreateFactory();
    }

    public async Task DisposeAsync()
    {
        factory?.Dispose();
        if (!string.IsNullOrWhiteSpace(controlPlaneDatabaseName) && DatabaseExists(controlPlaneDatabaseName))
        {
            await using var controlPlane = CreateDb();
            var generatedDatabases = await controlPlane.GymDatabases
                .AsNoTracking()
                .Select(x => x.DatabaseName)
                .ToArrayAsync();
            foreach (var generatedDatabase in generatedDatabases)
            {
                await DropDatabaseAsync(generatedDatabase);
            }
        }

        await DropDatabaseAsync(controlPlaneDatabaseName);
        await DropDatabaseAsync(defaultGymDatabaseName);
    }

    [Fact]
    public async Task PlatformAdminProvisioningCompletesAndIsIdempotent()
    {
        var actor = await CreatePlatformIdentityAsync();
        var requestId = Guid.NewGuid().ToString("N");
        var organizationSlug = $"phase7-org-{requestId}";
        var gymSlug = $"phase7-gym-{requestId}";
        var ownerEmail = $"phase7-owner-{requestId}@example.test";
        var databaseName = string.Empty;
        Guid operationId = Guid.Empty;
        Guid ownerUserId = Guid.Empty;
        Guid organizationId = Guid.Empty;
        Guid gymId = Guid.Empty;
        Guid gymDatabaseId = Guid.Empty;

        try
        {
            using var client = factory!.CreateClient();

            using (var unauthenticated = await client.PostAsJsonAsync(
                       "/api/v1/platform/provisioning",
                       ProvisioningBody(organizationSlug, gymSlug, ownerEmail)))
            {
                Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);
            }

            var login = await LoginAsync(client, actor.Email);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/platform/provisioning")
            {
                Content = JsonContent.Create(ProvisioningBody(organizationSlug, gymSlug, ownerEmail))
            };
            request.Headers.Add("Idempotency-Key", requestId);
            using var acceptedResponse = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Accepted, acceptedResponse.StatusCode);
            var accepted = (await acceptedResponse.Content.ReadFromJsonAsync<ApiResponse<AcceptedData>>())!.Data;
            operationId = accepted.OperationId;
            organizationId = accepted.OrganizationId;
            gymId = accepted.GymId;
            Assert.Equal("Requested", accepted.Status);
            Assert.Equal($"/api/v1/platform/provisioning/{operationId:D}", accepted.StatusUrl);

            using var replay = new HttpRequestMessage(HttpMethod.Post, "/api/v1/platform/provisioning")
            {
                Content = JsonContent.Create(ProvisioningBody(organizationSlug, gymSlug, ownerEmail))
            };
            replay.Headers.Add("Idempotency-Key", requestId);
            using var replayResponse = await client.SendAsync(replay);
            Assert.Equal(HttpStatusCode.Accepted, replayResponse.StatusCode);
            var replayed = (await replayResponse.Content.ReadFromJsonAsync<ApiResponse<AcceptedData>>())!.Data;
            Assert.Equal(operationId, replayed.OperationId);
            Assert.Equal(organizationId, replayed.OrganizationId);
            Assert.Equal(gymId, replayed.GymId);

            using var conflict = new HttpRequestMessage(HttpMethod.Post, "/api/v1/platform/provisioning")
            {
                Content = JsonContent.Create(ProvisioningBody(organizationSlug, gymSlug + "-different", ownerEmail))
            };
            conflict.Headers.Add("Idempotency-Key", requestId);
            using var conflictResponse = await client.SendAsync(conflict);
            Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
            Assert.Equal("IDEMPOTENCY_KEY_REUSED", (await conflictResponse.Content.ReadFromJsonAsync<ApiErrorResponse>())!.Error.Code);

            ProvisioningStatusData? status = null;
            for (var attempt = 0; attempt < 45; attempt++)
            {
                using var statusResponse = await client.GetAsync($"/api/v1/platform/provisioning/{operationId:D}");
                Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
                status = (await statusResponse.Content.ReadFromJsonAsync<ApiResponse<ProvisioningStatusData>>())!.Data;
                if (status.Status == "Active" || status.Status.EndsWith("Failed", StringComparison.Ordinal))
                {
                    break;
                }

                await Task.Delay(1000);
            }

            Assert.NotNull(status);
            Assert.True(status!.Status == "Active", Serialize(status));
            Assert.Equal(organizationId, status.OrganizationId);
            Assert.Equal(gymId, status.GymId);
            Assert.True(status.OwnerInitialized);
            Assert.False(status.Retryable);
            Assert.Equal(10, status.Steps.Count);
            Assert.All(status.Steps, step => Assert.Equal("Success", step.Status));
            Assert.NotNull(status.Database);
            databaseName = status.Database!.DatabaseName;
            gymDatabaseId = status.Database.DatabaseId;
            Assert.StartsWith($"LogicFit_Gym_{gymId:N}_", databaseName, StringComparison.Ordinal);
            Assert.DoesNotContain("connection", Serialize(status), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("password", Serialize(status), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("secret", Serialize(status), StringComparison.OrdinalIgnoreCase);

            ownerUserId = await VerifyProvisionedGymAsync(databaseName, gymId, actor.UserId, ownerEmail, factory!.Services.GetRequiredService<CanonicalSeedManifestReader>());

            using var retryActive = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/platform/provisioning/{operationId:D}/retry")
            {
                Content = JsonContent.Create(new { reason = "active-operation-must-not-retry" })
            };
            retryActive.Headers.Add("Idempotency-Key", $"retry-{requestId}");
            using var retryResponse = await client.SendAsync(retryActive);
            Assert.Equal(HttpStatusCode.Conflict, retryResponse.StatusCode);

            using var ownerClient = factory!.CreateClient();
            var ownerLogin = await LoginAsync(ownerClient, ownerEmail);
            ownerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerLogin.AccessToken);
            using var ownerProvisioning = new HttpRequestMessage(HttpMethod.Post, "/api/v1/platform/provisioning")
            {
                Content = JsonContent.Create(ProvisioningBody("phase7-owner-cannot-provision", "phase7-owner-cannot-provision", $"other-{requestId}@example.test"))
            };
            ownerProvisioning.Headers.Add("Idempotency-Key", $"owner-{requestId}");
            using var ownerResponse = await ownerClient.SendAsync(ownerProvisioning);
            Assert.Equal(HttpStatusCode.Forbidden, ownerResponse.StatusCode);
            Assert.Equal("GYM_SCOPE_DENIED", (await ownerResponse.Content.ReadFromJsonAsync<ApiErrorResponse>())!.Error.Code);

            using var malformed = await client.PostAsJsonAsync("/api/v1/platform/provisioning", new { organization = new { name = "missing" } });
            Assert.Equal(HttpStatusCode.BadRequest, malformed.StatusCode);
        }
        finally
        {
            await CleanupAsync(operationId, organizationId, gymId, ownerUserId, actor.UserId, databaseName);
        }
    }

    [Fact]
    public async Task ProvisioningRequiresVerifiedMfaStepUp()
    {
        var actor = await CreatePlatformIdentityAsync();
        await using (var db = CreateDb())
        {
            db.MfaFactors.Add(new MfaFactorEntity
            {
                MfaFactorId = Guid.NewGuid(),
                UserId = actor.UserId,
                FactorType = "totp",
                SecretRef = "test-factor-reference",
                Status = "active",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        using var client = factory!.CreateClient();
        var login = await LoginAsync(client, actor.Email);
        Assert.True(login.RequiresMfa);
        Assert.False(login.MfaVerified);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/platform/provisioning")
        {
            Content = JsonContent.Create(ProvisioningBody($"phase7-mfa-org-{actor.UserId:N}", $"phase7-mfa-gym-{actor.UserId:N}", $"phase7-mfa-owner-{actor.UserId:N}@example.test"))
        };
        request.Headers.Add("Idempotency-Key", $"mfa-{actor.UserId:N}");
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("MFA_REQUIRED", (await response.Content.ReadFromJsonAsync<ApiErrorResponse>())!.Error.Code);
    }

    [Fact]
    public async Task RetryResumesRetryableRunAndIsIdempotent()
    {
        var actor = await CreatePlatformIdentityAsync();
        var organizationId = Guid.NewGuid();
        var gymId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var databaseName = string.Empty;

        await using (var db = CreateDb())
        {
            var server = await db.Servers.SingleAsync(x => x.ServerId == PlatformServerDefaults.LocalServerId);
            db.Organizations.Add(new OrganizationEntity
            {
                OrganizationId = organizationId,
                Name = "Phase 7 Retry Organization",
                Slug = $"phase7-retry-org-{operationId:N}",
                Status = "active",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
            db.Gyms.Add(new GymEntity
            {
                GymId = gymId,
                OrganizationId = organizationId,
                Name = "Phase 7 Retry Gym",
                Slug = $"phase7-retry-gym-{operationId:N}",
                Status = "provisioning",
                TimezoneName = "Africa/Cairo",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
            db.ProvisioningRuns.Add(new ProvisioningRunEntity
            {
                ProvisioningRunId = operationId,
                OrganizationId = organizationId,
                GymId = gymId,
                RequestedByUserId = actor.UserId,
                OwnerUserId = actor.UserId,
                Status = ProvisioningContract.ProvisioningFailed,
                CurrentStep = "RequestValidation",
                AttemptNo = 1,
                IdempotencyKeyHash = new string('a', 64),
                RequestFingerprint = new string('b', 64),
                ServerId = server.ServerId,
                RequestedAtUtc = now,
                CompletedAtUtc = now,
                FailureCategory = "transient",
                ErrorCode = "PROVISIONING_FAILED",
                SafeErrorMetadataJson = "{\"step\":\"RequestValidation\",\"retryable\":true}",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
            foreach (var stepKey in ProvisioningContract.StepOrder)
            {
                db.ProvisioningSteps.Add(new ProvisioningStepEntity
                {
                    ProvisioningStepId = Guid.NewGuid(),
                    ProvisioningRunId = operationId,
                    StepKey = stepKey,
                    AttemptNo = 1,
                    Status = stepKey == "RequestValidation" ? "Failed" : "Pending",
                    CompletedAtUtc = stepKey == "RequestValidation" ? now : null,
                    FailureCategory = stepKey == "RequestValidation" ? "transient" : null,
                    ErrorCode = stepKey == "RequestValidation" ? "PROVISIONING_FAILED" : null,
                    Retryable = stepKey == "RequestValidation"
                });
            }

            await db.SaveChangesAsync();
        }

        try
        {
            using var client = factory!.CreateClient();
            var login = await LoginAsync(client, actor.Email);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
            var retryKey = $"retry-{operationId:N}";
            using var retryRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/platform/provisioning/{operationId:D}/retry")
            {
                Content = JsonContent.Create(new { reason = "Transient local provisioning dependency recovered." })
            };
            retryRequest.Headers.Add("Idempotency-Key", retryKey);
            using var retryResponse = await client.SendAsync(retryRequest);
            Assert.Equal(HttpStatusCode.Accepted, retryResponse.StatusCode);
            var accepted = (await retryResponse.Content.ReadFromJsonAsync<ApiResponse<RetryAcceptedData>>())!.Data;
            Assert.Equal(operationId, accepted.OperationId);
            Assert.True(accepted.RetryAccepted);
            Assert.Equal("RequestValidation", accepted.FailedStep);
            Assert.Equal(2, accepted.NextAttemptNo);

            using var replayRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/platform/provisioning/{operationId:D}/retry")
            {
                Content = JsonContent.Create(new { reason = "Transient local provisioning dependency recovered." })
            };
            replayRequest.Headers.Add("Idempotency-Key", retryKey);
            using var replayResponse = await client.SendAsync(replayRequest);
            Assert.Equal(HttpStatusCode.Accepted, replayResponse.StatusCode);
            var replayed = (await replayResponse.Content.ReadFromJsonAsync<ApiResponse<RetryAcceptedData>>())!.Data;
            Assert.Equal(accepted, replayed);

            ProvisioningStatusData? status = null;
            for (var attempt = 0; attempt < 45; attempt++)
            {
                using var statusResponse = await client.GetAsync($"/api/v1/platform/provisioning/{operationId:D}");
                Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
                status = (await statusResponse.Content.ReadFromJsonAsync<ApiResponse<ProvisioningStatusData>>())!.Data;
                if (status.Status == "Active" || status.Status.EndsWith("Failed", StringComparison.Ordinal))
                {
                    break;
                }

                await Task.Delay(1000);
            }

            Assert.NotNull(status);
            Assert.Equal("Active", status!.Status);
            Assert.Equal(2, status.AttemptNo);
            var latestSteps = status.Steps
                .GroupBy(step => step.StepKey)
                .Select(group => group.OrderByDescending(step => step.AttemptNo).First())
                .ToArray();
            Assert.Equal(10, latestSteps.Length);
            Assert.All(latestSteps, step => Assert.Equal("Success", step.Status));
            databaseName = status.Database!.DatabaseName;
            Assert.Equal(20, status.Steps.Count);
        }
        finally
        {
            await CleanupAsync(operationId, organizationId, gymId, actor.UserId, actor.UserId, databaseName);
        }
    }

    [Fact]
    public async Task StartupRecoversAcceptedOperationAndPersistsSafeFailure()
    {
        var actor = await CreatePlatformIdentityAsync();
        var organizationId = Guid.NewGuid();
        var gymId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await using (var db = CreateDb())
        {
            var server = await db.Servers.SingleAsync(x => x.ServerId == PlatformServerDefaults.LocalServerId);
            server.Status = "inactive";
            server.HealthStatus = "unavailable";
            var organization = new OrganizationEntity
            {
                OrganizationId = organizationId,
                Name = "Phase 7 Recovery Organization",
                Slug = $"phase7-recovery-org-{operationId:N}",
                Status = "active",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            var gym = new GymEntity
            {
                GymId = gymId,
                OrganizationId = organizationId,
                Name = "Phase 7 Recovery Gym",
                Slug = $"phase7-recovery-gym-{operationId:N}",
                Status = "provisioning",
                TimezoneName = "Africa/Cairo",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            db.Organizations.Add(organization);
            db.Gyms.Add(gym);
            db.ProvisioningRuns.Add(new ProvisioningRunEntity
            {
                ProvisioningRunId = operationId,
                OrganizationId = organizationId,
                GymId = gymId,
                RequestedByUserId = actor.UserId,
                OwnerUserId = actor.UserId,
                Status = ProvisioningContract.Requested,
                AttemptNo = 1,
                IdempotencyKeyHash = $"recovery-{operationId:N}",
                RequestFingerprint = $"recovery-{operationId:N}",
                ServerId = server.ServerId,
                RequestedAtUtc = now,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
            foreach (var stepKey in ProvisioningContract.StepOrder)
            {
                db.ProvisioningSteps.Add(new ProvisioningStepEntity
                {
                    ProvisioningStepId = Guid.NewGuid(),
                    ProvisioningRunId = operationId,
                    StepKey = stepKey,
                    AttemptNo = 1,
                    Status = "Pending",
                    Retryable = false
                });
            }

            await db.SaveChangesAsync();
        }

        try
        {
            using var recoveryFactory = CreateFactory();
            using var client = recoveryFactory.CreateClient();
            var login = await LoginAsync(client, actor.Email);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

            ProvisioningStatusData? status = null;
            for (var attempt = 0; attempt < 20; attempt++)
            {
                using var response = await client.GetAsync($"/api/v1/platform/provisioning/{operationId:D}");
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                status = (await response.Content.ReadFromJsonAsync<ApiResponse<ProvisioningStatusData>>())!.Data;
                if (status.Status.EndsWith("Failed", StringComparison.Ordinal))
                {
                    break;
                }

                await Task.Delay(250);
            }

            Assert.NotNull(status);
            Assert.Equal("ProvisioningFailed", status!.Status);
            Assert.Equal("ServerPlacement", status.CurrentStep);
            Assert.False(status.Retryable);
            Assert.NotNull(status.Failure);
            Assert.Equal("ServerPlacement", status.Failure!.FailedStep);
        }
        finally
        {
            await RestoreServerAndCleanupAsync(operationId, organizationId, gymId, actor.UserId);
        }
    }

    private static object ProvisioningBody(string organizationSlug, string gymSlug, string ownerEmail)
        => new
        {
            organization = new { name = "Phase 7 Test Organization", slug = organizationSlug },
            gym = new { name = "Phase 7 Test Gym", slug = gymSlug, timezoneName = "Africa/Cairo" },
            serverTarget = new { serverId = PlatformServerDefaults.LocalServerId },
            owner = new { email = ownerEmail, displayName = "Phase 7 Test Owner", initialPassword = Password }
        };

    private async Task<LoginData> LoginAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<LoginData>>())!.Data;
    }

    private async Task<TestIdentity> CreatePlatformIdentityAsync()
    {
        await using var db = CreateDb();
        var role = await db.Roles.SingleAsync(item => item.Name == "Platform Security Admin");
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var user = new UserEntity
        {
            UserId = userId,
            Email = $"phase7-platform-{userId:N}@example.test",
            DisplayName = "Phase 7 Platform Admin",
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
            GymId = null,
            RoleId = role.RoleId,
            ScopeType = "platform",
            Status = "active",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return new TestIdentity(userId, user.Email);
    }

    private WebApplicationFactory<Program> CreateFactory()
        => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LogicFit:SqlServer:ControlPlaneDatabase"] = controlPlaneDatabaseName,
                ["LogicFit:SqlServer:DefaultGymDatabase"] = defaultGymDatabaseName
            })));

    private async Task RestoreServerAndCleanupAsync(Guid operationId, Guid organizationId, Guid gymId, Guid actorUserId)
    {
        await using var db = CreateDb();
        var server = await db.Servers.SingleAsync(x => x.ServerId == PlatformServerDefaults.LocalServerId);
        server.Status = "active";
        server.HealthStatus = "healthy";
        server.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await CleanupAsync(operationId, organizationId, gymId, Guid.Empty, actorUserId, string.Empty);
    }

    private async Task<Guid> VerifyProvisionedGymAsync(string databaseName, Guid gymId, Guid actorId, string ownerEmail, CanonicalSeedManifestReader manifestReader)
    {
        await using var controlPlane = CreateDb();
        var owner = await controlPlane.Users.AsNoTracking().SingleAsync(x => x.Email == ownerEmail);
        var ownerUserId = owner.UserId;
        Assert.Equal("active", owner.Status);
        Assert.True(await controlPlane.UserGymRoles.AnyAsync(x => x.UserId == owner.UserId && x.GymId == gymId && x.Status == "active" && x.Role != null && x.Role.Name == "Gym Security Admin"));
        Assert.Equal(owner.UserId, await controlPlane.Gyms.Where(x => x.GymId == gymId).Select(x => x.OwnerUserId).SingleAsync());

        await using var gym = CreateGymDb(databaseName);
        Assert.True(await gym.Database.CanConnectAsync());
        Assert.Empty(await gym.Database.GetPendingMigrationsAsync());
        Assert.Equal(1133, await gym.Exercises.CountAsync());
        Assert.Equal(297, await gym.Muscles.CountAsync());
        Assert.Equal(367, await gym.Foods.CountAsync());
        Assert.Equal(194, await gym.AnatomyMappings.CountAsync());
        Assert.Equal(1, await gym.GymContexts.CountAsync(x => x.ControlPlaneGymId == gymId && x.Status == "active"));
        Assert.Equal(1, await gym.GymUsers.CountAsync(x => x.ControlPlaneUserId == owner.UserId && x.Status == "active"));

        var before = new
        {
            Exercises = await gym.Exercises.CountAsync(),
            Muscles = await gym.Muscles.CountAsync(),
            Foods = await gym.Foods.CountAsync(),
            Anatomy = await gym.AnatomyMappings.CountAsync()
        };
        await new CanonicalLibrarySeeder(gym, manifestReader).ApplyAsync();
        Assert.Equal(before.Exercises, await gym.Exercises.CountAsync());
        Assert.Equal(before.Muscles, await gym.Muscles.CountAsync());
        Assert.Equal(before.Foods, await gym.Foods.CountAsync());
        Assert.Equal(before.Anatomy, await gym.AnatomyMappings.CountAsync());

        var audit = await controlPlane.AuditEvents.AsNoTracking()
            .Where(x => x.TargetId != null)
            .ToListAsync();
        audit = audit.Where(x => x.Action.StartsWith("PROVISIONING_", StringComparison.Ordinal)).ToList();
        Assert.Contains(audit, x => x.Action == "PROVISIONING_REQUESTED" && x.ActorUserId == actorId);
        Assert.Contains(audit, x => x.Action == "PROVISIONING_ACTIVATED");
        Assert.DoesNotContain(audit, x => (x.MetadataJson ?? string.Empty).Contains("password", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(audit, x => (x.MetadataJson ?? string.Empty).Contains("secret", StringComparison.OrdinalIgnoreCase));
        return ownerUserId;
    }

    private async Task CleanupAsync(Guid operationId, Guid organizationId, Guid gymId, Guid ownerUserId, Guid actorUserId, string databaseName)
    {
        var databasesToDrop = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(databaseName))
        {
            databasesToDrop.Add(databaseName);
        }

        if (DatabaseExists(controlPlaneDatabaseName) && gymId != Guid.Empty)
        {
            await using var registryDb = CreateDb();
            foreach (var registeredDatabase in await registryDb.GymDatabases
                         .AsNoTracking()
                         .Where(x => x.GymId == gymId)
                         .Select(x => x.DatabaseName)
                         .ToArrayAsync())
            {
                databasesToDrop.Add(registeredDatabase);
            }
        }

        foreach (var database in databasesToDrop)
        {
            await DropDatabaseAsync(database);
        }

        await using var db = CreateDb();
        var runIds = operationId == Guid.Empty
            ? await db.ProvisioningRuns.Where(x => x.GymId == gymId).Select(x => x.ProvisioningRunId).ToArrayAsync()
            : [operationId];
        if (runIds.Length > 0)
        {
            await db.ProvisioningSteps.Where(x => runIds.Contains(x.ProvisioningRunId)).ExecuteDeleteAsync();
            await db.ProvisioningRuns.Where(x => runIds.Contains(x.ProvisioningRunId)).ExecuteDeleteAsync();
        }

        // Delete only databases belonging to this test-created Gym. The
        // generated physical database was dropped above.
        await db.GymDatabases.Where(x => x.GymId == gymId).ExecuteDeleteAsync();

        // Remove Gym-scoped role assignments before deleting the Gym because
        // the canonical FK intentionally protects the assignment boundary.
        var testUserIds = new[] { ownerUserId, actorUserId }
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();
        await db.Sessions
            .Where(x => x.GymId == gymId || testUserIds.Contains(x.UserId))
            .ExecuteDeleteAsync();
        await db.UserGymRoles
            .Where(x => x.GymId == gymId || testUserIds.Contains(x.UserId))
            .ExecuteDeleteAsync();

        if (gymId != Guid.Empty)
        {
            await db.Gyms.Where(x => x.GymId == gymId).ExecuteDeleteAsync();
        }

        if (organizationId != Guid.Empty)
        {
            await db.Organizations.Where(x => x.OrganizationId == organizationId).ExecuteDeleteAsync();
        }

        foreach (var userId in new[] { ownerUserId, actorUserId }.Where(x => x != Guid.Empty).Distinct())
        {
            await db.MfaRecoveryCodes.Where(x => x.UserId == userId).ExecuteDeleteAsync();
            await db.MfaFactors.Where(x => x.UserId == userId).ExecuteDeleteAsync();
            await db.PasswordResetTokens.Where(x => x.UserId == userId).ExecuteDeleteAsync();
            await db.Sessions.Where(x => x.UserId == userId).ExecuteDeleteAsync();
            await db.Credentials.Where(x => x.UserId == userId).ExecuteDeleteAsync();
            await db.AuditEvents.Where(x => x.ActorUserId == userId || x.TargetId == userId).ExecuteDeleteAsync();
            await db.Users.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        }
    }

    private static async Task CreateDatabaseAsync(string databaseName)
    {
        if (!Regex.IsMatch(databaseName, "^LogicFit_(?:ControlPlane|Gym)_Phase7Test_[0-9a-f]{32}$", RegexOptions.CultureInvariant))
        {
            throw new InvalidOperationException("The test database name is not an approved isolated Phase 7 test name.");
        }

        await using var connection = new SqlConnection("Server=localhost;Database=master;Integrated Security=True;TrustServerCertificate=True;Encrypt=False");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"IF DB_ID(@databaseName) IS NULL CREATE DATABASE {QuoteIdentifier(databaseName)};";
        command.Parameters.Add(new SqlParameter("@databaseName", System.Data.SqlDbType.NVarChar, 128) { Value = databaseName });
        await command.ExecuteNonQueryAsync();
    }

    private static async Task DropDatabaseAsync(string databaseName)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            return;
        }

        if (!Regex.IsMatch(databaseName, "^LogicFit_(?:ControlPlane|Gym)_Phase7Test_[0-9a-f]{32}$|^LogicFit_Gym_[0-9a-f]{32}_local$", RegexOptions.CultureInvariant))
        {
            throw new InvalidOperationException("The test database name is not an approved isolated Phase 7 test name.");
        }

        await using var connection = new SqlConnection("Server=localhost;Database=master;Integrated Security=True;TrustServerCertificate=True;Encrypt=False");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"IF DB_ID(@databaseName) IS NOT NULL BEGIN ALTER DATABASE {QuoteIdentifier(databaseName)} SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE {QuoteIdentifier(databaseName)}; END;";
        command.Parameters.Add(new SqlParameter("@databaseName", System.Data.SqlDbType.NVarChar, 128) { Value = databaseName });
        await command.ExecuteNonQueryAsync();
    }

    private static bool DatabaseExists(string databaseName)
    {
        using var connection = new SqlConnection("Server=localhost;Database=master;Integrated Security=True;TrustServerCertificate=True;Encrypt=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT DB_ID(@databaseName);";
        command.Parameters.Add(new SqlParameter("@databaseName", System.Data.SqlDbType.NVarChar, 128) { Value = databaseName });
        return command.ExecuteScalar() is not null and not DBNull;
    }

    private static string QuoteIdentifier(string value) => $"[{value.Replace("]", "]]", StringComparison.Ordinal)}]";

    private ControlPlaneDbContext CreateDb()
        => new(new DbContextOptionsBuilder<ControlPlaneDbContext>()
            .UseSqlServer(TestConnectionString(controlPlaneDatabaseName), sql => sql.MigrationsAssembly(typeof(ControlPlaneDbContext).Assembly.GetName().Name))
            .Options);

    private static GymDbContext CreateGymDb(string databaseName)
        => new(new DbContextOptionsBuilder<GymDbContext>()
            .UseSqlServer(TestConnectionString(databaseName), sql => sql.MigrationsAssembly(typeof(GymDbContext).Assembly.GetName().Name))
            .Options);

    private static string TestConnectionString(string databaseName)
        => $"Server=localhost;Database={databaseName};Integrated Security=True;TrustServerCertificate=True;Encrypt=False";

    private static string ProjectRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "database", "seeds", "manifest.json")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("The LogicFit project root was not found for the isolated provisioning test.");
    }

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value);

    private sealed record TestIdentity(Guid UserId, string Email);
    private sealed record LoginData(string AccessToken, Guid SessionId, bool RequiresMfa, string? Challenge, bool MfaVerified, DateTime ExpiresAtUtc, DateTime IdleExpiresAtUtc, DateTime AbsoluteExpiresAtUtc, UserData User);
    private sealed record UserData(Guid UserId, string Email, string DisplayName, string Status, DateTime? LastLoginAtUtc, string Version);
    private sealed record AcceptedData(Guid OperationId, Guid OrganizationId, Guid GymId, string Status, string? CurrentStep, DateTime RequestedAtUtc, string StatusUrl);
    private sealed record ProvisioningStatusData(Guid OperationId, Guid OrganizationId, Guid GymId, string Status, string? CurrentStep, int AttemptNo, DateTime RequestedAtUtc, DateTime? StartedAtUtc, DateTime? CompletedAtUtc, ServerData? Server, DatabaseData? Database, bool OwnerInitialized, bool Retryable, FailureData? Failure, IReadOnlyList<StepData> Steps);
    private sealed record ServerData(Guid ServerId, string Environment, string Status);
    private sealed record DatabaseData(Guid DatabaseId, string DatabaseName, string Status, string? SchemaVersion, string? SeedVersion);
    private sealed record FailureData(string FailureCategory, string ErrorCode, string FailedStep, DateTime OccurredAtUtc, bool Retryable);
    private sealed record RetryAcceptedData(Guid OperationId, string Status, bool RetryAccepted, string FailedStep, string NextStep, int NextAttemptNo, bool Retryable);
    private sealed record StepData(string StepKey, string Status, int AttemptNo, DateTime? StartedAtUtc, DateTime? CompletedAtUtc, bool Retryable, string? FailureCategory);

    private sealed class TestHostEnvironment(string contentRootPath) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "LogicFit.ApiTests";
        public string ContentRootPath { get; set; } = contentRootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(contentRootPath);
    }
}
