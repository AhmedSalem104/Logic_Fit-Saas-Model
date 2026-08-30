using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LogicFit.Application;
using LogicFit.Domain.Members;
using LogicFit.Infrastructure.Persistence;
using LogicFit.Infrastructure.Persistence.Entities;
using LogicFit.Shared;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LogicFit.Infrastructure.Members;

public sealed class MembersService(
    IGymDatabaseResolver databaseResolver,
    IGymDbContextFactory gymDbContextFactory,
    IAuthenticationService authentication,
    IAuthRepository auditRepository,
    ILogger<MembersService> logger) : IMembersService
{
    public async Task<AuthResult<MemberPageDto>> ListAsync(
        AuthenticatedUser currentUser,
        Guid gymId,
        MemberListQuery query,
        AuthRequestContext context,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAndResolveAsync<MemberPageDto>(
            currentUser,
            gymId,
            MembersContract.ReadPermission,
            context,
            cancellationToken);
        if (authorization.Failure is not null)
        {
            return authorization.Failure;
        }

        if (!IsValidPage(query.Page, query.PageSize)
            || query.Statuses.Count == 0
            || query.Statuses.Any(status => !MemberStatuses.All.Contains(status))
            || query.SortField is not ("createdAt" or "updatedAt"))
        {
            return Failure<MemberPageDto>(400, "INVALID_FILTER", "The requested Member filters are not valid.");
        }

        try
        {
            await using var db = gymDbContextFactory.Create(authorization.Route!.DatabaseName);
            var statuses = query.Statuses.ToArray();
            IQueryable<MemberEntity> members = db.Members
                .AsNoTracking()
                .Where(member => member.GymId == gymId && statuses.Contains(member.Status));

            var search = query.Search?.Trim();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var phoneSearch = NormalizePhone(search);
                members = members.Where(member =>
                    member.MemberCode.Contains(search)
                    || member.FullName.Contains(search)
                    || (phoneSearch.Length > 0 && member.Phone.Contains(phoneSearch))
                    || (member.Email != null && member.Email.Contains(search)));
            }

            members = query.SortField == "updatedAt"
                ? query.SortDescending
                    ? members.OrderByDescending(member => member.UpdatedAtUtc).ThenByDescending(member => member.MemberId)
                    : members.OrderBy(member => member.UpdatedAtUtc).ThenByDescending(member => member.MemberId)
                : query.SortDescending
                    ? members.OrderByDescending(member => member.CreatedAtUtc).ThenByDescending(member => member.MemberId)
                    : members.OrderBy(member => member.CreatedAtUtc).ThenByDescending(member => member.MemberId);

            var total = await members.CountAsync(cancellationToken);
            var skip = (long)(query.Page - 1) * query.PageSize;
            if (skip > int.MaxValue)
            {
                return Failure<MemberPageDto>(400, "INVALID_FILTER", "The requested Member page is not valid.");
            }

            var items = await members
                .Skip((int)skip)
                .Take(query.PageSize)
                .Select(member => ToSummary(member))
                .ToListAsync(cancellationToken);

            return AuthResult<MemberPageDto>.Success(new MemberPageDto(
                items,
                query.Page,
                query.PageSize,
                total,
                skip + items.Count < total));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Member list failed for Gym {GymId}.", gymId);
            return Unavailable<MemberPageDto>();
        }
    }

    public async Task<AuthResult<MemberDetailDto>> CreateAsync(
        AuthenticatedUser currentUser,
        Guid gymId,
        CreateMemberCommand command,
        string? idempotencyKey,
        AuthRequestContext context,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAndResolveAsync<MemberDetailDto>(
            currentUser,
            gymId,
            MembersContract.CreatePermission,
            context,
            cancellationToken);
        if (authorization.Failure is not null)
        {
            return authorization.Failure;
        }

        var validation = ValidateCreate(command, idempotencyKey);
        if (validation.Count > 0)
        {
            return Failure<MemberDetailDto>(400, "VALIDATION_ERROR", "The Member could not be created.", validation);
        }

        var normalized = NormalizeCreate(command);
        var key = idempotencyKey!.Trim();
        var keyHash = HashOpaque($"member-create:{currentUser.UserId:N}:{gymId:N}:{key}");
        var fingerprint = HashOpaque(string.Join("|",
            normalized.FullName,
            normalized.Phone,
            normalized.Email ?? string.Empty,
            normalized.RegistrationDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            normalized.Notes ?? string.Empty));

        try
        {
            await using var db = gymDbContextFactory.Create(authorization.Route!.DatabaseName);
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            var existing = await db.Members
                .SingleOrDefaultAsync(member => member.GymId == gymId && member.CreateIdempotencyKeyHash == keyHash, cancellationToken);
            if (existing is not null)
            {
                if (!string.Equals(existing.CreateRequestFingerprint, fingerprint, StringComparison.Ordinal))
                {
                    return Failure<MemberDetailDto>(409, "DUPLICATE_RESOURCE", "The idempotency key was already used for a different Member request.");
                }

                return AuthResult<MemberDetailDto>.Success(ToDetail(existing), 201);
            }

            var now = DateTime.UtcNow;
            var entity = new MemberEntity
            {
                MemberId = Guid.NewGuid(),
                GymId = gymId,
                MemberCode = await GenerateMemberCodeAsync(db, gymId, cancellationToken),
                FullName = normalized.FullName,
                Phone = normalized.Phone,
                Email = normalized.Email,
                RegistrationDate = normalized.RegistrationDate,
                Notes = normalized.Notes,
                Status = MemberStatuses.Active,
                CreateIdempotencyKeyHash = keyHash,
                CreateRequestFingerprint = fingerprint,
                CreatedByUserId = currentUser.UserId,
                UpdatedByUserId = currentUser.UserId,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            db.Members.Add(entity);
            db.MemberTimelineEvents.Add(CreateTimelineEvent(
                entity.MemberId,
                currentUser.UserId,
                "MEMBER_CREATED",
                now,
                "Member created.",
                new Dictionary<string, string?> { ["status"] = MemberStatuses.Active }));

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            await WriteAuditSafelyAsync(new AuditEntry(
                context.RequestId,
                currentUser.UserId,
                "member",
                entity.MemberId,
                "MEMBER_CREATED",
                "success",
                null,
                SafeMetadata(("gymId", gymId.ToString("D")), ("status", MemberStatuses.Active)),
                "gym",
                gymId),
                cancellationToken);

            return AuthResult<MemberDetailDto>.Success(ToDetail(entity), 201);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            logger.LogWarning("A duplicate Member create was detected for Gym {GymId}; no request payload was logged.", gymId);
            await using var recoveryDb = gymDbContextFactory.Create(authorization.Route!.DatabaseName);
            var committed = await recoveryDb.Members
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    member => member.GymId == gymId && member.CreateIdempotencyKeyHash == keyHash,
                    cancellationToken);
            if (committed is not null
                && string.Equals(committed.CreateRequestFingerprint, fingerprint, StringComparison.Ordinal))
            {
                return AuthResult<MemberDetailDto>.Success(ToDetail(committed), 201);
            }

            return Failure<MemberDetailDto>(409, "DUPLICATE_RESOURCE", "The Member request conflicts with an existing resource.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Member creation failed for Gym {GymId}.", gymId);
            return Unavailable<MemberDetailDto>();
        }
    }

    public async Task<AuthResult<MemberDetailDto>> GetAsync(
        AuthenticatedUser currentUser,
        Guid gymId,
        Guid memberId,
        AuthRequestContext context,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAndResolveAsync<MemberDetailDto>(
            currentUser,
            gymId,
            MembersContract.ReadPermission,
            context,
            cancellationToken);
        if (authorization.Failure is not null)
        {
            return authorization.Failure;
        }

        try
        {
            await using var db = gymDbContextFactory.Create(authorization.Route!.DatabaseName);
            var member = await db.Members.AsNoTracking()
                .SingleOrDefaultAsync(item => item.GymId == gymId && item.MemberId == memberId, cancellationToken);
            return member is null
                ? NotFound<MemberDetailDto>()
                : AuthResult<MemberDetailDto>.Success(ToDetail(member));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Member detail failed for Gym {GymId} and Member {MemberId}.", gymId, memberId);
            return Unavailable<MemberDetailDto>();
        }
    }

    public async Task<AuthResult<MemberDetailDto>> UpdateAsync(
        AuthenticatedUser currentUser,
        Guid gymId,
        Guid memberId,
        UpdateMemberCommand command,
        byte[] expectedVersion,
        AuthRequestContext context,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAndResolveAsync<MemberDetailDto>(
            currentUser,
            gymId,
            MembersContract.UpdatePermission,
            context,
            cancellationToken);
        if (authorization.Failure is not null)
        {
            return authorization.Failure;
        }

        var validation = ValidateUpdate(command, expectedVersion);
        if (validation.Count > 0)
        {
            return Failure<MemberDetailDto>(400, "VALIDATION_ERROR", "The Member could not be updated.", validation);
        }

        var normalized = NormalizeUpdate(command);
        try
        {
            await using var db = gymDbContextFactory.Create(authorization.Route!.DatabaseName);
            var member = await db.Members
                .SingleOrDefaultAsync(item => item.GymId == gymId && item.MemberId == memberId, cancellationToken);
            if (member is null)
            {
                return NotFound<MemberDetailDto>();
            }

            if (member.Status == MemberStatuses.Archived)
            {
                return Failure<MemberDetailDto>(422, "DOMAIN_RULE_VIOLATION", "Archived Members cannot be updated.");
            }

            if (!VersionsEqual(member.RowVersion, expectedVersion))
            {
                return ConcurrencyFailure<MemberDetailDto>();
            }

            var previousStatus = member.Status;
            var profile = ToProfile(member).Update(
                normalized.FullName,
                normalized.Phone,
                normalized.Email,
                normalized.RegistrationDate,
                normalized.Notes,
                normalized.Status);
            var now = DateTime.UtcNow;
            ApplyProfile(member, profile);
            member.UpdatedByUserId = currentUser.UserId;
            member.UpdatedAtUtc = now;
            db.Entry(member).Property(item => item.RowVersion).OriginalValue = expectedVersion;

            db.MemberTimelineEvents.Add(CreateTimelineEvent(
                member.MemberId,
                currentUser.UserId,
                "MEMBER_UPDATED",
                now,
                "Member profile updated.",
                new Dictionary<string, string?> { ["changedFields"] = "fullName,phone,email,registrationDate,notes,status" }));
            if (!string.Equals(previousStatus, member.Status, StringComparison.Ordinal))
            {
                db.MemberTimelineEvents.Add(CreateTimelineEvent(
                    member.MemberId,
                    currentUser.UserId,
                    "MEMBER_STATUS_CHANGED",
                    now,
                    "Member status changed.",
                    new Dictionary<string, string?>
                    {
                        ["from"] = previousStatus,
                        ["to"] = member.Status
                    }));
            }

            await db.SaveChangesAsync(cancellationToken);

            await WriteAuditSafelyAsync(new AuditEntry(
                context.RequestId,
                currentUser.UserId,
                "member",
                member.MemberId,
                "MEMBER_UPDATED",
                "success",
                null,
                SafeMetadata(("gymId", gymId.ToString("D")), ("changedFields", "fullName,phone,email,registrationDate,notes,status")),
                "gym",
                gymId),
                cancellationToken);
            if (!string.Equals(previousStatus, member.Status, StringComparison.Ordinal))
            {
                await WriteAuditSafelyAsync(new AuditEntry(
                    context.RequestId,
                    currentUser.UserId,
                    "member",
                    member.MemberId,
                    "MEMBER_STATUS_CHANGED",
                    "success",
                    null,
                    SafeMetadata(("gymId", gymId.ToString("D")), ("from", previousStatus), ("to", member.Status)),
                    "gym",
                    gymId),
                    cancellationToken);
            }

            return AuthResult<MemberDetailDto>.Success(ToDetail(member));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            return ConcurrencyFailure<MemberDetailDto>();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Member update failed for Gym {GymId} and Member {MemberId}.", gymId, memberId);
            return Unavailable<MemberDetailDto>();
        }
    }

    public async Task<AuthResult<MemberArchiveDto>> ArchiveAsync(
        AuthenticatedUser currentUser,
        Guid gymId,
        Guid memberId,
        byte[]? expectedVersion,
        AuthRequestContext context,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAndResolveAsync<MemberArchiveDto>(
            currentUser,
            gymId,
            MembersContract.ArchivePermission,
            context,
            cancellationToken);
        if (authorization.Failure is not null)
        {
            return authorization.Failure;
        }

        try
        {
            await using var db = gymDbContextFactory.Create(authorization.Route!.DatabaseName);
            var member = await db.Members
                .SingleOrDefaultAsync(item => item.GymId == gymId && item.MemberId == memberId, cancellationToken);
            if (member is null)
            {
                return NotFound<MemberArchiveDto>();
            }

            if (member.Status == MemberStatuses.Archived)
            {
                return AuthResult<MemberArchiveDto>.Success(ToArchive(member));
            }

            if (expectedVersion is null || expectedVersion.Length != 8)
            {
                return Failure<MemberArchiveDto>(400, "CONCURRENCY_VERSION_REQUIRED", "If-Match is required to archive a current Member.");
            }

            if (!VersionsEqual(member.RowVersion, expectedVersion))
            {
                return ConcurrencyFailure<MemberArchiveDto>();
            }

            var now = DateTime.UtcNow;
            member.Status = MemberStatuses.Archived;
            member.UpdatedByUserId = currentUser.UserId;
            member.UpdatedAtUtc = now;
            db.Entry(member).Property(item => item.RowVersion).OriginalValue = expectedVersion;
            db.MemberTimelineEvents.Add(CreateTimelineEvent(
                member.MemberId,
                currentUser.UserId,
                "MEMBER_ARCHIVED",
                now,
                "Member archived.",
                new Dictionary<string, string?> { ["status"] = MemberStatuses.Archived }));

            await db.SaveChangesAsync(cancellationToken);

            await WriteAuditSafelyAsync(new AuditEntry(
                context.RequestId,
                currentUser.UserId,
                "member",
                member.MemberId,
                "MEMBER_ARCHIVED",
                "success",
                null,
                SafeMetadata(("gymId", gymId.ToString("D")), ("status", MemberStatuses.Archived)),
                "gym",
                gymId),
                cancellationToken);

            return AuthResult<MemberArchiveDto>.Success(ToArchive(member));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            return ConcurrencyFailure<MemberArchiveDto>();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Member archive failed for Gym {GymId} and Member {MemberId}.", gymId, memberId);
            return Unavailable<MemberArchiveDto>();
        }
    }

    public async Task<AuthResult<MemberTimelinePageDto>> TimelineAsync(
        AuthenticatedUser currentUser,
        Guid gymId,
        Guid memberId,
        int page,
        int pageSize,
        AuthRequestContext context,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAndResolveAsync<MemberTimelinePageDto>(
            currentUser,
            gymId,
            MembersContract.ReadPermission,
            context,
            cancellationToken);
        if (authorization.Failure is not null)
        {
            return authorization.Failure;
        }

        if (!IsValidPage(page, pageSize))
        {
            return Failure<MemberTimelinePageDto>(400, "INVALID_FILTER", "The requested timeline page is not valid.");
        }

        try
        {
            await using var db = gymDbContextFactory.Create(authorization.Route!.DatabaseName);
            var memberExists = await db.Members.AsNoTracking()
                .AnyAsync(member => member.GymId == gymId && member.MemberId == memberId, cancellationToken);
            if (!memberExists)
            {
                return NotFound<MemberTimelinePageDto>();
            }

            var events = db.MemberTimelineEvents.AsNoTracking()
                .Where(item => item.MemberId == memberId && MembersContract.TimelineEventTypes.Contains(item.EventType))
                .OrderByDescending(item => item.EventAtUtc)
                .ThenByDescending(item => item.TimelineEventId);
            var total = await events.CountAsync(cancellationToken);
            var skip = (long)(page - 1) * pageSize;
            if (skip > int.MaxValue)
            {
                return Failure<MemberTimelinePageDto>(400, "INVALID_FILTER", "The requested timeline page is not valid.");
            }

            var items = await events
                .Skip((int)skip)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
            var result = items.Select(item => new MemberTimelineItemDto(
                item.TimelineEventId,
                item.MemberId,
                gymId,
                item.EventType,
                item.EventAtUtc,
                item.ActorUserId,
                ReadSafeMetadata(item.MetadataJson))).ToArray();

            return AuthResult<MemberTimelinePageDto>.Success(new MemberTimelinePageDto(
                result,
                page,
                pageSize,
                total,
                skip + result.Length < total));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Member timeline failed for Gym {GymId} and Member {MemberId}.", gymId, memberId);
            return Unavailable<MemberTimelinePageDto>();
        }
    }

    private async Task<(GymDatabaseRoute? Route, AuthResult<T>? Failure)> AuthorizeAndResolveAsync<T>(
        AuthenticatedUser currentUser,
        Guid gymId,
        string permission,
        AuthRequestContext context,
        CancellationToken cancellationToken)
    {
        bool permitted;
        try
        {
            permitted = await authentication.HasPermissionAsync(
                currentUser,
                permission,
                gymId,
                allowPlatformControlPlane: false,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Member authorization dependency failed for Gym {GymId}.", gymId);
            return (null, Unavailable<T>());
        }

        if (!permitted)
        {
            var scopeDenied = currentUser.GymId.HasValue
                && currentUser.GymId.Value != gymId
                && currentUser.Permissions.Contains(permission);
            await WriteAuditSafelyAsync(new AuditEntry(
                context.RequestId,
                currentUser.UserId,
                "iam.authorization",
                gymId,
                scopeDenied ? "authz.gym_scope_denied" : "authz.permission_denied",
                "failure",
                permission,
                SafeMetadata(("permission", permission), ("gymId", gymId.ToString("D"))),
                "gym",
                gymId),
                cancellationToken);
            return (null, Failure<T>(403, scopeDenied ? "GYM_SCOPE_DENIED" : "PERMISSION_DENIED", "The authenticated user is not authorized for this Member operation."));
        }

        try
        {
            var route = await databaseResolver.ResolveAsync(gymId, cancellationToken);
            return route is null
                ? (null, NotFound<T>())
                : (route, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Gym database resolution failed for Gym {GymId}.", gymId);
            return (null, Unavailable<T>());
        }
    }

    private static IReadOnlyList<ApiFieldError> ValidateCreate(CreateMemberCommand command, string? idempotencyKey)
    {
        var errors = new List<ApiFieldError>();
        AddProfileErrors(errors, command.FullName, command.Phone, command.Email, command.RegistrationDate, command.Notes);
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Trim().Length > 128)
        {
            errors.Add(new ApiFieldError("Idempotency-Key", "required_or_too_long"));
        }

        return errors;
    }

    private static IReadOnlyList<ApiFieldError> ValidateUpdate(UpdateMemberCommand command, byte[] expectedVersion)
    {
        var errors = new List<ApiFieldError>();
        AddProfileErrors(errors, command.FullName, command.Phone, command.Email, command.RegistrationDate, command.Notes);
        if (command.Status is not (MemberStatuses.Active or MemberStatuses.Inactive))
        {
            errors.Add(new ApiFieldError("status", "invalid_state"));
        }

        if (expectedVersion.Length != 8)
        {
            errors.Add(new ApiFieldError("If-Match", "invalid_version"));
        }

        return errors;
    }

    private static void AddProfileErrors(
        ICollection<ApiFieldError> errors,
        string? fullName,
        string? phone,
        string? email,
        DateOnly? registrationDate,
        string? notes)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            errors.Add(new ApiFieldError("fullName", "required"));
        }
        else if (fullName.Trim().Length > 120)
        {
            errors.Add(new ApiFieldError("fullName", "max_length"));
        }

        var normalizedPhone = NormalizePhone(phone);
        if (normalizedPhone.Length < 5 || normalizedPhone.Length > 30 || !IsValidPhone(normalizedPhone))
        {
            errors.Add(new ApiFieldError("phone", "invalid_format"));
        }

        var normalizedEmail = NormalizeEmail(email);
        if (normalizedEmail.Length > 254 || (normalizedEmail.Length > 0 && !IsValidEmail(normalizedEmail)))
        {
            errors.Add(new ApiFieldError("email", "invalid_format"));
        }

        if (!registrationDate.HasValue)
        {
            errors.Add(new ApiFieldError("registrationDate", "required"));
        }

        if (!string.IsNullOrWhiteSpace(notes) && notes.Trim().Length > 1000)
        {
            errors.Add(new ApiFieldError("notes", "max_length"));
        }
    }

    private static (string FullName, string Phone, string? Email, DateOnly RegistrationDate, string? Notes) NormalizeCreate(CreateMemberCommand command)
        => (
            command.FullName!.Trim(),
            NormalizePhone(command.Phone),
            NormalizeEmail(command.Email) is { Length: > 0 } email ? email : null,
            command.RegistrationDate!.Value,
            NormalizeNotes(command.Notes));

    private static (string FullName, string Phone, string? Email, DateOnly RegistrationDate, string? Notes, string Status) NormalizeUpdate(UpdateMemberCommand command)
        => (
            command.FullName!.Trim(),
            NormalizePhone(command.Phone),
            NormalizeEmail(command.Email) is { Length: > 0 } email ? email : null,
            command.RegistrationDate!.Value,
            NormalizeNotes(command.Notes),
            command.Status!);

    private static string? NormalizeNotes(string? notes)
        => string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

    private static string NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return string.Empty;
        }

        var trimmed = phone.Trim();
        var builder = new StringBuilder(trimmed.Length);
        for (var index = 0; index < trimmed.Length; index++)
        {
            var character = trimmed[index];
            if (char.IsDigit(character) || (character == '+' && builder.Length == 0))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    private static string NormalizeEmail(string? email) => email?.Trim().ToLowerInvariant() ?? string.Empty;

    private static bool IsValidPhone(string phone)
        => phone.Length > 0
            && (phone[0] == '+' ? phone.Length > 1 && phone[1..].All(char.IsDigit) : phone.All(char.IsDigit));

    private static bool IsValidEmail(string email)
    {
        var at = email.IndexOf('@');
        return at > 0
            && at == email.LastIndexOf('@')
            && at < email.Length - 1
            && !email.Contains(' ')
            && email[(at + 1)..].Contains('.');
    }

    private static async Task<string> GenerateMemberCodeAsync(GymDbContext db, Guid gymId, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var code = $"LF-{Convert.ToHexString(RandomNumberGenerator.GetBytes(8))}";
            if (!await db.Members.AnyAsync(member => member.GymId == gymId && member.MemberCode == code, cancellationToken))
            {
                return code;
            }
        }

        throw new InvalidOperationException("A unique Member Code could not be generated.");
    }

    private static MemberProfile ToProfile(MemberEntity member)
        => new(member.MemberId, member.GymId, member.MemberCode, member.FullName, member.Phone, member.Email, member.RegistrationDate, member.Notes, member.Status);

    private static void ApplyProfile(MemberEntity entity, MemberProfile profile)
    {
        entity.FullName = profile.FullName;
        entity.Phone = profile.Phone;
        entity.Email = profile.Email;
        entity.RegistrationDate = profile.RegistrationDate;
        entity.Notes = profile.Notes;
        entity.Status = profile.Status;
    }

    private static MemberSummaryDto ToSummary(MemberEntity member)
        => new(member.MemberId, member.MemberCode, member.FullName, member.Phone, member.Email, member.RegistrationDate, member.Status, member.CreatedAtUtc, member.UpdatedAtUtc, EncodeVersion(member.RowVersion));

    private static MemberDetailDto ToDetail(MemberEntity member)
        => new(member.MemberId, member.GymId, member.MemberCode, member.FullName, member.Phone, member.Email, member.RegistrationDate, member.Notes, member.Status, member.CreatedAtUtc, member.UpdatedAtUtc, EncodeVersion(member.RowVersion));

    private static MemberArchiveDto ToArchive(MemberEntity member)
        => new(member.MemberId, member.Status, member.UpdatedAtUtc, EncodeVersion(member.RowVersion));

    private static MemberTimelineEventEntity CreateTimelineEvent(
        Guid memberId,
        Guid actorUserId,
        string eventType,
        DateTime eventAtUtc,
        string summary,
        IReadOnlyDictionary<string, string?> metadata)
        => new()
        {
            TimelineEventId = Guid.NewGuid(),
            MemberId = memberId,
            ActorUserId = actorUserId,
            EventType = eventType,
            EventAtUtc = eventAtUtc,
            SourceType = "member",
            SourceId = memberId,
            Summary = summary,
            MetadataJson = JsonSerializer.Serialize(metadata),
            CreatedAtUtc = eventAtUtc
        };

    private static IReadOnlyDictionary<string, string?> ReadSafeMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return new Dictionary<string, string?>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string?>>(metadataJson)
                ?? new Dictionary<string, string?>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, string?>();
        }
    }

    private async Task WriteAuditSafelyAsync(AuditEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            await auditRepository.WriteAuditAsync(entry, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Member audit write failed for action {Action} and target {TargetId}.", entry.Action, entry.TargetId);
        }
    }

    private static bool IsValidPage(int page, int pageSize)
        => page >= 1 && pageSize >= 1 && pageSize <= MembersContract.MaximumPageSize;

    private static bool VersionsEqual(byte[]? actual, byte[] expected)
        => actual is { Length: 8 } && expected.Length == 8 && CryptographicOperations.FixedTimeEquals(actual, expected);

    private static string EncodeVersion(byte[]? version)
        => Convert.ToBase64String(version is { Length: > 0 } ? version : [0]);

    private static string HashOpaque(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static string SafeMetadata(params (string Key, string Value)[] values)
        => JsonSerializer.Serialize(values.ToDictionary(value => value.Key, value => (string?)value.Value, StringComparer.Ordinal));

    private static bool IsUniqueViolation(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException sql && sql.Number is 2601 or 2627)
            {
                return true;
            }
        }

        return false;
    }

    private static AuthResult<T> NotFound<T>()
        => Failure<T>(404, "RESOURCE_NOT_FOUND", "The requested Member resource was not found.");

    private static AuthResult<T> Unavailable<T>()
        => Failure<T>(503, "DEPENDENCY_UNAVAILABLE", "The Gym Member store is temporarily unavailable.");

    private static AuthResult<T> ConcurrencyFailure<T>()
        => Failure<T>(409, "CONCURRENCY_CONFLICT", "The Member changed before this operation could be completed.");

    private static AuthResult<T> Failure<T>(int statusCode, string code, string message, IReadOnlyList<ApiFieldError>? fields = null)
        => AuthResult<T>.Failure(statusCode, code, message, fields);
}
