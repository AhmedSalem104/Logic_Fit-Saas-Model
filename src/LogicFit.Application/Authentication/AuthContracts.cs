using LogicFit.Shared;

namespace LogicFit.Application;

public sealed record AuthRequestContext(
    string RequestId,
    string? UserAgent,
    string? IpAddress,
    string? RawSessionToken = null);

public sealed record AuthResult<T>(
    bool Succeeded,
    int StatusCode,
    string ErrorCode,
    string Message,
    T? Data = default,
    IReadOnlyList<ApiFieldError>? FieldErrors = null)
{
    public static AuthResult<T> Success(T data, int statusCode = 200) => new(true, statusCode, string.Empty, string.Empty, data);

    public static AuthResult<T> Failure(
        int statusCode,
        string errorCode,
        string message,
        IReadOnlyList<ApiFieldError>? fieldErrors = null)
        => new(false, statusCode, errorCode, message, default, fieldErrors);
}

public sealed record LoginCommand(string? Email, string? Password);
public sealed record RefreshCommand(string? RefreshToken);
public sealed record LogoutCommand(Guid SessionId);
public sealed record PasswordChangeCommand(string? CurrentPassword, string? NewPassword);
public sealed record PasswordResetRequestCommand(string? Email);
public sealed record PasswordResetCompleteCommand(string? Token, string? NewPassword);
public sealed record MfaVerifyCommand(string? Challenge, string? Method, string? Code);
public sealed record MfaDisableCommand(string? CurrentPassword, string? Code);
public sealed record RecoveryCodesRegenerateCommand(string? CurrentPassword, string? Code);
public sealed record AccessUserCreateCommand(string? Email, string? DisplayName, string? InitialPassword, Guid RoleId, Guid? GymId);
public sealed record AccessUserStatusCommand(string? Status, string? Reason);
public sealed record RoleAssignmentCommand(string? Reason);
public sealed record RoleRevocationCommand(string? Reason);

public sealed record AuthUserDto(
    Guid UserId,
    string Email,
    string DisplayName,
    string Status,
    DateTime? LastLoginAtUtc,
    string Version);

public sealed record AuthScopeDto(Guid? GymId, string ScopeType, IReadOnlyList<string> Permissions);

public sealed record AuthSessionDto(
    string AccessToken,
    Guid SessionId,
    bool RequiresMfa,
    string? Challenge,
    bool MfaVerified,
    DateTime ExpiresAtUtc,
    DateTime IdleExpiresAtUtc,
    DateTime AbsoluteExpiresAtUtc,
    AuthUserDto User);

public sealed record AuthMeDto(AuthUserDto User, IReadOnlyList<AuthScopeDto> Scopes, IReadOnlyList<string> Permissions);

public sealed record SimpleRevocationDto(Guid SessionId, bool Revoked);
public sealed record RoleRevocationDto(Guid AssignmentId, bool Revoked);
public sealed record PasswordChangeDto(bool Changed, bool ReauthenticationRequired);
public sealed record PasswordResetAcceptedDto(bool Accepted);
public sealed record MfaEnrollmentDto(Guid FactorId, string Status, string Secret, string ProvisioningUri);
public sealed record MfaVerificationDto(bool Verified, AuthSessionDto? Session);
public sealed record MfaDisablementDto(bool Disabled);
public sealed record RecoveryCodesDto(IReadOnlyList<string> Codes);

public sealed record SessionListItemDto(
    Guid SessionId,
    Guid? GymId,
    string SessionKind,
    bool MfaVerified,
    DateTime CreatedAtUtc,
    DateTime LastSeenAtUtc,
    DateTime IdleExpiresAtUtc,
    DateTime AbsoluteExpiresAtUtc,
    DateTime ExpiresAtUtc,
    string? UserAgent,
    bool IsCurrent);
public sealed record SessionPageDto(IReadOnlyList<SessionListItemDto> Items, int Page, int PageSize, int Total, bool HasNext);

public sealed record AccessPermissionDto(Guid PermissionId, string Key, string Domain, string Action, string RiskLevel, string Description);
public sealed record AccessRoleDto(Guid RoleId, string ScopeType, string Name, string Status, IReadOnlyList<AccessPermissionDto> Permissions);
public sealed record AccessCatalogDto(IReadOnlyList<AccessPermissionDto> Permissions, IReadOnlyList<AccessRoleDto> Roles, int RolePermissionAssignmentCount);

public sealed record AccessRoleAssignmentDto(
    Guid AssignmentId,
    Guid UserId,
    Guid RoleId,
    string RoleName,
    Guid? GymId,
    string ScopeType,
    string Status,
    string Version);

public sealed record AccessUserDto(
    Guid UserId,
    string Email,
    string DisplayName,
    string Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string Version,
    IReadOnlyList<AccessRoleAssignmentDto> Assignments);

public sealed record AccessUsersPageDto(
    IReadOnlyList<AccessUserDto> Items,
    int Page,
    int PageSize,
    int Total,
    bool HasNext);

public sealed record AccessAssignmentDto(
    Guid AssignmentId,
    Guid UserId,
    Guid RoleId,
    string RoleName,
    Guid? GymId,
    string ScopeType,
    string Status,
    string Version);

public sealed record AccessStatusDto(Guid UserId, string Status, bool SessionsRevoked, string Version);

public sealed record AuthUserRecord(
    Guid UserId,
    string Email,
    string DisplayName,
    string Status,
    DateTime? LastLoginAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    byte[] RowVersion);

public sealed record AuthCredentialRecord(Guid CredentialId, Guid UserId, string SecretHash, string SecretVersion);
public sealed record AuthMfaRecord(Guid FactorId, Guid UserId, string FactorType, string SecretRef, string Status, DateTime? VerifiedAtUtc);

public sealed record AuthAssignmentRecord(
    Guid AssignmentId,
    Guid UserId,
    Guid? GymId,
    Guid RoleId,
    string ScopeType,
    string RoleName,
    string Status,
    byte[] RowVersion,
    IReadOnlySet<string> Permissions);

public sealed record AuthSessionResolution(AuthenticatedUser User, SessionRecord Session);
public sealed record PasswordResetRecord(Guid TokenId, Guid UserId, DateTime ExpiresAtUtc, bool Used, bool Revoked);

public sealed record AccessCatalogRecord(
    IReadOnlyList<AccessPermissionDto> Permissions,
    IReadOnlyList<AccessRoleDto> Roles,
    int RolePermissionAssignmentCount);

public sealed record AccessUsersQuery(
    int Page,
    int PageSize,
    Guid? GymId,
    string? ScopeType,
    string? Status,
    string? Search,
    string? SortField,
    bool SortDescending);

public interface IAuthRepository
{
    Task<AuthUserRecord?> FindUserByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task<AuthUserRecord?> FindUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AuthCredentialRecord?> FindPasswordCredentialAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AuthMfaRecord?> FindEnabledMfaAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AuthMfaRecord?> FindMfaAsync(Guid userId, Guid factorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuthAssignmentRecord>> GetAssignmentsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task UpdateLastLoginAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task WriteAuditAsync(AuditEntry entry, CancellationToken cancellationToken = default);

    Task<bool> ChangePasswordAndRevokeSessionsAsync(Guid userId, string passwordHash, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task CreatePasswordResetTokenAsync(Guid userId, string tokenHash, DateTime expiresAtUtc, string? ipAddress, string requestId, CancellationToken cancellationToken = default);
    Task<PasswordResetRecord?> FindPasswordResetTokenAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<bool> CompletePasswordResetAsync(Guid tokenId, Guid userId, string passwordHash, DateTime nowUtc, CancellationToken cancellationToken = default);

    Task<AuthMfaRecord> CreatePendingMfaAsync(Guid userId, string protectedSecret, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task<bool> EnableMfaAsync(Guid userId, Guid factorId, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task<bool> DisableMfaAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task ReplaceRecoveryCodesAsync(Guid userId, Guid factorId, IReadOnlyList<string> codeHashes, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task<bool> ConsumeRecoveryCodeAsync(Guid userId, string codeHash, DateTime nowUtc, CancellationToken cancellationToken = default);

    Task<AccessCatalogRecord> GetAccessCatalogAsync(CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<AccessUserDto> Items, int Total)> ListAccessUsersAsync(AccessUsersQuery query, CancellationToken cancellationToken = default);
    Task<AccessUserDto?> CreateAccessUserAsync(string email, string displayName, string passwordHash, Guid roleId, Guid? gymId, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task<AccessStatusDto?> ChangeUserStatusAsync(Guid userId, string status, byte[]? expectedVersion, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task<(AccessAssignmentDto? Assignment, string Outcome, bool VersionConflict)> EnsureRoleAssignmentAsync(Guid userId, Guid roleId, Guid? gymId, byte[]? expectedVersion, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task<(bool Found, bool Changed, bool VersionConflict)> RevokeRoleAssignmentAsync(Guid userId, Guid assignmentId, byte[]? expectedVersion, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task<bool> IsGymActiveAsync(Guid gymId, CancellationToken cancellationToken = default);
}

public sealed record AuditEntry(
    string RequestId,
    Guid? ActorUserId,
    string TargetType,
    Guid? TargetId,
    string Action,
    string Result,
    string? Reason,
    string? MetadataJson = null,
    string? ScopeType = null,
    Guid? ScopeId = null);

public interface IAuthenticationService
{
    Task<AuthResult<AuthSessionDto>> LoginAsync(LoginCommand command, AuthRequestContext context, CancellationToken cancellationToken = default);
    Task<AuthResult<AuthSessionDto>> RefreshAsync(RefreshCommand command, AuthRequestContext context, CancellationToken cancellationToken = default);
    Task<AuthResult<SimpleRevocationDto>> LogoutAsync(AuthenticatedUser currentUser, LogoutCommand command, AuthRequestContext context, CancellationToken cancellationToken = default);
    Task<AuthResult<AuthMeDto>> GetMeAsync(AuthenticatedUser currentUser, CancellationToken cancellationToken = default);
    Task<AuthResult<PasswordChangeDto>> ChangePasswordAsync(AuthenticatedUser currentUser, PasswordChangeCommand command, AuthRequestContext context, CancellationToken cancellationToken = default);
    Task<AuthResult<PasswordResetAcceptedDto>> RequestPasswordResetAsync(PasswordResetRequestCommand command, AuthRequestContext context, CancellationToken cancellationToken = default);
    Task<AuthResult<PasswordResetAcceptedDto>> CompletePasswordResetAsync(PasswordResetCompleteCommand command, AuthRequestContext context, CancellationToken cancellationToken = default);
    Task<AuthResult<MfaEnrollmentDto>> EnrollMfaAsync(AuthenticatedUser currentUser, AuthRequestContext context, CancellationToken cancellationToken = default);
    Task<AuthResult<MfaVerificationDto>> VerifyMfaAsync(AuthenticatedUser? currentUser, MfaVerifyCommand command, AuthRequestContext context, CancellationToken cancellationToken = default);
    Task<AuthResult<MfaDisablementDto>> DisableMfaAsync(AuthenticatedUser currentUser, MfaDisableCommand command, AuthRequestContext context, CancellationToken cancellationToken = default);
    Task<AuthResult<RecoveryCodesDto>> RegenerateRecoveryCodesAsync(AuthenticatedUser currentUser, RecoveryCodesRegenerateCommand command, AuthRequestContext context, CancellationToken cancellationToken = default);
    Task<AuthResult<SessionPageDto>> ListSessionsAsync(AuthenticatedUser currentUser, Guid? gymId, int page, int pageSize, string? sort, bool descending, AuthRequestContext context, CancellationToken cancellationToken = default);
    Task<AuthResult<SimpleRevocationDto>> RevokeOwnedSessionAsync(AuthenticatedUser currentUser, Guid sessionId, string? reason, AuthRequestContext context, CancellationToken cancellationToken = default);
    Task<AuthSessionResolution?> ResolveSessionAsync(string rawToken, CancellationToken cancellationToken = default);
    Task<bool> HasPermissionAsync(AuthenticatedUser currentUser, string permissionKey, Guid? targetGymId = null, bool allowPlatformControlPlane = false, CancellationToken cancellationToken = default);
    Task<AuthResult<AccessCatalogDto>> GetAccessCatalogAsync(AuthenticatedUser currentUser, AuthRequestContext context, CancellationToken cancellationToken = default);
    Task<AuthResult<AccessUsersPageDto>> ListAccessUsersAsync(AuthenticatedUser currentUser, AccessUsersQuery query, AuthRequestContext context, CancellationToken cancellationToken = default);
    Task<AuthResult<AccessUserDto>> CreateAccessUserAsync(AuthenticatedUser currentUser, AccessUserCreateCommand command, AuthRequestContext context, CancellationToken cancellationToken = default);
    Task<AuthResult<AccessStatusDto>> ChangeUserStatusAsync(AuthenticatedUser currentUser, Guid targetUserId, AccessUserStatusCommand command, byte[]? expectedVersion, AuthRequestContext context, CancellationToken cancellationToken = default);
    Task<AuthResult<AccessAssignmentDto>> EnsureRoleAssignmentAsync(AuthenticatedUser currentUser, Guid targetUserId, Guid roleId, Guid? gymId, byte[]? expectedVersion, RoleAssignmentCommand command, AuthRequestContext context, CancellationToken cancellationToken = default);
    Task<AuthResult<RoleRevocationDto>> RevokeRoleAssignmentAsync(AuthenticatedUser currentUser, Guid targetUserId, Guid assignmentId, byte[]? expectedVersion, RoleRevocationCommand command, AuthRequestContext context, CancellationToken cancellationToken = default);
}
