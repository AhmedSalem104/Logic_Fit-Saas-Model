using System.Data;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LogicFit.Application;
using LogicFit.Shared;
using LogicFit.Infrastructure.Persistence;
using LogicFit.Infrastructure.Persistence.Entities;
using LogicFit.Infrastructure.Services.Seeding;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LogicFit.Infrastructure.Provisioning;

public sealed class SqlProvisioningService(
    ControlPlaneDbContext db,
    IAuthenticationService authentication,
    IProvisioningQueue queue,
    IPasswordHasher passwordHasher,
    IGymDbContextFactory gymDbContextFactory,
    ISqlServerDatabaseCreator databaseCreator,
    CanonicalSeedManifestReader manifestReader,
    ILogger<SqlProvisioningService> logger) : IProvisioningService, IProvisioningWorkflow
{
    private const string Active = "active";
    private const string Inactive = "inactive";
    private const string Healthy = "healthy";
    private const string Unavailable = "unavailable";
    private const string GymSecurityAdmin = "Gym Security Admin";
    private const string Provisioning = "provisioning";
    private const string DatabaseProvisioning = "Provisioning";
    private const string DatabaseMigrating = "Migrating";
    private const string DatabaseSeeding = "Seeding";
    private const string DatabaseVerifying = "Verifying";
    private const string DatabaseActive = "Active";

    public async Task<AuthResult<ProvisioningAcceptedDto>> RequestAsync(
        AuthenticatedUser currentUser,
        ProvisioningRequest? request,
        string? idempotencyKey,
        AuthRequestContext context,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizePlatformAsync<ProvisioningAcceptedDto>(currentUser, context, requireMfaStepUp: true, cancellationToken: cancellationToken);
        if (authorization is not null)
        {
            return authorization;
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Trim().Length is < 1 or > 128)
        {
            return Failure<ProvisioningAcceptedDto>(400, "VALIDATION_ERROR", "A valid Idempotency-Key is required.", [new("Idempotency-Key", "required")]);
        }

        var normalized = NormalizeRequest(request);
        if (normalized.Error is not null)
        {
            return Failure<ProvisioningAcceptedDto>(400, "VALIDATION_ERROR", normalized.Error, normalized.FieldErrors);
        }

        var normalizedRequest = normalized.Value!;

        var target = await db.Servers.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ServerId == normalizedRequest.ServerId, cancellationToken);
        if (target is null || !IsSelectable(target))
        {
            return Failure<ProvisioningAcceptedDto>(422, "DOMAIN_RULE_VIOLATION", "The selected server is not a selectable registered target.");
        }

        var key = idempotencyKey.Trim();
        var keyHash = Hash($"start:{currentUser.UserId:N}:{NormalizeEnvironment(target.Environment)}:{key}");
        var fingerprint = Hash(CanonicalRequest(normalizedRequest));

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var existing = await db.ProvisioningRuns
            .SingleOrDefaultAsync(x => x.RequestedByUserId == currentUser.UserId && x.IdempotencyKeyHash == keyHash, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestFingerprint, fingerprint, StringComparison.Ordinal))
            {
                return Failure<ProvisioningAcceptedDto>(409, "IDEMPOTENCY_KEY_REUSED", "The Idempotency-Key was already used with a different request.");
            }

            await transaction.CommitAsync(cancellationToken);
            return AuthResult<ProvisioningAcceptedDto>.Success(MapAccepted(existing), 202);
        }

        var duplicateOrganization = await db.Organizations.AnyAsync(x => x.Slug == normalizedRequest.OrganizationSlug, cancellationToken);
        if (duplicateOrganization)
        {
            return Failure<ProvisioningAcceptedDto>(409, "DUPLICATE_RESOURCE", "The organization slug is already registered.");
        }

        var duplicateOwner = await db.Users.AnyAsync(x => x.Email == normalizedRequest.OwnerEmail, cancellationToken);
        if (duplicateOwner)
        {
            return Failure<ProvisioningAcceptedDto>(409, "DUPLICATE_RESOURCE", "The owner email is already registered.");
        }

        var ownerRole = await db.Roles.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ScopeType == "gym" && x.Name == GymSecurityAdmin && x.Status == Active, cancellationToken);
        if (ownerRole is null)
        {
            return Failure<ProvisioningAcceptedDto>(503, "DEPENDENCY_UNAVAILABLE", "The canonical Gym Owner role is not available.");
        }

        var now = DateTime.UtcNow;
        var organizationId = Guid.NewGuid();
        var gymId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();

        db.Organizations.Add(new OrganizationEntity
        {
            OrganizationId = organizationId,
            Name = normalizedRequest.OrganizationName,
            Slug = normalizedRequest.OrganizationSlug,
            Status = Active,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        db.Gyms.Add(new GymEntity
        {
            GymId = gymId,
            OrganizationId = organizationId,
            Name = normalizedRequest.GymName,
            Slug = normalizedRequest.GymSlug,
            Status = Provisioning,
            TimezoneName = normalizedRequest.TimezoneName,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        var owner = new UserEntity
        {
            UserId = ownerUserId,
            Email = normalizedRequest.OwnerEmail,
            DisplayName = normalizedRequest.OwnerDisplayName,
            // The existing IAM status contract admits active/disabled only.
            // The owner remains inaccessible until the provisioning workflow
            // creates the Gym-scoped role assignment and projection.
            Status = Active,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        owner.Credentials.Add(new CredentialEntity
        {
            CredentialId = Guid.NewGuid(),
            UserId = ownerUserId,
            CredentialType = "password",
            SecretHash = passwordHasher.Hash(normalizedRequest.InitialPassword),
            SecretVersion = "lf-pbkdf2-sha256-v1",
            LastRotatedAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        db.Users.Add(owner);

        var run = new ProvisioningRunEntity
        {
            ProvisioningRunId = operationId,
            OrganizationId = organizationId,
            GymId = gymId,
            RequestedByUserId = currentUser.UserId,
            OwnerUserId = ownerUserId,
            Status = ProvisioningContract.Requested,
            AttemptNo = 1,
            IdempotencyKeyHash = keyHash,
            RequestFingerprint = fingerprint,
            ServerId = target.ServerId,
            RequestedAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        db.ProvisioningRuns.Add(run);
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

        AddAudit(run, "PROVISIONING_REQUESTED", context.RequestId, new
        {
            operationId,
            organizationId,
            gymId,
            serverId = target.ServerId,
            state = ProvisioningContract.Requested,
            actorId = currentUser.UserId
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return Failure<ProvisioningAcceptedDto>(409, "DUPLICATE_RESOURCE", "The provisioning request conflicts with an existing platform resource.");
        }

        await queue.EnqueueAsync(operationId, cancellationToken);
        return AuthResult<ProvisioningAcceptedDto>.Success(MapAccepted(run), 202);
    }

    public async Task<AuthResult<ProvisioningStatusDto>> GetStatusAsync(
        AuthenticatedUser currentUser,
        Guid operationId,
        AuthRequestContext context,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizePlatformAsync<ProvisioningStatusDto>(currentUser, context, requireMfaStepUp: false, cancellationToken: cancellationToken);
        if (authorization is not null)
        {
            return authorization;
        }

        var run = await db.ProvisioningRuns.AsNoTracking()
            .Include(x => x.Server)
            .Include(x => x.GymDatabase)
            .Include(x => x.Gym)
            .Include(x => x.Steps)
            .SingleOrDefaultAsync(x => x.ProvisioningRunId == operationId, cancellationToken);
        if (run is null)
        {
            return Failure<ProvisioningStatusDto>(404, "RESOURCE_NOT_FOUND", "The provisioning operation was not found.");
        }

        var failedStep = run.Steps
            .Where(x => x.Status == "Failed")
            .OrderByDescending(x => x.AttemptNo)
            .ThenByDescending(x => x.CompletedAtUtc)
            .FirstOrDefault();
        var ownerInitialized = run.OwnerUserId.HasValue
            && run.Gym is not null
            && run.Gym.OwnerUserId == run.OwnerUserId
            && await db.UserGymRoles.AnyAsync(x => x.UserId == run.OwnerUserId && x.GymId == run.GymId && x.ScopeType == "gym" && x.Status == Active && x.Role != null && x.Role.Name == GymSecurityAdmin, cancellationToken);

        var orderedSteps = run.Steps
            .OrderBy(x => StepOrder(x.StepKey))
            .ThenBy(x => x.AttemptNo)
            .Select(x => new ProvisioningStepDto(x.StepKey, x.Status, x.AttemptNo, x.StartedAtUtc, x.CompletedAtUtc, x.Retryable, x.FailureCategory))
            .ToArray();
        var retryable = failedStep?.Retryable == true && ProvisioningContract.RetryableFailureStates.Contains(run.Status);
        var failure = failedStep is null || !ProvisioningContract.RetryableFailureStates.Contains(run.Status)
            ? null
            : new ProvisioningFailureDto(
                run.FailureCategory ?? failedStep.FailureCategory ?? "unknown",
                run.ErrorCode ?? failedStep.ErrorCode ?? "PROVISIONING_FAILED",
                failedStep.StepKey,
                failedStep.CompletedAtUtc ?? run.UpdatedAtUtc,
                retryable);

        return AuthResult<ProvisioningStatusDto>.Success(new ProvisioningStatusDto(
            run.ProvisioningRunId,
            run.OrganizationId,
            run.GymId,
            run.Status,
            run.CurrentStep,
            run.AttemptNo,
            run.RequestedAtUtc,
            run.StartedAtUtc,
            run.CompletedAtUtc,
            run.Server is null ? null : new ProvisioningServerDto(run.Server.ServerId, run.Server.Environment, run.Server.Status),
            run.GymDatabase is null ? null : new ProvisioningDatabaseDto(run.GymDatabase.GymDatabaseId, run.GymDatabase.DatabaseName, run.GymDatabase.Status, run.GymDatabase.SchemaVersion, run.GymDatabase.SeedVersion),
            ownerInitialized,
            retryable,
            failure,
            orderedSteps));
    }

    public async Task<AuthResult<ProvisioningRetryAcceptedDto>> RetryAsync(
        AuthenticatedUser currentUser,
        Guid operationId,
        ProvisioningRetryRequest? request,
        string? idempotencyKey,
        AuthRequestContext context,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizePlatformAsync<ProvisioningRetryAcceptedDto>(currentUser, context, requireMfaStepUp: true, cancellationToken: cancellationToken);
        if (authorization is not null)
        {
            return authorization;
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Trim().Length is < 1 or > 128)
        {
            return Failure<ProvisioningRetryAcceptedDto>(400, "VALIDATION_ERROR", "A valid Idempotency-Key is required.", [new("Idempotency-Key", "required")]);
        }

        var reason = SafeText(request?.Reason, 500);
        if (string.IsNullOrWhiteSpace(reason))
        {
            return Failure<ProvisioningRetryAcceptedDto>(422, "VALIDATION_ERROR", "A safe retry reason is required.", [new("reason", "required")]);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var run = await db.ProvisioningRuns
            .Include(x => x.Server)
            .Include(x => x.Steps)
            .SingleOrDefaultAsync(x => x.ProvisioningRunId == operationId, cancellationToken);
        if (run is null)
        {
            return Failure<ProvisioningRetryAcceptedDto>(404, "RESOURCE_NOT_FOUND", "The provisioning operation was not found.");
        }

        var environment = NormalizeEnvironment(run.Server?.Environment);
        var keyHash = Hash($"retry:{currentUser.UserId:N}:{environment}:{idempotencyKey.Trim()}");
        var fingerprint = Hash($"{operationId:D}|{reason}");
        if (string.Equals(run.LastRetryIdempotencyKeyHash, keyHash, StringComparison.Ordinal))
        {
            if (!string.Equals(run.LastRetryFingerprint, fingerprint, StringComparison.Ordinal))
            {
                return Failure<ProvisioningRetryAcceptedDto>(409, "IDEMPOTENCY_KEY_REUSED", "The Idempotency-Key was already used with a different retry request.");
            }

            await transaction.CommitAsync(cancellationToken);
            return AuthResult<ProvisioningRetryAcceptedDto>.Success(new ProvisioningRetryAcceptedDto(
                run.ProvisioningRunId,
                FailureStateFor(run.Status, run.LastRetryFailedStep ?? run.CurrentStep),
                true,
                run.LastRetryFailedStep ?? run.CurrentStep ?? "unknown",
                run.LastRetryNextStep ?? run.CurrentStep ?? "unknown",
                run.LastRetryAttemptNo ?? run.AttemptNo,
                true), 202);
        }

        if (run.Status == ProvisioningContract.Active)
        {
            return Failure<ProvisioningRetryAcceptedDto>(409, "INVALID_STATE_TRANSITION", "An active Gym provisioning operation cannot be retried.");
        }

        if (!ProvisioningContract.RetryableFailureStates.Contains(run.Status))
        {
            return Failure<ProvisioningRetryAcceptedDto>(409, "CONCURRENCY_CONFLICT", "The provisioning operation is already in progress.");
        }

        var failedStep = run.Steps
            .Where(x => x.Status == "Failed")
            .OrderByDescending(x => x.AttemptNo)
            .ThenByDescending(x => x.CompletedAtUtc)
            .FirstOrDefault(x => x.StepKey == run.CurrentStep)
            ?? run.Steps.Where(x => x.Status == "Failed").OrderByDescending(x => x.AttemptNo).FirstOrDefault();
        if (failedStep is null || !failedStep.Retryable)
        {
            return Failure<ProvisioningRetryAcceptedDto>(409, "INVALID_STATE_TRANSITION", "The failed provisioning step is not retryable.");
        }

        var failedStepKey = failedStep.StepKey;
        var nextAttempt = run.AttemptNo + 1;
        run.AttemptNo = nextAttempt;
        run.LastRetryIdempotencyKeyHash = keyHash;
        run.LastRetryFingerprint = fingerprint;
        run.LastRetryFailedStep = failedStepKey;
        run.LastRetryNextStep = failedStepKey;
        run.LastRetryAttemptNo = nextAttempt;
        run.UpdatedAtUtc = DateTime.UtcNow;
        db.ProvisioningSteps.Add(new ProvisioningStepEntity
        {
            ProvisioningStepId = Guid.NewGuid(),
            ProvisioningRunId = run.ProvisioningRunId,
            StepKey = failedStepKey,
            AttemptNo = nextAttempt,
            Status = "Pending",
            Retryable = false
        });
        AddAudit(run, "PROVISIONING_RETRY_STARTED", context.RequestId, new
        {
            operationId,
            failedStep = failedStepKey,
            nextStep = failedStepKey,
            nextAttempt,
            actorId = currentUser.UserId,
            reason
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await queue.EnqueueAsync(operationId, cancellationToken);
        return AuthResult<ProvisioningRetryAcceptedDto>.Success(new ProvisioningRetryAcceptedDto(
            operationId,
            run.Status,
            true,
            failedStepKey,
            failedStepKey,
            nextAttempt,
            true), 202);
    }

    public async Task<IReadOnlyList<Guid>> GetRecoverableRunIdsAsync(CancellationToken cancellationToken = default)
        => await db.ProvisioningRuns.AsNoTracking()
            .Where(x => x.Status == ProvisioningContract.Requested
                || x.Status == ProvisioningContract.Provisioning
                || x.Status == ProvisioningContract.Migrating
                || x.Status == ProvisioningContract.Seeding
                || x.Status == ProvisioningContract.Verifying
                || ((x.Status == ProvisioningContract.ProvisioningFailed
                    || x.Status == ProvisioningContract.MigrationFailed
                    || x.Status == ProvisioningContract.SeedingFailed
                    || x.Status == ProvisioningContract.VerificationFailed)
                    && x.LastRetryAttemptNo == x.AttemptNo))
            .OrderBy(x => x.RequestedAtUtc)
            .Select(x => x.ProvisioningRunId)
            .ToArrayAsync(cancellationToken);

    public async Task ProcessAsync(Guid operationId, CancellationToken cancellationToken = default)
    {
        var run = await db.ProvisioningRuns
            .Include(x => x.Steps)
            .SingleOrDefaultAsync(x => x.ProvisioningRunId == operationId, cancellationToken);
        if (run is null || run.Status == ProvisioningContract.Active)
        {
            return;
        }

        if (ProvisioningContract.RetryableFailureStates.Contains(run.Status))
        {
            if (run.LastRetryAttemptNo != run.AttemptNo)
            {
                return;
            }

            PromoteRetryState(run);
            await db.SaveChangesAsync(cancellationToken);
        }

        try
        {
            if (run.Status == ProvisioningContract.Requested)
            {
                if (!await ExecuteStepAsync(run, "RequestValidation", ProvisioningContract.Requested, ProvisioningContract.ProvisioningFailed, null, () => ValidateAcceptedResourcesAsync(run, cancellationToken), cancellationToken))
                {
                    return;
                }

                run.Status = ProvisioningContract.Provisioning;
                run.CurrentStep = "OrganizationCreation";
                run.StartedAtUtc ??= DateTime.UtcNow;
                run.UpdatedAtUtc = DateTime.UtcNow;
                AddAudit(run, "PROVISIONING_STARTED", WorkerRequestId(run), new { operationId, state = run.Status, actorId = run.RequestedByUserId });
                await db.SaveChangesAsync(cancellationToken);
            }

            if (run.Status == ProvisioningContract.Provisioning)
            {
                if (!await ExecuteProvisioningStageAsync(run, cancellationToken))
                {
                    return;
                }
            }

            if (run.Status == ProvisioningContract.Migrating)
            {
                if (!await ExecuteMigrationStageAsync(run, cancellationToken))
                {
                    return;
                }
            }

            if (run.Status == ProvisioningContract.Seeding)
            {
                if (!await ExecuteSeedingStageAsync(run, cancellationToken))
                {
                    return;
                }
            }

            if (run.Status == ProvisioningContract.Verifying)
            {
                if (!await ExecuteVerificationStageAsync(run, cancellationToken))
                {
                    return;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            var failureState = FailureStateFor(run.Status, run.CurrentStep);
            await MarkFailedAsync(run, failureState, run.CurrentStep ?? "unknown", exception, cancellationToken);
        }
    }

    private async Task<bool> ExecuteProvisioningStageAsync(ProvisioningRunEntity run, CancellationToken cancellationToken)
    {
        if (!await ExecuteStepAsync(run, "OrganizationCreation", ProvisioningContract.Provisioning, ProvisioningContract.ProvisioningFailed, null, async () =>
        {
            if (!await db.Organizations.AnyAsync(x => x.OrganizationId == run.OrganizationId, cancellationToken))
            {
                throw new InvalidDataException("The accepted organization registry record is missing.");
            }
        }, cancellationToken))
        {
            return false;
        }

        if (!await ExecuteStepAsync(run, "GymRegistryCreation", ProvisioningContract.Provisioning, ProvisioningContract.ProvisioningFailed, null, async () =>
        {
            if (!await db.Gyms.AnyAsync(x => x.GymId == run.GymId && x.OrganizationId == run.OrganizationId && x.Status == Provisioning, cancellationToken))
            {
                throw new InvalidDataException("The accepted Gym registry record is missing or has an invalid state.");
            }
        }, cancellationToken))
        {
            return false;
        }

        if (!await ExecuteStepAsync(run, "ServerPlacement", ProvisioningContract.Provisioning, ProvisioningContract.ProvisioningFailed, null, async () =>
        {
            var server = await db.Servers.SingleOrDefaultAsync(x => x.ServerId == run.ServerId, cancellationToken);
            if (server is null || !IsSelectable(server))
            {
                throw new InvalidOperationException("The registered provisioning server is not selectable.");
            }
        }, cancellationToken))
        {
            return false;
        }

        if (!await ExecuteStepAsync(run, "DatabaseCreation", ProvisioningContract.Provisioning, ProvisioningContract.ProvisioningFailed, null, async () =>
        {
            var server = await db.Servers.SingleAsync(x => x.ServerId == run.ServerId, cancellationToken);
            var databaseName = DatabaseName(run.GymId, server.Environment);
            var registry = run.GymDatabaseId.HasValue
                ? await db.GymDatabases.SingleOrDefaultAsync(x => x.GymDatabaseId == run.GymDatabaseId.Value, cancellationToken)
                : await db.GymDatabases.SingleOrDefaultAsync(x => x.GymId == run.GymId && x.Environment == NormalizeEnvironment(server.Environment), cancellationToken);

            if (registry is null)
            {
                registry = new GymDatabaseEntity
                {
                    GymDatabaseId = Guid.NewGuid(),
                    GymId = run.GymId,
                    ServerId = server.ServerId,
                    DatabaseName = databaseName,
                    Environment = NormalizeEnvironment(server.Environment),
                    Status = DatabaseProvisioning,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                db.GymDatabases.Add(registry);
                run.GymDatabaseId = registry.GymDatabaseId;
                run.UpdatedAtUtc = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
            }

            if (registry.GymId != run.GymId || registry.ServerId != server.ServerId || registry.DatabaseName != databaseName)
            {
                throw new InvalidDataException("The existing database registry record does not belong to this provisioning operation.");
            }

            await databaseCreator.EnsureCreatedAsync(registry.DatabaseName, cancellationToken);
            registry.Status = DatabaseProvisioning;
            registry.UpdatedAtUtc = DateTime.UtcNow;
            AddAudit(run, "PROVISIONING_DATABASE_CREATED", WorkerRequestId(run), new
            {
                operationId = run.ProvisioningRunId,
                run.OrganizationId,
                run.GymId,
                serverId = server.ServerId,
                databaseId = registry.GymDatabaseId,
                state = run.Status
            });
            await db.SaveChangesAsync(cancellationToken);
        }, cancellationToken))
        {
            return false;
        }

        run.Status = ProvisioningContract.Migrating;
        run.CurrentStep = "EfCoreMigrations";
        run.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<bool> ExecuteMigrationStageAsync(ProvisioningRunEntity run, CancellationToken cancellationToken)
    {
        var succeeded = await ExecuteStepAsync(run, "EfCoreMigrations", ProvisioningContract.Migrating, ProvisioningContract.MigrationFailed, "PROVISIONING_MIGRATING", async () =>
        {
            var registry = await GetDatabaseRegistryAsync(run, cancellationToken);
            await using var gym = gymDbContextFactory.Create(registry.DatabaseName);
            logger.LogInformation("Provisioning migration started for operation {OperationId}.", run.ProvisioningRunId);
            await gym.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Provisioning migration completed for operation {OperationId}.", run.ProvisioningRunId);
            var applied = (await gym.Database.GetAppliedMigrationsAsync(cancellationToken)).LastOrDefault();
            logger.LogInformation("Provisioning migration history read for operation {OperationId}.", run.ProvisioningRunId);
            if (string.IsNullOrWhiteSpace(applied))
            {
                throw new InvalidDataException("The target Gym database has no applied EF Core migration.");
            }

            registry.SchemaVersion = applied;
            registry.Status = DatabaseMigrating;
            registry.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Provisioning migration registry persisted for operation {OperationId}.", run.ProvisioningRunId);
        }, cancellationToken);
        if (!succeeded)
        {
            return false;
        }

        var database = await GetDatabaseRegistryAsync(run, cancellationToken);
        database.Status = DatabaseSeeding;
        run.Status = ProvisioningContract.Seeding;
        run.CurrentStep = "CanonicalSeeding";
        run.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<bool> ExecuteSeedingStageAsync(ProvisioningRunEntity run, CancellationToken cancellationToken)
    {
        var succeeded = await ExecuteStepAsync(run, "CanonicalSeeding", ProvisioningContract.Seeding, ProvisioningContract.SeedingFailed, "PROVISIONING_SEEDING", async () =>
        {
            var registry = await GetDatabaseRegistryAsync(run, cancellationToken);
            await using var gym = gymDbContextFactory.Create(registry.DatabaseName);
            await new CanonicalLibrarySeeder(gym, manifestReader).ApplyAsync(cancellationToken);
            registry.SeedVersion = manifestReader.Read().SeedVersion;
            registry.Status = DatabaseSeeding;
            registry.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
        if (!succeeded)
        {
            return false;
        }

        var database = await GetDatabaseRegistryAsync(run, cancellationToken);
        database.Status = DatabaseVerifying;
        run.Status = ProvisioningContract.Verifying;
        run.CurrentStep = "Verification";
        run.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<bool> ExecuteVerificationStageAsync(ProvisioningRunEntity run, CancellationToken cancellationToken)
    {
        var succeeded = await ExecuteStepAsync(run, "Verification", ProvisioningContract.Verifying, ProvisioningContract.VerificationFailed, "PROVISIONING_VERIFYING", async () =>
        {
            var registry = await GetDatabaseRegistryAsync(run, cancellationToken);
            await using var gym = gymDbContextFactory.Create(registry.DatabaseName);
            var pending = await gym.Database.GetPendingMigrationsAsync(cancellationToken);
            if (pending.Any())
            {
                throw new InvalidDataException("The target Gym database still has pending EF Core migrations.");
            }

            await EnsureGymContextAsync(gym, run, cancellationToken);
            var manifest = manifestReader.Read();
            var actual = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["muscle-groups"] = await gym.MuscleGroups.CountAsync(cancellationToken),
                ["muscles"] = await gym.Muscles.CountAsync(cancellationToken),
                ["equipment"] = await gym.Equipment.CountAsync(cancellationToken),
                ["exercise-categories"] = await gym.ExerciseCategories.CountAsync(cancellationToken),
                ["levels"] = await gym.Levels.CountAsync(cancellationToken),
                ["exercises"] = await gym.Exercises.CountAsync(cancellationToken),
                ["anatomy-mappings"] = await gym.AnatomyMappings.CountAsync(cancellationToken),
                ["food-categories"] = await gym.FoodCategories.CountAsync(cancellationToken),
                ["units"] = await gym.FoodUnits.CountAsync(cancellationToken),
                ["foods"] = await gym.Foods.CountAsync(cancellationToken)
            };
            foreach (var dataset in manifest.Datasets.Where(x => actual.ContainsKey(x.Dataset)))
            {
                if (actual[dataset.Dataset] != dataset.RecordCount)
                {
                    throw new InvalidDataException($"The canonical seed count for {dataset.Dataset} is invalid.");
                }
            }

            var installations = await gym.SeedInstallations.CountAsync(cancellationToken);
            if (installations != manifest.Datasets.Count)
            {
                throw new InvalidDataException("The canonical seed installation manifest is incomplete.");
            }

            registry.SchemaVersion = (await gym.Database.GetAppliedMigrationsAsync(cancellationToken)).LastOrDefault();
            registry.SeedVersion = manifest.SeedVersion;
            registry.Status = DatabaseVerifying;
            registry.LastHealthAtUtc = DateTime.UtcNow;
            registry.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
        if (!succeeded)
        {
            return false;
        }

        if (!await ExecuteStepAsync(run, "OwnerInitialization", ProvisioningContract.Verifying, ProvisioningContract.VerificationFailed, null, () => InitializeOwnerAsync(run, cancellationToken), cancellationToken))
        {
            return false;
        }

        if (!await ExecuteStepAsync(run, "Activation", ProvisioningContract.Verifying, ProvisioningContract.VerificationFailed, null, () => ActivateAsync(run, cancellationToken), cancellationToken))
        {
            return false;
        }

        return true;
    }

    private async Task<bool> ExecuteStepAsync(
        ProvisioningRunEntity run,
        string stepKey,
        string runState,
        string failureState,
        string? startAudit,
        Func<Task> action,
        CancellationToken cancellationToken)
    {
        run.Status = runState;
        run.CurrentStep = stepKey;
        run.StartedAtUtc ??= DateTime.UtcNow;
        run.UpdatedAtUtc = DateTime.UtcNow;
        var step = await db.ProvisioningSteps
            .SingleOrDefaultAsync(x => x.ProvisioningRunId == run.ProvisioningRunId && x.StepKey == stepKey && x.AttemptNo == run.AttemptNo, cancellationToken);
        if (step is null)
        {
            step = new ProvisioningStepEntity
            {
                ProvisioningStepId = Guid.NewGuid(),
                ProvisioningRunId = run.ProvisioningRunId,
                StepKey = stepKey,
                AttemptNo = run.AttemptNo,
                Status = "Pending",
                Retryable = false
            };
            db.ProvisioningSteps.Add(step);
        }

        if (step.Status == "Success")
        {
            return true;
        }

        step.Status = "Running";
        step.StartedAtUtc ??= DateTime.UtcNow;
        step.CompletedAtUtc = null;
        step.FailureCategory = null;
        step.ErrorCode = null;
        step.Retryable = false;
        await db.SaveChangesAsync(cancellationToken);
        if (startAudit is not null)
        {
            AddAudit(run, startAudit, WorkerRequestId(run), new { operationId = run.ProvisioningRunId, run.GymId, run.OrganizationId, step = stepKey, state = runState, actorId = run.RequestedByUserId });
            await db.SaveChangesAsync(cancellationToken);
        }

        try
        {
            await action();
            step.Status = "Success";
            step.CompletedAtUtc = DateTime.UtcNow;
            step.Retryable = false;
            run.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            await MarkFailedAsync(run, failureState, stepKey, exception, cancellationToken);
            return false;
        }
    }

    private async Task ValidateAcceptedResourcesAsync(ProvisioningRunEntity run, CancellationToken cancellationToken)
    {
        // The acceptance transaction is the source of truth for these records.
        // This step intentionally verifies existence only; it never recreates a
        // different organization or Gym after an accepted operation.
        if (!await db.Organizations.AnyAsync(x => x.OrganizationId == run.OrganizationId, cancellationToken)
            || !await db.Gyms.AnyAsync(x => x.GymId == run.GymId && x.OrganizationId == run.OrganizationId, cancellationToken)
            || !await db.Users.AnyAsync(x => x.UserId == run.OwnerUserId, cancellationToken))
        {
            throw new InvalidDataException("An accepted provisioning registry or Owner record is missing.");
        }
    }

    private async Task InitializeOwnerAsync(ProvisioningRunEntity run, CancellationToken cancellationToken)
    {
        if (!run.OwnerUserId.HasValue)
        {
            throw new InvalidDataException("The provisioning operation has no persisted Owner identity.");
        }

        var registry = await GetDatabaseRegistryAsync(run, cancellationToken);
        await using (var gym = gymDbContextFactory.Create(registry.DatabaseName))
        {
            var context = await gym.GymContexts.SingleOrDefaultAsync(x => x.ControlPlaneGymId == run.GymId, cancellationToken)
                ?? throw new InvalidDataException("The target Gym context is missing.");
            context.Status = Active;
            var projection = await gym.GymUsers.SingleOrDefaultAsync(x => x.ControlPlaneUserId == run.OwnerUserId.Value, cancellationToken);
            if (projection is null)
            {
                gym.GymUsers.Add(new GymUserEntity
                {
                    GymUserId = Guid.NewGuid(),
                    ControlPlaneUserId = run.OwnerUserId.Value,
                    Status = Active,
                    DisplayName = await db.Users.Where(x => x.UserId == run.OwnerUserId.Value).Select(x => x.DisplayName).SingleAsync(cancellationToken),
                    LastPermissionSyncAtUtc = DateTime.UtcNow,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                });
            }
            else
            {
                projection.Status = Active;
                projection.LastPermissionSyncAtUtc = DateTime.UtcNow;
                projection.UpdatedAtUtc = DateTime.UtcNow;
            }
            await gym.SaveChangesAsync(cancellationToken);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var user = await db.Users.SingleOrDefaultAsync(x => x.UserId == run.OwnerUserId.Value, cancellationToken)
            ?? throw new InvalidDataException("The persisted Owner identity is missing.");
        var role = await db.Roles.SingleOrDefaultAsync(x => x.ScopeType == "gym" && x.Name == GymSecurityAdmin && x.Status == Active, cancellationToken)
            ?? throw new InvalidDataException("The canonical Gym Owner role is missing.");
        var assignment = await db.UserGymRoles.SingleOrDefaultAsync(x => x.UserId == user.UserId && x.GymId == run.GymId && x.RoleId == role.RoleId, cancellationToken);
        if (assignment is null)
        {
            db.UserGymRoles.Add(new UserGymRoleEntity
            {
                AssignmentId = Guid.NewGuid(),
                UserId = user.UserId,
                GymId = run.GymId,
                RoleId = role.RoleId,
                ScopeType = "gym",
                Status = Active,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            assignment.Status = Active;
            assignment.UpdatedAtUtc = DateTime.UtcNow;
        }
        user.Status = Active;
        user.UpdatedAtUtc = DateTime.UtcNow;
        var gymRegistry = await db.Gyms.SingleAsync(x => x.GymId == run.GymId, cancellationToken);
        gymRegistry.OwnerUserId = user.UserId;
        gymRegistry.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task ActivateAsync(ProvisioningRunEntity run, CancellationToken cancellationToken)
    {
        logger.LogInformation("Provisioning activation started for operation {OperationId}.", run.ProvisioningRunId);
        var registry = await GetDatabaseRegistryAsync(run, cancellationToken);
        logger.LogInformation("Provisioning activation loaded database registry for operation {OperationId}.", run.ProvisioningRunId);
        var gym = await db.Gyms.SingleAsync(x => x.GymId == run.GymId, cancellationToken);
        logger.LogInformation("Provisioning activation loaded Gym registry for operation {OperationId}.", run.ProvisioningRunId);
        if (!gym.OwnerUserId.HasValue || gym.OwnerUserId != run.OwnerUserId)
        {
            throw new InvalidDataException("The Gym Owner was not initialized before activation.");
        }

        gym.Status = Active;
        gym.UpdatedAtUtc = DateTime.UtcNow;
        registry.Status = DatabaseActive;
        registry.LastHealthAtUtc = DateTime.UtcNow;
        registry.UpdatedAtUtc = DateTime.UtcNow;
        run.Status = ProvisioningContract.Active;
        run.CurrentStep = null;
        run.CompletedAtUtc = DateTime.UtcNow;
        run.UpdatedAtUtc = DateTime.UtcNow;
        AddAudit(run, "PROVISIONING_ACTIVATED", WorkerRequestId(run), new
        {
            operationId = run.ProvisioningRunId,
            run.OrganizationId,
            run.GymId,
            serverId = run.ServerId,
            databaseId = run.GymDatabaseId,
            state = run.Status,
            actorId = run.RequestedByUserId
        });
        logger.LogInformation("Provisioning activation persisting final state for operation {OperationId}.", run.ProvisioningRunId);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Provisioning activation completed for operation {OperationId}.", run.ProvisioningRunId);
    }

    private async Task EnsureGymContextAsync(GymDbContext gym, ProvisioningRunEntity run, CancellationToken cancellationToken)
    {
        var registry = await db.Gyms.AsNoTracking().SingleAsync(x => x.GymId == run.GymId, cancellationToken);
        var context = await gym.GymContexts.SingleOrDefaultAsync(x => x.ControlPlaneGymId == run.GymId, cancellationToken);
        if (context is null)
        {
            gym.GymContexts.Add(new GymContextEntity
            {
                GymContextId = Guid.NewGuid(),
                ControlPlaneGymId = run.GymId,
                GymCode = $"gym-{run.GymId:N}",
                DisplayName = registry.Name,
                TimezoneName = registry.TimezoneName,
                Status = Provisioning,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
            await gym.SaveChangesAsync(cancellationToken);
            return;
        }

        if (context.DisplayName != registry.Name || context.TimezoneName != registry.TimezoneName || context.ControlPlaneGymId != run.GymId)
        {
            throw new InvalidDataException("The target Gym context does not match the Control Plane registry.");
        }
    }

    private async Task<GymDatabaseEntity> GetDatabaseRegistryAsync(ProvisioningRunEntity run, CancellationToken cancellationToken)
        => run.GymDatabaseId.HasValue
            ? await db.GymDatabases.SingleAsync(x => x.GymDatabaseId == run.GymDatabaseId.Value && x.GymId == run.GymId, cancellationToken)
            : throw new InvalidDataException("The provisioning operation has no database registry record.");

    private async Task MarkFailedAsync(ProvisioningRunEntity run, string failureState, string stepKey, Exception exception, CancellationToken cancellationToken)
    {
        var step = await db.ProvisioningSteps.SingleOrDefaultAsync(x => x.ProvisioningRunId == run.ProvisioningRunId && x.StepKey == stepKey && x.AttemptNo == run.AttemptNo, cancellationToken);
        var isNewStep = step is null;
        step ??= new ProvisioningStepEntity
        {
            ProvisioningStepId = Guid.NewGuid(),
            ProvisioningRunId = run.ProvisioningRunId,
            StepKey = stepKey,
            AttemptNo = run.AttemptNo
        };
        if (isNewStep)
        {
            db.ProvisioningSteps.Add(step);
        }

        var retryable = IsRetryable(exception);
        var category = FailureCategory(exception);
        var errorCode = ErrorCodeFor(failureState);
        step.Status = "Failed";
        step.CompletedAtUtc = DateTime.UtcNow;
        step.Retryable = retryable;
        step.FailureCategory = category;
        step.ErrorCode = errorCode;
        step.SafeMetadataJson = JsonSerializer.Serialize(new { step = stepKey, category, retryable });
        run.Status = failureState;
        run.CurrentStep = stepKey;
        run.FailureCategory = category;
        run.ErrorCode = errorCode;
        run.SafeErrorMetadataJson = JsonSerializer.Serialize(new { step = stepKey, category, retryable });
        run.CompletedAtUtc = DateTime.UtcNow;
        run.UpdatedAtUtc = DateTime.UtcNow;
        AddAudit(run, "PROVISIONING_FAILED", WorkerRequestId(run), new
        {
            operationId = run.ProvisioningRunId,
            run.OrganizationId,
            run.GymId,
            serverId = run.ServerId,
            databaseId = run.GymDatabaseId,
            state = failureState,
            step = stepKey,
            failureCategory = category,
            errorCode,
            retryable,
            actorId = run.RequestedByUserId
        });
        logger.LogError("Provisioning operation {OperationId} failed at {Step} with category {Category}.", run.ProvisioningRunId, stepKey, category);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static void PromoteRetryState(ProvisioningRunEntity run)
    {
        run.Status = run.CurrentStep switch
        {
            "RequestValidation" => ProvisioningContract.Requested,
            "EfCoreMigrations" => ProvisioningContract.Migrating,
            "CanonicalSeeding" => ProvisioningContract.Seeding,
            "Verification" or "OwnerInitialization" or "Activation" => ProvisioningContract.Verifying,
            _ => ProvisioningContract.Provisioning
        };
        run.CompletedAtUtc = null;
        run.FailureCategory = null;
        run.ErrorCode = null;
        run.SafeErrorMetadataJson = null;
        run.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string FailureStateFor(string status, string? step)
        => status switch
        {
            ProvisioningContract.Migrating => ProvisioningContract.MigrationFailed,
            ProvisioningContract.Seeding => ProvisioningContract.SeedingFailed,
            ProvisioningContract.Verifying => ProvisioningContract.VerificationFailed,
            _ when step is "EfCoreMigrations" => ProvisioningContract.MigrationFailed,
            _ when step is "CanonicalSeeding" => ProvisioningContract.SeedingFailed,
            _ when step is "Verification" or "OwnerInitialization" or "Activation" => ProvisioningContract.VerificationFailed,
            _ => ProvisioningContract.ProvisioningFailed
        };

    private static string ErrorCodeFor(string failureState)
        => failureState switch
        {
            ProvisioningContract.MigrationFailed => "MIGRATION_FAILED",
            ProvisioningContract.SeedingFailed => "SEEDING_FAILED",
            ProvisioningContract.VerificationFailed => "VERIFICATION_FAILED",
            _ => "PROVISIONING_FAILED"
        };

    private static bool IsRetryable(Exception exception)
    {
        if (exception is TimeoutException or IOException)
        {
            return true;
        }

        if (exception is SqlException sqlException)
        {
            return sqlException.Number is -2 or 53 or 1205 or 1222 or 4060 or 40197 or 40501 or 49918 or 49919 or 49920;
        }

        return exception.InnerException is not null && IsRetryable(exception.InnerException);
    }

    private static string FailureCategory(Exception exception)
        => exception switch
        {
            InvalidDataException or ArgumentException => "integrity",
            InvalidOperationException => "configuration",
            TimeoutException or IOException => "transient",
            SqlException => "sql_dependency",
            _ => "dependency"
        };

    private async Task<AuthResult<T>?> AuthorizePlatformAsync<T>(AuthenticatedUser currentUser, AuthRequestContext context, bool requireMfaStepUp, CancellationToken cancellationToken)
    {
        if (requireMfaStepUp && !currentUser.IsMfaVerified)
        {
            return Failure<T>(403, "MFA_REQUIRED", "Complete the verified MFA step-up before provisioning a Gym.");
        }

        if (currentUser.GymId.HasValue)
        {
            await WriteDeniedAuditAsync(currentUser.UserId, context, "gym_scope", currentUser.GymId, cancellationToken);
            return Failure<T>(403, "GYM_SCOPE_DENIED", "Gym users cannot invoke Platform provisioning operations.");
        }

        bool allowed;
        try
        {
            allowed = await authentication.HasPermissionAsync(currentUser, ProvisioningContract.Permission, cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }

        if (!allowed)
        {
            await WriteDeniedAuditAsync(currentUser.UserId, context, ProvisioningContract.Permission, null, cancellationToken);
            return Failure<T>(403, "PERMISSION_DENIED", "The authenticated user is not authorized for provisioning.");
        }

        return null;
    }

    private async Task WriteDeniedAuditAsync(Guid actorUserId, AuthRequestContext context, string reason, Guid? scopeId, CancellationToken cancellationToken)
    {
        var audit = new AuditEventEntity
        {
            AuditEventId = Guid.NewGuid(),
            RequestId = SafeText(context.RequestId, 80),
            ActorUserId = actorUserId,
            ScopeType = scopeId.HasValue ? "gym" : "platform",
            ScopeId = scopeId,
            TargetType = "provisioning.run",
            Action = "authz.permission_denied",
            Result = "failure",
            Reason = SafeText(reason, 500),
            MetadataJson = JsonSerializer.Serialize(new { permission = ProvisioningContract.Permission, actorId = actorUserId }),
            OccurredAtUtc = DateTime.UtcNow
        };
        db.AuditEvents.Add(audit);
        await db.SaveChangesAsync(cancellationToken);
    }

    private void AddAudit(ProvisioningRunEntity? run, string action, string requestId, object metadata)
    {
        db.AuditEvents.Add(new AuditEventEntity
        {
            AuditEventId = Guid.NewGuid(),
            RequestId = SafeText(requestId, 80),
            ActorUserId = run?.RequestedByUserId,
            ScopeType = "platform",
            TargetType = "provisioning.run",
            TargetId = run?.ProvisioningRunId,
            Action = SafeText(action, 120) ?? "unknown",
            Result = action == "PROVISIONING_FAILED" ? "failure" : "success",
            MetadataJson = JsonSerializer.Serialize(metadata),
            OccurredAtUtc = DateTime.UtcNow
        });
    }

    private static string WorkerRequestId(ProvisioningRunEntity run) => $"provisioning:{run.ProvisioningRunId:D}";

    private static ProvisioningAcceptedDto MapAccepted(ProvisioningRunEntity run)
        => new(run.ProvisioningRunId, run.OrganizationId, run.GymId, run.Status, run.CurrentStep, run.RequestedAtUtc, $"/api/v1/platform/provisioning/{run.ProvisioningRunId:D}");

    private static bool IsSelectable(ServerEntity server)
        => string.Equals(server.Status, Active, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(server.HealthStatus, Unavailable, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(server.Environment);

    private static int StepOrder(string stepKey)
    {
        for (var index = 0; index < ProvisioningContract.StepOrder.Count; index++)
        {
            if (string.Equals(ProvisioningContract.StepOrder[index], stepKey, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return int.MaxValue;
    }

    private static string DatabaseName(Guid gymId, string environment)
    {
        var normalized = NormalizeEnvironment(environment);
        var value = $"LogicFit_Gym_{gymId:N}_{normalized}";
        if (value.Length > 128)
        {
            throw new InvalidDataException("The generated Gym database name exceeds the SQL Server identifier limit.");
        }

        return value;
    }

    private static string NormalizeEnvironment(string? value)
    {
        var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        return Regex.Replace(normalized, "[^a-z0-9-]", "-");
    }

    private static (NormalizedProvisioningRequest? Value, string? Error, IReadOnlyList<ApiFieldError>? FieldErrors) NormalizeRequest(ProvisioningRequest? request)
    {
        if (request?.Organization is null || request.Gym is null || request.ServerTarget is null || request.Owner is null)
        {
            return (null, "organization, gym, serverTarget, and owner are required.", [new("request", "required")]);
        }

        var organizationName = SafeText(request.Organization.Name, 160);
        var organizationSlug = NormalizeSlug(request.Organization.Slug, 120);
        var gymName = SafeText(request.Gym.Name, 160);
        var gymSlug = NormalizeSlug(request.Gym.Slug, 120);
        var timezone = SafeText(request.Gym.TimezoneName, 80);
        var email = SafeText(request.Owner.Email, 320)?.ToLowerInvariant();
        var displayName = SafeText(request.Owner.DisplayName, 160);
        var password = request.Owner.InitialPassword;
        var errors = new List<ApiFieldError>();
        if (organizationName is null) errors.Add(new("organization.name", "required"));
        if (organizationSlug is null) errors.Add(new("organization.slug", "invalid"));
        if (gymName is null) errors.Add(new("gym.name", "required"));
        if (gymSlug is null) errors.Add(new("gym.slug", "invalid"));
        if (timezone is null || !IsTimeZone(timezone)) errors.Add(new("gym.timezoneName", "invalid"));
        if (request.ServerTarget.ServerId == Guid.Empty) errors.Add(new("serverTarget.serverId", "required"));
        if (email is null || !IsEmail(email)) errors.Add(new("owner.email", "invalid"));
        if (displayName is null) errors.Add(new("owner.displayName", "required"));
        if (string.IsNullOrEmpty(password)) errors.Add(new("owner.initialPassword", "required"));
        else if (password.Length < 12) errors.Add(new("owner.initialPassword", "policy"));
        else if (password.Length > 256) errors.Add(new("owner.initialPassword", "policy"));
        if (errors.Count > 0)
        {
            return (null, "The provisioning request failed validation.", errors);
        }

        return (new NormalizedProvisioningRequest(organizationName!, organizationSlug!, gymName!, gymSlug!, timezone!, request.ServerTarget.ServerId, email!, displayName!, password!), null, null);
    }

    private static string CanonicalRequest(NormalizedProvisioningRequest request)
        => string.Join("|", request.OrganizationName, request.OrganizationSlug, request.GymName, request.GymSlug, request.TimezoneName, request.ServerId.ToString("D"), request.OwnerEmail, request.OwnerDisplayName);

    private static bool IsTimeZone(string value)
    {
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(value);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                return TimeZoneInfo.TryConvertIanaIdToWindowsId(value, out var windowsId)
                    && !string.IsNullOrWhiteSpace(windowsId)
                    && TimeZoneInfo.FindSystemTimeZoneById(windowsId) is not null;
            }
            catch (TimeZoneNotFoundException)
            {
                return false;
            }
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }

    private static bool IsEmail(string value)
    {
        if (value.Length is < 3 or > 320 || value.Contains(' ') || value.IndexOf('@') <= 0 || value.LastIndexOf('@') != value.IndexOf('@') || value.EndsWith('@'))
        {
            return false;
        }

        try
        {
            return new MailAddress(value).Address.Equals(value, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string? NormalizeSlug(string? value, int maxLength)
    {
        var normalized = SafeText(value, maxLength)?.ToLowerInvariant();
        return normalized is not null && Regex.IsMatch(normalized, "^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)
            ? normalized
            : null;
    }

    private static string? SafeText(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength && !trimmed.Any(char.IsControl) ? trimmed : null;
    }

    private static string Hash(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static bool IsUniqueViolation(DbUpdateException exception)
        => exception.InnerException is SqlException sql && sql.Number is 2601 or 2627;

    private static AuthResult<T> Failure<T>(int statusCode, string code, string message, IReadOnlyList<ApiFieldError>? fields = null)
        => AuthResult<T>.Failure(statusCode, code, message, fields);

    private sealed record NormalizedProvisioningRequest(
        string OrganizationName,
        string OrganizationSlug,
        string GymName,
        string GymSlug,
        string TimezoneName,
        Guid ServerId,
        string OwnerEmail,
        string OwnerDisplayName,
        string InitialPassword);
}
