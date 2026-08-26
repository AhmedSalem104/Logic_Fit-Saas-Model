namespace LogicFit.Application;

public sealed record AuthenticatedUser(
    Guid UserId,
    Guid? GymId,
    bool IsMfaVerified,
    IReadOnlySet<string> Permissions,
    Guid? SessionId = null);

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
    DateTime AbsoluteExpiresAtUtc,
    DateTime CreatedAtUtc,
    DateTime LastSeenAtUtc,
    string SessionKind,
    string? UserAgent);

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
    Task<SessionRecord?> FindByIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<SessionRecord?> FindOwnedByIdAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SessionRecord>> ListActiveForUserAsync(Guid userId, Guid? gymId, CancellationToken cancellationToken = default);
    Task TouchAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<bool> MarkMfaVerifiedAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken = default);
    Task RevokeAsync(Guid sessionId, string reason, CancellationToken cancellationToken = default);
    Task<bool> RevokeOwnedAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default);
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

public interface ISecretProtector
{
    string Protect(string plaintext);
    string Unprotect(string protectedValue);
}

public interface IGymScopeService
{
    Task<Domain.ValueObjects.GymScope?> ResolveAsync(Guid gymId, CancellationToken cancellationToken = default);
}
