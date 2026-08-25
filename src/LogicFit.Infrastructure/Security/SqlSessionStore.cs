using System.Security.Cryptography;
using System.Text;
using LogicFit.Application;
using LogicFit.Infrastructure.Persistence;
using LogicFit.Infrastructure.Persistence.Entities;
using LogicFit.Domain.ValueObjects;
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
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
        var entity = new SessionEntity
        {
            SessionId = Guid.NewGuid(),
            UserId = userId,
            GymId = gymId,
            TokenHash = HashToken(rawToken),
            ExpiresAtUtc = now.Add(policy.AbsoluteLifetime),
            IdleExpiresAtUtc = now.Add(policy.IdleTimeout),
            AbsoluteExpiresAtUtc = now.Add(policy.AbsoluteLifetime),
            SessionKind = mfaVerified ? "staff" : "mfa_pending",
            MfaVerified = mfaVerified,
            UserAgent = userAgent,
            IpAddress = ipAddress,
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

        return entity is null
            ? null
            : new SessionRecord(entity.SessionId, entity.UserId, entity.GymId, entity.MfaVerified, entity.ExpiresAtUtc, entity.IdleExpiresAtUtc ?? entity.ExpiresAtUtc, entity.AbsoluteExpiresAtUtc ?? entity.ExpiresAtUtc);
    }

    public async Task RevokeAsync(Guid sessionId, string reason, CancellationToken cancellationToken = default)
    {
        _ = reason;
        var entity = await db.Sessions.FirstOrDefaultAsync(x => x.SessionId == sessionId, cancellationToken);
        if (entity is null || entity.RevokedAtUtc is not null)
        {
            return;
        }

        entity.RevokedAtUtc = DateTime.UtcNow;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllForUserAsync(Guid userId, string reason, CancellationToken cancellationToken = default)
    {
        _ = reason;
        var now = DateTime.UtcNow;
        await db.Sessions.Where(x => x.UserId == userId && x.RevokedAtUtc == null).ExecuteUpdateAsync(setters => setters
            .SetProperty(x => x.RevokedAtUtc, now)
            .SetProperty(x => x.UpdatedAtUtc, now), cancellationToken);
    }

    private static string HashToken(string rawToken)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();
}
