namespace LogicFit.Domain.Constants;

public sealed record PermissionDefinition(string Key, string Domain, string Action, string RiskLevel, string Description);
public sealed record RoleDefinition(string Key, string ScopeType, string Name);
public sealed record RolePermissionDefinition(string RoleKey, string PermissionKey);

public static class PermissionCatalog
{
    public static IReadOnlyList<PermissionDefinition> Permissions { get; } =
    [
        new("auth.login", "auth", "login", "normal", "Start a staff session after credential validation."),
        new("auth.logout", "auth", "logout", "normal", "Revoke the current authenticated session."),
        new("auth.password.change", "auth", "password.change", "high", "Change the authenticated user's password."),
        new("auth.password.reset", "auth", "password.reset", "high", "Use the approved password recovery capability."),
        new("auth.password_reset.request", "auth", "password_reset.request", "normal", "Request a generic password reset response."),
        new("auth.password_reset.complete", "auth", "password_reset.complete", "high", "Complete a single-use password reset exchange."),
        new("auth.mfa.enroll", "auth", "mfa.enroll", "high", "Start Authenticator App/TOTP enrollment."),
        new("auth.mfa.verify", "auth", "mfa.verify", "high", "Verify a TOTP login or enrollment challenge."),
        new("auth.mfa.disable", "auth", "mfa.disable", "high", "Disable the authenticated user's TOTP factor after step-up."),
        new("auth.mfa.recovery", "auth", "mfa.recovery", "high", "Regenerate or use MFA recovery codes."),
        new("auth.sessions.view", "auth", "sessions.view", "normal", "View the authenticated user's active sessions."),
        new("auth.sessions.revoke", "auth", "sessions.revoke", "high", "Revoke an owned authenticated session."),
        new("auth.session.manage", "auth", "session.manage", "high", "Manage sessions under the approved authentication contract."),
        new("platform.security.manage", "platform", "security.manage", "critical", "Manage users, roles, permission assignments and security controls."),
        new("platform.view", "platform", "view", "normal", "View safe platform scope information.")
    ];

    public static IReadOnlyList<RoleDefinition> Roles { get; } =
    [
        new("gym-authenticated-user", "gym", "Gym Authenticated User"),
        new("gym-security-admin", "gym", "Gym Security Admin"),
        new("platform-security-admin", "platform", "Platform Security Admin")
    ];

    public static IReadOnlyList<RolePermissionDefinition> RolePermissions { get; } =
    [
        new("gym-authenticated-user", "auth.login"),
        new("gym-authenticated-user", "auth.logout"),
        new("gym-authenticated-user", "auth.password.change"),
        new("gym-authenticated-user", "auth.password.reset"),
        new("gym-authenticated-user", "auth.mfa.enroll"),
        new("gym-authenticated-user", "auth.mfa.verify"),
        new("gym-authenticated-user", "auth.mfa.disable"),
        new("gym-authenticated-user", "auth.mfa.recovery"),
        new("gym-authenticated-user", "auth.sessions.view"),
        new("gym-authenticated-user", "auth.sessions.revoke"),
        new("gym-authenticated-user", "auth.session.manage"),
        new("gym-security-admin", "platform.security.manage"),
        new("platform-security-admin", "platform.security.manage"),
        new("platform-security-admin", "platform.view")
    ];
}
