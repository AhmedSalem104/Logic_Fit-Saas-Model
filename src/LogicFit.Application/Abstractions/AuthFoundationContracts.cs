namespace LogicFit.Application;

public sealed record AuthenticatedUser(Guid UserId, Guid? GymId, bool IsMfaVerified, IReadOnlySet<string> Permissions);

public interface ICurrentUserAccessor
{
    AuthenticatedUser? Current { get; set; }
}

public sealed record SessionCreated(
    Guid SessionId,
    string RawToken,
    DateTime ExpiresAtUtc,
    DateTime IdleExpiresAtUtc,
    DateTime AbsoluteExpiresAtUtc);

public sealed record SessionRecord(
    Guid SessionId,
    Guid UserId,
    Guid? GymId,
    bool MfaVerified,
    DateTime ExpiresAtUtc,
    DateTime IdleExpiresAtUtc,
    DateTime AbsoluteExpiresAtUtc);

public interface ISessionStore
{
    Task<SessionCreated> CreateAsync(
        Guid userId,
        Guid? gymId,
        bool mfaVerified,
        string? userAgent,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<SessionRecord?> FindActiveAsync(string rawToken, CancellationToken cancellationToken = default);
    Task RevokeAsync(Guid sessionId, string reason, CancellationToken cancellationToken = default);
    Task RevokeAllForUserAsync(Guid userId, string reason, CancellationToken cancellationToken = default);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string encodedHash, string password);
}

public sealed record TotpProvisioning(string Secret, string ProvisioningUri);

public interface ITotpService
{
    TotpProvisioning CreateProvisioning(string accountName);
    bool Verify(string secret, string code, DateTimeOffset? timestamp = null);
}

public interface IRecoveryCodeGenerator
{
    IReadOnlyList<string> Generate(int count);
}

public interface IGymScopeService
{
    Task<Domain.ValueObjects.GymScope?> ResolveAsync(Guid gymId, CancellationToken cancellationToken = default);
}
