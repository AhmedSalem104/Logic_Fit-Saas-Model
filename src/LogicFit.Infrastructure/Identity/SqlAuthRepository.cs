using System.Data;
using System.Security.Cryptography;
using System.Text;
using LogicFit.Application;
using LogicFit.Infrastructure.Persistence;
using LogicFit.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Infrastructure.Identity;

/// <summary>
/// Control Plane persistence for the Phase 5B authentication boundary.
/// The repository deliberately exposes only safe projections to the API layer.
/// </summary>
public sealed class SqlAuthRepository(ControlPlaneDbContext db) : IAuthRepository
{
    public async Task<AuthUserRecord?> FindUserByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        var entity = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
        return entity is null ? null : MapUser(entity);
    }

    public async Task<AuthUserRecord?> FindUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var entity = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        return entity is null ? null : MapUser(entity);
    }

    public async Task<AuthCredentialRecord?> FindPasswordCredentialAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var entity = await db.Credentials.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId && x.CredentialType == "password", cancellationToken);
        return entity is null ? null : new AuthCredentialRecord(entity.CredentialId, entity.UserId, entity.SecretHash, entity.SecretVersion);
    }

    public async Task<AuthMfaRecord?> FindEnabledMfaAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var entity = await db.MfaFactors.AsNoTracking()
            .Where(x => x.UserId == userId && x.FactorType == "totp" && x.Status == "active")
            .OrderByDescending(x => x.VerifiedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        return entity is null ? null : MapMfa(entity);
    }

    public async Task<AuthMfaRecord?> FindMfaAsync(Guid userId, Guid factorId, CancellationToken cancellationToken = default)
    {
        var entity = await db.MfaFactors.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.MfaFactorId == factorId, cancellationToken);
        return entity is null ? null : MapMfa(entity);
    }

    public async Task<IReadOnlyList<AuthAssignmentRecord>> GetAssignmentsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var entities = await db.UserGymRoles.AsNoTracking()
            .Include(x => x.Role)
                .ThenInclude(x => x!.RolePermissions)
                    .ThenInclude(x => x.Permission)
            .Where(x => x.UserId == userId && x.Role != null && x.Role.Status == "active")
            .OrderBy(x => x.ScopeType)
            .ThenBy(x => x.GymId)
            .ToListAsync(cancellationToken);

        return entities.Select(entity => new AuthAssignmentRecord(
            entity.AssignmentId,
            entity.UserId,
            entity.GymId,
            entity.RoleId,
            entity.ScopeType,
            entity.Role?.Name ?? string.Empty,
            entity.Status,
            entity.RowVersion,
            entity.Role?.RolePermissions
                .Where(x => x.Permission != null)
                .Select(x => x.Permission!.PermissionKey)
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
                ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase))).ToArray();
    }

    public async Task UpdateLastLoginAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        await db.Users.Where(x => x.UserId == userId).ExecuteUpdateAsync(setters => setters
            .SetProperty(x => x.LastLoginAtUtc, nowUtc)
            .SetProperty(x => x.UpdatedAtUtc, nowUtc), cancellationToken);
    }

    public async Task WriteAuditAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        db.AuditEvents.Add(new AuditEventEntity
        {
            AuditEventId = Guid.NewGuid(),
            RequestId = Truncate(entry.RequestId, 80),
            ActorUserId = entry.ActorUserId,
            TargetType = Truncate(entry.TargetType, 120) ?? "unknown",
            TargetId = entry.TargetId,
            Action = Truncate(entry.Action, 120) ?? "unknown",
            Result = Truncate(entry.Result, 30) ?? "unknown",
            Reason = Truncate(entry.Reason, 500),
            MetadataJson = Truncate(entry.MetadataJson, 4000),
            OccurredAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ChangePasswordAndRevokeSessionsAsync(Guid userId, string passwordHash, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var credential = await db.Credentials.FirstOrDefaultAsync(x => x.UserId == userId && x.CredentialType == "password", cancellationToken);
        var user = await db.Users.FirstOrDefaultAsync(x => x.UserId == userId && x.Status == "active", cancellationToken);
        if (credential is null || user is null)
        {
            return false;
        }

        credential.SecretHash = passwordHash;
        credential.SecretVersion = "lf-pbkdf2-sha256-v1";
        credential.LastRotatedAtUtc = nowUtc;
        credential.UpdatedAtUtc = nowUtc;
        user.UpdatedAtUtc = nowUtc;
        await RevokeAllTrackedAsync(userId, nowUtc, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task CreatePasswordResetTokenAsync(Guid userId, string tokenHash, DateTime expiresAtUtc, string? ipAddress, string requestId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await db.PasswordResetTokens
            .Where(x => x.UserId == userId && x.UsedAtUtc == null && x.RevokedAtUtc == null && x.ExpiresAtUtc > now)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.RevokedAtUtc, now), cancellationToken);

        db.PasswordResetTokens.Add(new PasswordResetTokenEntity
        {
            PasswordResetTokenId = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAtUtc = expiresAtUtc,
            RequestedIp = Truncate(ipAddress, 64),
            RequestId = Truncate(requestId, 80),
            CreatedAtUtc = now
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PasswordResetRecord?> FindPasswordResetTokenAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        var entity = await db.PasswordResetTokens.AsNoTracking().FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        return entity is null
            ? null
            : new PasswordResetRecord(entity.PasswordResetTokenId, entity.UserId, entity.ExpiresAtUtc, entity.UsedAtUtc != null, entity.RevokedAtUtc != null);
    }

    public async Task<bool> CompletePasswordResetAsync(Guid tokenId, Guid userId, string passwordHash, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var token = await db.PasswordResetTokens.FirstOrDefaultAsync(x => x.PasswordResetTokenId == tokenId && x.UserId == userId, cancellationToken);
        var credential = await db.Credentials.FirstOrDefaultAsync(x => x.UserId == userId && x.CredentialType == "password", cancellationToken);
        var user = await db.Users.FirstOrDefaultAsync(x => x.UserId == userId && x.Status == "active", cancellationToken);
        if (token is null || credential is null || user is null || token.UsedAtUtc != null || token.RevokedAtUtc != null || token.ExpiresAtUtc <= nowUtc)
        {
            return false;
        }

        token.UsedAtUtc = nowUtc;
        credential.SecretHash = passwordHash;
        credential.SecretVersion = "lf-pbkdf2-sha256-v1";
        credential.LastRotatedAtUtc = nowUtc;
        credential.UpdatedAtUtc = nowUtc;
        user.UpdatedAtUtc = nowUtc;
        await RevokeAllTrackedAsync(userId, nowUtc, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<AuthMfaRecord> CreatePendingMfaAsync(Guid userId, string protectedSecret, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        await db.MfaFactors.Where(x => x.UserId == userId && x.Status == "pending")
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, "disabled")
                .SetProperty(x => x.UpdatedAtUtc, nowUtc), cancellationToken);

        var entity = new MfaFactorEntity
        {
            MfaFactorId = Guid.NewGuid(),
            UserId = userId,
            FactorType = "totp",
            SecretRef = protectedSecret,
            Status = "pending",
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };
        db.MfaFactors.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return MapMfa(entity);
    }

    public async Task<bool> EnableMfaAsync(Guid userId, Guid factorId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var updated = await db.MfaFactors
            .Where(x => x.UserId == userId && x.MfaFactorId == factorId && x.FactorType == "totp" && x.Status == "pending")
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, "active")
                .SetProperty(x => x.VerifiedAtUtc, nowUtc)
                .SetProperty(x => x.UpdatedAtUtc, nowUtc), cancellationToken);
        return updated == 1;
    }

    public async Task<bool> DisableMfaAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var updated = await db.MfaFactors
            .Where(x => x.UserId == userId && x.FactorType == "totp" && x.Status == "active")
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, "disabled")
                .SetProperty(x => x.UpdatedAtUtc, nowUtc), cancellationToken);
        await db.MfaRecoveryCodes.Where(x => x.UserId == userId && x.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.RevokedAtUtc, nowUtc), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return updated > 0;
    }

    public async Task ReplaceRecoveryCodesAsync(Guid userId, Guid factorId, IReadOnlyList<string> codeHashes, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.MfaRecoveryCodes.Where(x => x.UserId == userId && x.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.RevokedAtUtc, nowUtc), cancellationToken);

        foreach (var codeHash in codeHashes.Distinct(StringComparer.Ordinal))
        {
            db.MfaRecoveryCodes.Add(new MfaRecoveryCodeEntity
            {
                RecoveryCodeId = Guid.NewGuid(),
                UserId = userId,
                MfaFactorId = factorId,
                CodeHash = codeHash,
                CreatedAtUtc = nowUtc
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<bool> ConsumeRecoveryCodeAsync(Guid userId, string codeHash, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var updated = await db.MfaRecoveryCodes
            .Where(x => x.UserId == userId && x.CodeHash == codeHash && x.UsedAtUtc == null && x.RevokedAtUtc == null
                && x.MfaFactorId != null
                && db.MfaFactors.Any(f => f.MfaFactorId == x.MfaFactorId && f.UserId == userId && f.Status == "active"))
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.UsedAtUtc, nowUtc), cancellationToken);
        return updated == 1;
    }

    public async Task<AccessCatalogRecord> GetAccessCatalogAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await db.Permissions.AsNoTracking().OrderBy(x => x.PermissionKey).ToListAsync(cancellationToken);
        var roles = await db.Roles.AsNoTracking()
            .Include(x => x.RolePermissions)
                .ThenInclude(x => x.Permission)
            .OrderBy(x => x.ScopeType)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var permissionDtos = permissions.Select(MapPermission).ToArray();
        var roleDtos = roles.Select(role => new AccessRoleDto(
            role.RoleId,
            role.ScopeType,
            role.Name,
            role.Status,
            role.RolePermissions.Where(x => x.Permission != null).Select(x => MapPermission(x.Permission!)).OrderBy(x => x.Key).ToArray())).ToArray();

        return new AccessCatalogRecord(permissionDtos, roleDtos, await db.RolePermissions.CountAsync(cancellationToken));
    }

    public async Task<(IReadOnlyList<AccessUserDto> Items, int Total)> ListAccessUsersAsync(AccessUsersQuery query, CancellationToken cancellationToken = default)
    {
        IQueryable<UserEntity> users = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            users = users.Where(x => x.Status == query.Status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            users = users.Where(x => x.Email.ToLower().Contains(search) || x.DisplayName.ToLower().Contains(search));
        }

        if (query.GymId.HasValue)
        {
            users = users.Where(x => x.GymRoles.Any(role => role.GymId == query.GymId && (query.ScopeType == null || role.ScopeType == query.ScopeType)));
        }
        else if (!string.IsNullOrWhiteSpace(query.ScopeType))
        {
            users = users.Where(x => x.GymRoles.Any(role => role.ScopeType == query.ScopeType));
        }

        var total = await users.CountAsync(cancellationToken);
        users = ApplyUserOrdering(users, query.SortField, query.SortDescending);
        var entities = await users
            .Include(x => x.GymRoles)
                .ThenInclude(x => x.Role)
            .AsSplitQuery()
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (entities.Select(entity => MapAccessUser(entity, query)).ToArray(), total);
    }

    public async Task<AccessUserDto?> CreateAccessUserAsync(string email, string displayName, string passwordHash, Guid roleId, Guid? gymId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        if (await db.Users.AnyAsync(x => x.Email == email, cancellationToken))
        {
            return null;
        }

        var role = await db.Roles.FirstOrDefaultAsync(x => x.RoleId == roleId && x.Status == "active", cancellationToken);
        if (role is null || (role.ScopeType == "gym" && !gymId.HasValue) || (role.ScopeType == "platform" && gymId.HasValue))
        {
            return null;
        }

        var user = new UserEntity
        {
            UserId = Guid.NewGuid(),
            Email = email,
            DisplayName = displayName,
            Status = "active",
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };
        user.Credentials.Add(new CredentialEntity
        {
            CredentialId = Guid.NewGuid(),
            UserId = user.UserId,
            CredentialType = "password",
            SecretHash = passwordHash,
            SecretVersion = "lf-pbkdf2-sha256-v1",
            LastRotatedAtUtc = nowUtc,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        });
        user.GymRoles.Add(new UserGymRoleEntity
        {
            AssignmentId = Guid.NewGuid(),
            UserId = user.UserId,
            GymId = gymId,
            RoleId = role.RoleId,
            ScopeType = role.ScopeType,
            Status = "active",
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        });
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var assignment = user.GymRoles.Single();
        return new AccessUserDto(
            user.UserId,
            user.Email,
            user.DisplayName,
            user.Status,
            user.CreatedAtUtc,
            user.UpdatedAtUtc,
            EncodeVersion(user.RowVersion),
            [new AccessRoleAssignmentDto(assignment.AssignmentId, user.UserId, role.RoleId, role.Name, assignment.GymId, assignment.ScopeType, assignment.Status, EncodeVersion(assignment.RowVersion))]);
    }

    public async Task<AccessStatusDto?> ChangeUserStatusAsync(Guid userId, string status, byte[]? expectedVersion, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var user = await db.Users.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var changed = !string.Equals(user.Status, status, StringComparison.OrdinalIgnoreCase);
        if (changed && (expectedVersion is null || !user.RowVersion.SequenceEqual(expectedVersion)))
        {
            return null;
        }

        if (changed)
        {
            user.Status = status;
            user.UpdatedAtUtc = nowUtc;
            if (status == "disabled")
            {
                await RevokeAllTrackedAsync(userId, nowUtc, cancellationToken);
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        await db.Entry(user).ReloadAsync(cancellationToken);
        return new AccessStatusDto(user.UserId, user.Status, changed && status == "disabled", EncodeVersion(user.RowVersion));
    }

    public async Task<(AccessAssignmentDto? Assignment, string Outcome, bool VersionConflict)> EnsureRoleAssignmentAsync(Guid userId, Guid roleId, Guid? gymId, byte[]? expectedVersion, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var user = await db.Users.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        var role = await db.Roles.FirstOrDefaultAsync(x => x.RoleId == roleId, cancellationToken);
        if (user is null || role is null || role.Status != "active")
        {
            return (null, "not_found", false);
        }

        var assignment = await db.UserGymRoles
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.RoleId == roleId && x.GymId == gymId && x.ScopeType == role.ScopeType, cancellationToken);
        if (assignment is not null && assignment.Status == "active")
        {
            return (MapAssignment(assignment), "existing", false);
        }

        if (assignment is not null)
        {
            if (expectedVersion is null || !assignment.RowVersion.SequenceEqual(expectedVersion))
            {
                return (null, "concurrency", true);
            }

            assignment.Status = "active";
            assignment.UpdatedAtUtc = nowUtc;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return (MapAssignment(assignment), "reactivated", false);
        }

        assignment = new UserGymRoleEntity
        {
            AssignmentId = Guid.NewGuid(),
            UserId = userId,
            GymId = gymId,
            RoleId = roleId,
            ScopeType = role.ScopeType,
            Status = "active",
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };
        db.UserGymRoles.Add(assignment);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (MapAssignment(assignment), "created", false);
    }

    public async Task<(bool Found, bool Changed, bool VersionConflict)> RevokeRoleAssignmentAsync(Guid userId, Guid assignmentId, byte[]? expectedVersion, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var assignment = await db.UserGymRoles.FirstOrDefaultAsync(x => x.AssignmentId == assignmentId && x.UserId == userId, cancellationToken);
        if (assignment is null)
        {
            return (false, false, false);
        }

        if (assignment.Status != "active")
        {
            return (true, false, false);
        }

        if (expectedVersion is null || !assignment.RowVersion.SequenceEqual(expectedVersion))
        {
            return (true, false, true);
        }

        assignment.Status = "revoked";
        assignment.UpdatedAtUtc = nowUtc;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (true, true, false);
    }

    public Task<bool> IsGymActiveAsync(Guid gymId, CancellationToken cancellationToken = default)
        => db.Gyms.AnyAsync(x => x.GymId == gymId && (x.Status == "active" || x.Status == "ready"), cancellationToken);

    private async Task RevokeAllTrackedAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken)
    {
        await db.Sessions.Where(x => x.UserId == userId && x.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.RevokedAtUtc, nowUtc)
                .SetProperty(x => x.UpdatedAtUtc, nowUtc), cancellationToken);
    }

    private static IQueryable<UserEntity> ApplyUserOrdering(IQueryable<UserEntity> users, string? sortField, bool descending)
    {
        return (sortField?.Trim().ToLowerInvariant(), descending) switch
        {
            ("email", false) => users.OrderBy(x => x.Email),
            ("email", true) => users.OrderByDescending(x => x.Email),
            ("updatedatutc", false) => users.OrderBy(x => x.UpdatedAtUtc),
            ("updatedatutc", true) => users.OrderByDescending(x => x.UpdatedAtUtc),
            (_, false) => users.OrderBy(x => x.CreatedAtUtc),
            (_, true) => users.OrderByDescending(x => x.CreatedAtUtc)
        };
    }

    private static AuthUserRecord MapUser(UserEntity entity)
        => new(entity.UserId, entity.Email, entity.DisplayName, entity.Status, entity.LastLoginAtUtc, entity.CreatedAtUtc, entity.UpdatedAtUtc, entity.RowVersion);

    private static AuthMfaRecord MapMfa(MfaFactorEntity entity)
        => new(entity.MfaFactorId, entity.UserId, entity.FactorType, entity.SecretRef, entity.Status, entity.VerifiedAtUtc);

    private static AccessPermissionDto MapPermission(PermissionEntity entity)
        => new(entity.PermissionId, entity.PermissionKey, entity.Domain, entity.Action, entity.RiskLevel, entity.Description);

    private static AccessUserDto MapAccessUser(UserEntity entity, AccessUsersQuery query)
    {
        var assignments = entity.GymRoles
            .Where(x => (query.GymId == null || x.GymId == query.GymId)
                && (query.ScopeType == null || x.ScopeType == query.ScopeType))
            .OrderBy(x => x.ScopeType)
            .ThenBy(x => x.GymId)
            .Select(x => new AccessRoleAssignmentDto(
                x.AssignmentId,
                entity.UserId,
                x.RoleId,
                x.Role?.Name ?? string.Empty,
                x.GymId,
                x.ScopeType,
                x.Status,
                EncodeVersion(x.RowVersion)))
            .ToArray();

        return new AccessUserDto(entity.UserId, entity.Email, entity.DisplayName, entity.Status, entity.CreatedAtUtc, entity.UpdatedAtUtc, EncodeVersion(entity.RowVersion), assignments);
    }

    private static AccessAssignmentDto MapAssignment(UserGymRoleEntity entity)
        => new(entity.AssignmentId, entity.UserId, entity.RoleId, entity.Role?.Name ?? string.Empty, entity.GymId, entity.ScopeType, entity.Status, EncodeVersion(entity.RowVersion));

    private static string EncodeVersion(byte[]? version)
        => Convert.ToBase64String(version is { Length: > 0 } ? version : [0]);

    private static string? Truncate(string? value, int maxLength)
        => string.IsNullOrWhiteSpace(value) ? null : value[..Math.Min(value.Length, maxLength)];
}
