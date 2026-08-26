using System.Security.Cryptography;
using System.Text;
using LogicFit.Application;
using LogicFit.Domain.ValueObjects;
using LogicFit.Infrastructure.Persistence;
using LogicFit.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Infrastructure.Security;

public sealed class SqlSessionStore(
    ControlPlaneDbContext db,
    SessionPolicy policy) : ISessionStore
{
    public async Task<SessionCreated> CreateAsync(
        Guid userId,
        Guid? gymId,
        bool mfaVerified,
        string? userAgent,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var rawToken = CreateRawToken();
        var absoluteExpiry = now.Add(policy.AbsoluteLifetime);
        var effectiveExpiry = mfaVerified
            ? absoluteExpiry
            : Min(now.Add(policy.MfaChallengeLifetime), absoluteExpiry);
        var entity = new SessionEntity
        {
            SessionId = Guid.NewGuid(),
            UserId = userId,
            GymId = gymId,
            TokenHash = HashToken(rawToken),
            ExpiresAtUtc = effectiveExpiry,
            IdleExpiresAtUtc = Min(now.Add(policy.IdleTimeout), effectiveExpiry),
            AbsoluteExpiresAtUtc = absoluteExpiry,
            SessionKind = mfaVerified ? "staff" : "mfa_pending",
            MfaVerified = mfaVerified,
            UserAgent = Truncate(userAgent, 512),
            IpAddress = Truncate(ipAddress, 64),
            LastSeenAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        db.Sessions.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return new SessionCreated(entity.SessionId, rawToken, entity.ExpiresAtUtc, entity.IdleExpiresAtUtc!.Value, entity.AbsoluteExpiresAtUtc!.Value);
    }

    public async Task<SessionRecord?> FindActiveAsync(string rawToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var hash = HashToken(rawToken);
        var entity = await db.Sessions.AsNoTracking().FirstOrDefaultAsync(x =>
            x.TokenHash == hash
            && x.RevokedAtUtc == null
            && x.ExpiresAtUtc > now
            && (x.IdleExpiresAtUtc == null || x.IdleExpiresAtUtc > now)
            && (x.AbsoluteExpiresAtUtc == null || x.AbsoluteExpiresAtUtc > now), cancellationToken);

        return entity is null ? null : ToRecord(entity);
    }

    public async Task<SessionRecord?> FindByIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var entity = await db.Sessions.AsNoTracking().FirstOrDefaultAsync(x =>
            x.SessionId == sessionId
            && x.RevokedAtUtc == null
            && x.ExpiresAtUtc > now
            && (x.IdleExpiresAtUtc == null || x.IdleExpiresAtUtc > now)
            && (x.AbsoluteExpiresAtUtc == null || x.AbsoluteExpiresAtUtc > now), cancellationToken);
        return entity is null ? null : ToRecord(entity);
    }

    public async Task<SessionRecord?> FindOwnedByIdAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var entity = await db.Sessions.AsNoTracking().FirstOrDefaultAsync(x =>
            x.SessionId == sessionId && x.UserId == userId, cancellationToken);
        return entity is null ? null : ToRecord(entity);
    }

    public async Task<IReadOnlyList<SessionRecord>> ListActiveForUserAsync(Guid userId, Guid? gymId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var entities = await db.Sessions.AsNoTracking()
            .Where(x => x.UserId == userId
                && x.GymId == gymId
                && x.RevokedAtUtc == null
                && x.ExpiresAtUtc > now
                && (x.IdleExpiresAtUtc == null || x.IdleExpiresAtUtc > now)
                && (x.AbsoluteExpiresAtUtc == null || x.AbsoluteExpiresAtUtc > now))
            .OrderByDescending(x => x.LastSeenAtUtc ?? x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        return entities.Select(ToRecord).ToArray();
    }

    public async Task TouchAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var entity = await db.Sessions.FirstOrDefaultAsync(x => x.SessionId == sessionId && x.RevokedAtUtc == null, cancellationToken);
        if (entity is null)
        {
            return;
        }

        var absolute = entity.AbsoluteExpiresAtUtc ?? entity.ExpiresAtUtc;
        var effectiveExpiry = entity.SessionKind == "mfa_pending" && !entity.MfaVerified
            ? entity.ExpiresAtUtc
            : absolute;
        entity.LastSeenAtUtc = now;
        entity.IdleExpiresAtUtc = Min(now.Add(policy.IdleTimeout), effectiveExpiry);
        entity.ExpiresAtUtc = effectiveExpiry;
        entity.UpdatedAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> MarkMfaVerifiedAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var idleLimit = now.Add(policy.IdleTimeout);
        var updated = await db.Sessions
            .Where(x => x.SessionId == sessionId && x.UserId == userId && x.RevokedAtUtc == null && !x.MfaVerified)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.MfaVerified, true)
                .SetProperty(x => x.SessionKind, "staff")
                .SetProperty(x => x.ExpiresAtUtc, x => x.AbsoluteExpiresAtUtc ?? x.ExpiresAtUtc)
                .SetProperty(x => x.IdleExpiresAtUtc, x => x.AbsoluteExpiresAtUtc.HasValue
                    ? (x.AbsoluteExpiresAtUtc.Value < idleLimit ? x.AbsoluteExpiresAtUtc.Value : idleLimit)
                    : idleLimit)
                .SetProperty(x => x.LastSeenAtUtc, now)
                .SetProperty(x => x.UpdatedAtUtc, now), cancellationToken);
        return updated == 1;
    }

    public async Task RevokeAsync(Guid sessionId, string reason, CancellationToken cancellationToken = default)
    {
        _ = reason;
        var now = DateTime.UtcNow;
        await db.Sessions
            .Where(x => x.SessionId == sessionId && x.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.RevokedAtUtc, now)
                .SetProperty(x => x.UpdatedAtUtc, now), cancellationToken);
    }

    public async Task<bool> RevokeOwnedAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var updated = await db.Sessions
            .Where(x => x.UserId == userId && x.SessionId == sessionId && x.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.RevokedAtUtc, now)
                .SetProperty(x => x.UpdatedAtUtc, now), cancellationToken);
        return updated == 1;
    }

    public async Task RevokeAllForUserAsync(Guid userId, string reason, CancellationToken cancellationToken = default)
    {
        _ = reason;
        var now = DateTime.UtcNow;
        await db.Sessions.Where(x => x.UserId == userId && x.RevokedAtUtc == null).ExecuteUpdateAsync(setters => setters
            .SetProperty(x => x.RevokedAtUtc, now)
            .SetProperty(x => x.UpdatedAtUtc, now), cancellationToken);
    }

    private static SessionRecord ToRecord(SessionEntity entity)
        => new(
            entity.SessionId,
            entity.UserId,
            entity.GymId,
            entity.MfaVerified,
            entity.ExpiresAtUtc,
            entity.IdleExpiresAtUtc ?? entity.ExpiresAtUtc,
            entity.AbsoluteExpiresAtUtc ?? entity.ExpiresAtUtc,
            entity.CreatedAtUtc,
            entity.LastSeenAtUtc ?? entity.CreatedAtUtc,
            entity.SessionKind,
            entity.UserAgent);

    private static string CreateRawToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

    internal static string HashToken(string rawToken)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();

    private static DateTime Min(DateTime left, DateTime right) => left <= right ? left : right;

    private static string? Truncate(string? value, int maxLength)
        => string.IsNullOrWhiteSpace(value) ? null : value[..Math.Min(value.Length, maxLength)];
}
