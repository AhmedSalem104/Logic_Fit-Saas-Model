using System.Security.Cryptography;
using System.Text;
using LogicFit.Domain.Constants;
using LogicFit.Domain.ValueObjects;

namespace LogicFit.Application;

public sealed class AuthenticationService(
    IAuthRepository repository,
    ISessionStore sessions,
    IPasswordHasher passwordHasher,
    ITotpService totp,
    IRecoveryCodeGenerator recoveryCodes,
    ISecretProtector secretProtector,
    SessionPolicy sessionPolicy) : IAuthenticationService
{
    private const int PasswordMinimumLength = 12;
    private const int PasswordMaximumLength = 256;
    private const int RecoveryCodeCount = 10;
    private const string Active = "active";
    private const string Disabled = "disabled";
    private const string GymScope = "gym";
    private const string PlatformScope = "platform";

    public async Task<AuthResult<AuthSessionDto>> LoginAsync(LoginCommand command, AuthRequestContext context, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(command.Email);
        var emailError = ValidateEmail(email);
        if (emailError is not null || string.IsNullOrEmpty(command.Password))
        {
            return Failure<AuthSessionDto>(400, "VALIDATION_ERROR", "Email and password are required.",
                emailError is null ? [new("password", "required")] : [new("email", emailError)]);
        }

        var user = await repository.FindUserByEmailAsync(email, cancellationToken);
        var credential = user is null ? null : await repository.FindPasswordCredentialAsync(user.UserId, cancellationToken);
        var validPassword = credential is not null && passwordHasher.Verify(credential.SecretHash, command.Password);
        if (user is null || !validPassword || !string.Equals(user.Status, Active, StringComparison.OrdinalIgnoreCase))
        {
            await AuditAsync(context, user?.UserId, "iam.user", user?.UserId, "auth.login.failed", "failure", "invalid_credentials", cancellationToken);
            return Failure<AuthSessionDto>(401, "AUTHENTICATION_FAILED", "The supplied credentials could not be authenticated.");
        }

        var scope = await SelectLoginScopeAsync(user.UserId, cancellationToken);
        if (scope is null)
        {
            await AuditAsync(context, user.UserId, "iam.user", user.UserId, "auth.login.failed", "failure", "no_active_scope", cancellationToken);
            return Failure<AuthSessionDto>(401, "AUTHENTICATION_FAILED", "The supplied credentials could not be authenticated.");
        }

        var enabledMfa = await repository.FindEnabledMfaAsync(user.UserId, cancellationToken);
        var mfaRequired = enabledMfa is not null;
        var created = await sessions.CreateAsync(user.UserId, scope.Value.GymId, !mfaRequired, context.UserAgent, context.IpAddress, cancellationToken);
        if (!mfaRequired)
        {
            await repository.UpdateLastLoginAsync(user.UserId, DateTime.UtcNow, cancellationToken);
        }

        await AuditAsync(context, user.UserId, "iam.user", user.UserId, "auth.login.succeeded", "success", mfaRequired ? "mfa_required" : null, cancellationToken);
        return AuthResult<AuthSessionDto>.Success(ToSessionDto(created, user, mfaRequired, mfaRequired ? created.SessionId.ToString("D") : null));
    }

    public async Task<AuthResult<AuthSessionDto>> RefreshAsync(RefreshCommand command, AuthRequestContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            return Failure<AuthSessionDto>(400, "VALIDATION_ERROR", "A refresh token is required.", [new("refreshToken", "required")]);
        }

        var current = await sessions.FindActiveAsync(command.RefreshToken, cancellationToken);
        if (current is null)
        {
            return Failure<AuthSessionDto>(401, "SESSION_INVALID", "The session is no longer valid.");
        }

        var user = await repository.FindUserByIdAsync(current.UserId, cancellationToken);
        if (user is null || user.Status != Active)
        {
            return Failure<AuthSessionDto>(401, "SESSION_INVALID", "The session is no longer valid.");
        }

        if (current.GymId.HasValue && !await repository.IsGymActiveAsync(current.GymId.Value, cancellationToken))
        {
            return Failure<AuthSessionDto>(401, "SESSION_INVALID", "The session is no longer valid.");
        }

        var scope = await GetSessionScopeAsync(current, cancellationToken);
        if (scope is null)
        {
            return Failure<AuthSessionDto>(401, "SESSION_INVALID", "The session is no longer valid.");
        }

        await sessions.RevokeAsync(current.SessionId, "refresh_rotation", cancellationToken);
        var replacement = await sessions.CreateAsync(user.UserId, current.GymId, current.MfaVerified, context.UserAgent, context.IpAddress, cancellationToken);
        await AuditAsync(context, user.UserId, "iam.session", current.SessionId, "auth.session.refreshed", "success", null, cancellationToken);
        return AuthResult<AuthSessionDto>.Success(ToSessionDto(replacement, user, !current.MfaVerified, current.MfaVerified ? null : replacement.SessionId.ToString("D")));
    }

    public async Task<AuthResult<SimpleRevocationDto>> LogoutAsync(AuthenticatedUser currentUser, LogoutCommand command, AuthRequestContext context, CancellationToken cancellationToken = default)
    {
        if (!currentUser.SessionId.HasValue || command.SessionId != currentUser.SessionId.Value)
        {
            return Failure<SimpleRevocationDto>(403, "PERMISSION_DENIED", "Only the current session can be logged out through this operation.");
        }

        await sessions.RevokeAsync(command.SessionId, "logout", cancellationToken);
        await AuditAsync(context, currentUser.UserId, "iam.session", command.SessionId, "auth.logout", "success", null, cancellationToken);
        return AuthResult<SimpleRevocationDto>.Success(new SimpleRevocationDto(command.SessionId, true));
    }

    public async Task<AuthResult<AuthMeDto>> GetMeAsync(AuthenticatedUser currentUser, CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsMfaVerified)
        {
            return Failure<AuthMeDto>(403, "MFA_REQUIRED", "Complete the security challenge before accessing the authenticated account.");
        }

        var user = await repository.FindUserByIdAsync(currentUser.UserId, cancellationToken);
        if (user is null || user.Status != Active)
        {
            return Failure<AuthMeDto>(401, "SESSION_INVALID", "The session is no longer valid.");
        }

        var assignments = await repository.GetAssignmentsAsync(user.UserId, cancellationToken);
        var visible = assignments.Where(x => IsVisibleInSession(x, currentUser.GymId)).ToArray();
        var scopes = visible
            .GroupBy(x => (x.GymId, x.ScopeType))
            .Select(group => new AuthScopeDto(group.Key.GymId, group.Key.ScopeType, group.SelectMany(x => x.Permissions).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray()))
            .ToArray();
        return AuthResult<AuthMeDto>.Success(new AuthMeDto(ToUserDto(user), scopes, scopes.SelectMany(x => x.Permissions).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray()));
    }

    public async Task<AuthResult<PasswordChangeDto>> ChangePasswordAsync(AuthenticatedUser currentUser, PasswordChangeCommand command, AuthRequestContext context, CancellationToken cancellationToken = default)
    {
        var permission = await RequirePermissionAsync<PasswordChangeDto>(currentUser, "auth.password.change", context, cancellationToken);
        if (permission is not null)
        {
            return permission;
        }

        if (!currentUser.IsMfaVerified)
        {
            return Failure<PasswordChangeDto>(403, "MFA_REQUIRED", "Complete the security challenge before changing the password.");
        }

        if (string.IsNullOrEmpty(command.CurrentPassword) || string.IsNullOrEmpty(command.NewPassword))
        {
            return Failure<PasswordChangeDto>(400, "VALIDATION_ERROR", "Current and new passwords are required.", [new("password", "required")]);
        }

        var policyError = ValidatePassword(command.NewPassword);
        if (policyError is not null)
        {
            return Failure<PasswordChangeDto>(422, "PASSWORD_POLICY_VIOLATION", policyError, [new("newPassword", "policy")]);
        }

        var credential = await repository.FindPasswordCredentialAsync(currentUser.UserId, cancellationToken);
        if (credential is null || !passwordHasher.Verify(credential.SecretHash, command.CurrentPassword))
        {
            await AuditAsync(context, currentUser.UserId, "iam.user", currentUser.UserId, "auth.password.change.failed", "failure", "current_password_invalid", cancellationToken);
            return Failure<PasswordChangeDto>(422, "CURRENT_PASSWORD_INVALID", "The current password is not valid.");
        }

        var changed = await repository.ChangePasswordAndRevokeSessionsAsync(currentUser.UserId, passwordHasher.Hash(command.NewPassword), DateTime.UtcNow, cancellationToken);
        if (!changed)
        {
            return Failure<PasswordChangeDto>(409, "CONCURRENCY_CONFLICT", "The credential could not be changed safely.");
        }

        await AuditAsync(context, currentUser.UserId, "iam.user", currentUser.UserId, "auth.password.change.succeeded", "success", null, cancellationToken);
        return AuthResult<PasswordChangeDto>.Success(new PasswordChangeDto(true, true));
    }

    public async Task<AuthResult<PasswordResetAcceptedDto>> RequestPasswordResetAsync(PasswordResetRequestCommand command, AuthRequestContext context, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(command.Email);
        var emailError = ValidateEmail(email);
        if (emailError is not null)
        {
            return Failure<PasswordResetAcceptedDto>(400, "VALIDATION_ERROR", "A valid email is required.", [new("email", emailError)]);
        }

        var user = await repository.FindUserByEmailAsync(email, cancellationToken);
        if (user is not null && user.Status == Active && await repository.FindPasswordCredentialAsync(user.UserId, cancellationToken) is not null)
        {
            var token = CreateOpaqueToken();
            await repository.CreatePasswordResetTokenAsync(user.UserId, HashOpaque(token), DateTime.UtcNow.Add(sessionPolicy.PasswordResetLifetime), context.IpAddress, context.RequestId, cancellationToken);
            await AuditAsync(context, user.UserId, "iam.password_reset", null, "auth.password_reset.requested", "accepted", null, cancellationToken);
        }
        else
        {
            await AuditAsync(context, null, "iam.password_reset", null, "auth.password_reset.requested", "accepted", null, cancellationToken);
        }

        // The approved contract is enumeration-safe and does not return the token.
        return AuthResult<PasswordResetAcceptedDto>.Success(new PasswordResetAcceptedDto(true), 202);
    }

    public async Task<AuthResult<PasswordResetAcceptedDto>> CompletePasswordResetAsync(PasswordResetCompleteCommand command, AuthRequestContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Token) || string.IsNullOrEmpty(command.NewPassword))
        {
            return Failure<PasswordResetAcceptedDto>(400, "VALIDATION_ERROR", "A reset token and new password are required.", [new("token", "required"), new("newPassword", "required")]);
        }

        var policyError = ValidatePassword(command.NewPassword);
        if (policyError is not null)
        {
            return Failure<PasswordResetAcceptedDto>(422, "PASSWORD_POLICY_VIOLATION", policyError, [new("newPassword", "policy")]);
        }

        var reset = await repository.FindPasswordResetTokenAsync(HashOpaque(command.Token), cancellationToken);
        if (reset is null || reset.Used || reset.Revoked || reset.ExpiresAtUtc <= DateTime.UtcNow)
        {
            await AuditAsync(context, null, "iam.password_reset", null, "auth.password_reset.completed", "failure", "invalid_token", cancellationToken);
            return Failure<PasswordResetAcceptedDto>(422, "RESET_TOKEN_INVALID", "The password reset request is no longer valid.");
        }

        var completed = await repository.CompletePasswordResetAsync(reset.TokenId, reset.UserId, passwordHasher.Hash(command.NewPassword), DateTime.UtcNow, cancellationToken);
        if (!completed)
        {
            return Failure<PasswordResetAcceptedDto>(422, "RESET_TOKEN_INVALID", "The password reset request is no longer valid.");
        }

        await AuditAsync(context, reset.UserId, "iam.password_reset", reset.TokenId, "auth.password_reset.completed", "success", null, cancellationToken);
        return AuthResult<PasswordResetAcceptedDto>.Success(new PasswordResetAcceptedDto(true));
    }

    public async Task<AuthResult<MfaEnrollmentDto>> EnrollMfaAsync(AuthenticatedUser currentUser, AuthRequestContext context, CancellationToken cancellationToken = default)
    {
        var permission = await RequirePermissionAsync<MfaEnrollmentDto>(currentUser, "auth.mfa.enroll", context, cancellationToken);
        if (permission is not null)
        {
            return permission;
        }

        if (!currentUser.IsMfaVerified)
        {
            return Failure<MfaEnrollmentDto>(403, "MFA_REQUIRED", "Complete the security challenge before changing MFA settings.");
        }

        if (await repository.FindEnabledMfaAsync(currentUser.UserId, cancellationToken) is not null)
        {
            return Failure<MfaEnrollmentDto>(409, "MFA_ALREADY_ENABLED", "MFA is already enabled for this account.");
        }

        var provisioning = totp.CreateProvisioning(currentUser.UserId.ToString("N"));
        var pending = await repository.CreatePendingMfaAsync(currentUser.UserId, secretProtector.Protect(provisioning.Secret), DateTime.UtcNow, cancellationToken);
        await AuditAsync(context, currentUser.UserId, "iam.mfa_factor", pending.FactorId, "auth.mfa.enrollment.started", "success", null, cancellationToken);
        return AuthResult<MfaEnrollmentDto>.Success(new MfaEnrollmentDto(pending.FactorId, pending.Status, provisioning.Secret, provisioning.ProvisioningUri));
    }

    public async Task<AuthResult<MfaVerificationDto>> VerifyMfaAsync(AuthenticatedUser? currentUser, MfaVerifyCommand command, AuthRequestContext context, CancellationToken cancellationToken = default)
    {
        var method = string.IsNullOrWhiteSpace(command.Method) ? "totp" : command.Method.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(command.Challenge) || string.IsNullOrWhiteSpace(command.Code) || method is not ("totp" or "recovery_code") || !Guid.TryParse(command.Challenge, out var challengeId))
        {
            return Failure<MfaVerificationDto>(400, "VALIDATION_ERROR", "A valid MFA challenge, method, and code are required.");
        }

        var pendingSession = await sessions.FindByIdAsync(challengeId, cancellationToken);
        if (pendingSession is not null && pendingSession.SessionKind == "mfa_pending" && !pendingSession.MfaVerified)
        {
            if (currentUser is null
                || currentUser.UserId != pendingSession.UserId
                || currentUser.SessionId != pendingSession.SessionId
                || string.IsNullOrWhiteSpace(context.RawSessionToken))
            {
                return Failure<MfaVerificationDto>(401, "MFA_VERIFICATION_FAILED", "The MFA verification could not be completed.");
            }

            var factor = await repository.FindEnabledMfaAsync(pendingSession.UserId, cancellationToken);
            if (factor is null || !await VerifyFactorAsync(pendingSession.UserId, factor, method, command.Code, cancellationToken))
            {
                await AuditAsync(context, pendingSession.UserId, "iam.mfa_factor", factor?.FactorId, FailureAction(method), "failure", "verification_failed", cancellationToken);
                return Failure<MfaVerificationDto>(401, "MFA_VERIFICATION_FAILED", "The MFA verification could not be completed.");
            }

            if (!await sessions.MarkMfaVerifiedAsync(pendingSession.SessionId, pendingSession.UserId, cancellationToken))
            {
                return Failure<MfaVerificationDto>(409, "CONCURRENCY_CONFLICT", "The MFA challenge was already completed or changed.");
            }

            var user = await repository.FindUserByIdAsync(pendingSession.UserId, cancellationToken);
            if (user is null || user.Status != Active)
            {
                return Failure<MfaVerificationDto>(401, "MFA_VERIFICATION_FAILED", "The MFA verification could not be completed.");
            }

            await repository.UpdateLastLoginAsync(user.UserId, DateTime.UtcNow, cancellationToken);
            await AuditAsync(context, user.UserId, "iam.mfa_factor", factor.FactorId, SuccessAction(method), "success", null, cancellationToken);
            var verifiedSession = string.IsNullOrWhiteSpace(context.RawSessionToken)
                ? null
                : await sessions.FindActiveAsync(context.RawSessionToken, cancellationToken);
            if (verifiedSession is null)
            {
                return Failure<MfaVerificationDto>(409, "CONCURRENCY_CONFLICT", "The MFA session could not be completed safely.");
            }

            var sessionDto = ToSessionDto(
                new SessionCreated(
                    verifiedSession.SessionId,
                    context.RawSessionToken!,
                    verifiedSession.ExpiresAtUtc,
                    verifiedSession.IdleExpiresAtUtc,
                    verifiedSession.AbsoluteExpiresAtUtc),
                user,
                false,
                null);
            return AuthResult<MfaVerificationDto>.Success(new MfaVerificationDto(true, sessionDto));
        }

        if (currentUser is null || method != "totp")
        {
            return Failure<MfaVerificationDto>(401, "MFA_VERIFICATION_FAILED", "The MFA verification could not be completed.");
        }

        var pendingFactor = await repository.FindMfaAsync(currentUser.UserId, challengeId, cancellationToken);
        if (pendingFactor is null || pendingFactor.Status != "pending")
        {
            return Failure<MfaVerificationDto>(401, "MFA_VERIFICATION_FAILED", "The MFA verification could not be completed.");
        }

        if (!await VerifyFactorAsync(currentUser.UserId, pendingFactor, "totp", command.Code, cancellationToken)
            || !await repository.EnableMfaAsync(currentUser.UserId, pendingFactor.FactorId, DateTime.UtcNow, cancellationToken))
        {
            await AuditAsync(context, currentUser.UserId, "iam.mfa_factor", pendingFactor.FactorId, "auth.mfa.totp.verification_failed", "failure", "verification_failed", cancellationToken);
            return Failure<MfaVerificationDto>(401, "MFA_VERIFICATION_FAILED", "The MFA verification could not be completed.");
        }

        var generatedCodes = recoveryCodes.Generate(RecoveryCodeCount);
        await repository.ReplaceRecoveryCodesAsync(currentUser.UserId, pendingFactor.FactorId, generatedCodes.Select(HashOpaque).ToArray(), DateTime.UtcNow, cancellationToken);
        await AuditAsync(context, currentUser.UserId, "iam.mfa_factor", pendingFactor.FactorId, "auth.mfa.enabled", "success", null, cancellationToken);
        return AuthResult<MfaVerificationDto>.Success(new MfaVerificationDto(true, null));
    }

    public async Task<AuthResult<MfaDisablementDto>> DisableMfaAsync(AuthenticatedUser currentUser, MfaDisableCommand command, AuthRequestContext context, CancellationToken cancellationToken = default)
    {
        var permission = await RequirePermissionAsync<MfaDisablementDto>(currentUser, "auth.mfa.disable", context, cancellationToken);
        if (permission is not null)
        {
            return permission;
        }

        if (!currentUser.IsMfaVerified)
        {
            return Failure<MfaDisablementDto>(403, "MFA_REQUIRED", "Complete the security challenge before changing MFA settings.");
        }

        var stepUp = await VerifyStepUpAsync(currentUser.UserId, command.CurrentPassword, command.Code, cancellationToken);
        if (!stepUp)
        {
            return Failure<MfaDisablementDto>(422, "MFA_STEP_UP_REQUIRED", "A valid password or current authenticator code is required.");
        }

        if (!await repository.DisableMfaAsync(currentUser.UserId, DateTime.UtcNow, cancellationToken))
        {
            return Failure<MfaDisablementDto>(409, "MFA_NOT_ENABLED", "MFA is not enabled for this account.");
        }

        await sessions.RevokeAllForUserAsync(currentUser.UserId, "mfa_disabled", cancellationToken);
        await AuditAsync(context, currentUser.UserId, "iam.mfa_factor", currentUser.UserId, "auth.mfa.disabled", "success", null, cancellationToken);
        return AuthResult<MfaDisablementDto>.Success(new MfaDisablementDto(true));
    }

    public async Task<AuthResult<RecoveryCodesDto>> RegenerateRecoveryCodesAsync(AuthenticatedUser currentUser, RecoveryCodesRegenerateCommand command, AuthRequestContext context, CancellationToken cancellationToken = default)
    {
        var permission = await RequirePermissionAsync<RecoveryCodesDto>(currentUser, "auth.mfa.recovery", context, cancellationToken);
        if (permission is not null)
        {
            return permission;
        }

        if (!currentUser.IsMfaVerified)
        {
            return Failure<RecoveryCodesDto>(403, "MFA_REQUIRED", "Complete the security challenge before changing MFA settings.");
        }

        var factor = await repository.FindEnabledMfaAsync(currentUser.UserId, cancellationToken);
        if (factor is null)
        {
            return Failure<RecoveryCodesDto>(409, "MFA_NOT_ENABLED", "MFA is not enabled for this account.");
        }

        if (!await VerifyStepUpAsync(currentUser.UserId, command.CurrentPassword, command.Code, cancellationToken))
        {
            return Failure<RecoveryCodesDto>(422, "MFA_STEP_UP_REQUIRED", "A valid password or current authenticator code is required.");
        }

        var generated = recoveryCodes.Generate(RecoveryCodeCount);
        await repository.ReplaceRecoveryCodesAsync(currentUser.UserId, factor.FactorId, generated.Select(HashOpaque).ToArray(), DateTime.UtcNow, cancellationToken);
        await AuditAsync(context, currentUser.UserId, "iam.mfa_recovery", currentUser.UserId, "auth.mfa.recovery_codes.regenerated", "success", null, cancellationToken);
        return AuthResult<RecoveryCodesDto>.Success(new RecoveryCodesDto(generated));
    }

    public async Task<AuthResult<SessionPageDto>> ListSessionsAsync(AuthenticatedUser currentUser, Guid? gymId, int page, int pageSize, string? sort, bool descending, AuthRequestContext context, CancellationToken cancellationToken = default)
    {
        var permission = await RequirePermissionAsync<SessionPageDto>(currentUser, "auth.sessions.view", context, cancellationToken);
        if (permission is not null)
        {
            return permission;
        }

        if (page < 1 || pageSize is < 1 or > 100 || (sort is not null && sort is not ("createdAtUtc" or "lastSeenAtUtc" or "expiresAtUtc")))
        {
            return Failure<SessionPageDto>(400, "INVALID_FILTER", "The requested session filters are not valid.");
        }

        var targetGym = gymId ?? currentUser.GymId;
        if (targetGym.HasValue && !await HasPermissionAsync(currentUser, "auth.sessions.view", targetGym, false, cancellationToken))
        {
            await AuditAsync(context, currentUser.UserId, "iam.authorization", targetGym, "authz.scope_denied", "failure", "auth.sessions.view", cancellationToken);
            return Failure<SessionPageDto>(403, "GYM_SCOPE_DENIED", "The requested Gym is outside the authorized scope.");
        }

        var allRecords = await sessions.ListActiveForUserAsync(currentUser.UserId, targetGym, cancellationToken);
        var total = allRecords.Count;
        var records = allRecords.AsEnumerable();
        records = sort switch
        {
            "lastSeenAtUtc" => descending ? records.OrderByDescending(x => x.LastSeenAtUtc) : records.OrderBy(x => x.LastSeenAtUtc),
            "expiresAtUtc" => descending ? records.OrderByDescending(x => x.ExpiresAtUtc) : records.OrderBy(x => x.ExpiresAtUtc),
            _ => descending ? records.OrderByDescending(x => x.CreatedAtUtc) : records.OrderBy(x => x.CreatedAtUtc)
        };

        var pageItems = records.Skip((page - 1) * pageSize).Take(pageSize).Select(x => new SessionListItemDto(
            x.SessionId, x.GymId, x.SessionKind, x.MfaVerified, x.CreatedAtUtc, x.LastSeenAtUtc, x.IdleExpiresAtUtc, x.AbsoluteExpiresAtUtc, x.ExpiresAtUtc, x.UserAgent, x.SessionId == currentUser.SessionId)).ToArray();
        return AuthResult<SessionPageDto>.Success(new SessionPageDto(pageItems, page, pageSize, total, page * pageSize < total));
    }

    public async Task<AuthResult<SimpleRevocationDto>> RevokeOwnedSessionAsync(AuthenticatedUser currentUser, Guid sessionId, string? reason, AuthRequestContext context, CancellationToken cancellationToken = default)
    {
        var permission = await RequirePermissionAsync<SimpleRevocationDto>(currentUser, "auth.sessions.revoke", context, cancellationToken);
        if (permission is not null)
        {
            return permission;
        }

        var session = await sessions.FindOwnedByIdAsync(currentUser.UserId, sessionId, cancellationToken);
        if (session is null || session.GymId != currentUser.GymId)
        {
            await AuditAsync(context, currentUser.UserId, "iam.session", sessionId, "authz.session_scope_denied", "failure", "owned_session_required", cancellationToken);
            return Failure<SimpleRevocationDto>(404, "RESOURCE_NOT_FOUND", "The requested session was not found in the authorized scope.");
        }

        var changed = await sessions.RevokeOwnedAsync(currentUser.UserId, sessionId, cancellationToken);
        await AuditAsync(context, currentUser.UserId, "iam.session", sessionId, changed ? "auth.session.revoked" : "auth.session.revoke_noop", "success", SafeReason(reason), cancellationToken);
        return AuthResult<SimpleRevocationDto>.Success(new SimpleRevocationDto(sessionId, true));
    }

    public async Task<AuthSessionResolution?> ResolveSessionAsync(string rawToken, CancellationToken cancellationToken = default)
    {
        var session = await sessions.FindActiveAsync(rawToken, cancellationToken);
        if (session is null)
        {
            return null;
        }

        var user = await repository.FindUserByIdAsync(session.UserId, cancellationToken);
        if (user is null || user.Status != Active)
        {
            return null;
        }

        if (session.GymId.HasValue && !await repository.IsGymActiveAsync(session.GymId.Value, cancellationToken))
        {
            return null;
        }

        var assignments = await repository.GetAssignmentsAsync(user.UserId, cancellationToken);
        var visible = assignments.Where(x => IsVisibleInSession(x, session.GymId)).ToArray();
        if (visible.Length == 0)
        {
            return null;
        }

        await sessions.TouchAsync(session.SessionId, cancellationToken);
        var permissions = visible.SelectMany(x => x.Permissions).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var actor = new AuthenticatedUser(user.UserId, session.GymId, session.MfaVerified, permissions, session.SessionId);
        return new AuthSessionResolution(actor, session);
    }

    public async Task<bool> HasPermissionAsync(AuthenticatedUser currentUser, string permissionKey, Guid? targetGymId = null, bool allowPlatformControlPlane = false, CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsMfaVerified)
        {
            return false;
        }

        if (!targetGymId.HasValue)
        {
            return currentUser.Permissions.Contains(permissionKey);
        }

        if (!await repository.IsGymActiveAsync(targetGymId.Value, cancellationToken))
        {
            return false;
        }

        if (currentUser.GymId == targetGymId && currentUser.Permissions.Contains(permissionKey))
        {
            return true;
        }

        if (!allowPlatformControlPlane)
        {
            return false;
        }

        var assignments = await repository.GetAssignmentsAsync(currentUser.UserId, cancellationToken);
        return assignments.Any(x => x.Status == Active && x.ScopeType == PlatformScope && x.Permissions.Contains(permissionKey));
    }

    public async Task<AuthResult<AccessCatalogDto>> GetAccessCatalogAsync(AuthenticatedUser currentUser, AuthRequestContext context, CancellationToken cancellationToken = default)
    {
        var permission = await RequirePermissionAsync<AccessCatalogDto>(currentUser, "platform.security.manage", context, cancellationToken);
        if (permission is not null)
        {
            return permission;
        }

        var catalog = await repository.GetAccessCatalogAsync(cancellationToken);
        return AuthResult<AccessCatalogDto>.Success(new AccessCatalogDto(catalog.Permissions, catalog.Roles, catalog.RolePermissionAssignmentCount));
    }

    public async Task<AuthResult<AccessUsersPageDto>> ListAccessUsersAsync(AuthenticatedUser currentUser, AccessUsersQuery query, AuthRequestContext context, CancellationToken cancellationToken = default)
    {
        var permission = await RequirePermissionAsync<AccessUsersPageDto>(currentUser, "platform.security.manage", context, cancellationToken);
        if (permission is not null)
        {
            return permission;
        }

        if (query.Page < 1 || query.PageSize is < 1 or > 100 || query.ScopeType is not (null or "gym" or "platform") || query.Status is not (null or "active" or "disabled") || query.SortField is not (null or "createdAtUtc" or "updatedAtUtc" or "email"))
        {
            return Failure<AccessUsersPageDto>(400, "INVALID_FILTER", "The requested access filters are not valid.");
        }

        var effectiveGym = query.GymId ?? currentUser.GymId;
        if (effectiveGym.HasValue)
        {
            if (!await HasPermissionAsync(currentUser, "platform.security.manage", effectiveGym, true, cancellationToken))
            {
                await AuditAsync(context, currentUser.UserId, "iam.authorization", effectiveGym, "authz.scope_denied", "failure", "platform.security.manage", cancellationToken);
                return Failure<AccessUsersPageDto>(403, "GYM_SCOPE_DENIED", "The requested Gym is outside the authorized scope.");
            }
            query = query with { GymId = effectiveGym };
        }
        else if (query.ScopeType == GymScope)
        {
            return Failure<AccessUsersPageDto>(403, "GYM_SCOPE_DENIED", "An explicit Gym scope is required.");
        }
        else if (query.ScopeType is null)
        {
            query = query with { ScopeType = PlatformScope };
        }

        var result = await repository.ListAccessUsersAsync(query, cancellationToken);
        var page = new AccessUsersPageDto(result.Items, query.Page, query.PageSize, result.Total, query.Page * query.PageSize < result.Total);
        return AuthResult<AccessUsersPageDto>.Success(page);
    }

    public async Task<AuthResult<AccessUserDto>> CreateAccessUserAsync(AuthenticatedUser currentUser, AccessUserCreateCommand command, AuthRequestContext context, CancellationToken cancellationToken = default)
    {
        var permission = await RequirePermissionAsync<AccessUserDto>(currentUser, "platform.security.manage", context, cancellationToken);
        if (permission is not null)
        {
            return permission;
        }

        var email = NormalizeEmail(command.Email);
        var emailError = ValidateEmail(email);
        if (emailError is not null || string.IsNullOrWhiteSpace(command.DisplayName) || string.IsNullOrWhiteSpace(command.InitialPassword) || command.RoleId == Guid.Empty)
        {
            return Failure<AccessUserDto>(400, "VALIDATION_ERROR", "Email, display name, initial password, and role are required.");
        }

        var passwordError = ValidatePassword(command.InitialPassword);
        if (passwordError is not null)
        {
            return Failure<AccessUserDto>(422, "PASSWORD_POLICY_VIOLATION", passwordError);
        }

        var duplicate = await repository.FindUserByEmailAsync(email, cancellationToken);
        if (duplicate is not null)
        {
            return Failure<AccessUserDto>(409, "DUPLICATE_RESOURCE", "A user with this email already exists.");
        }

        var catalog = await repository.GetAccessCatalogAsync(cancellationToken);
        var role = catalog.Roles.FirstOrDefault(x => x.RoleId == command.RoleId && x.Status == Active);
        if (role is null || (role.ScopeType == GymScope && !command.GymId.HasValue) || (role.ScopeType == PlatformScope && command.GymId.HasValue))
        {
            return Failure<AccessUserDto>(422, "DOMAIN_RULE_VIOLATION", "The selected role and Gym scope do not match.");
        }

        if (currentUser.GymId.HasValue && role.ScopeType != GymScope)
        {
            await AuditAsync(context, currentUser.UserId, "iam.authorization", null, "authz.scope_denied", "failure", "platform_role_from_gym_scope", cancellationToken);
            return Failure<AccessUserDto>(403, "GYM_SCOPE_DENIED", "A Gym-scoped administrator cannot create a platform-scoped identity.");
        }

        if (command.GymId.HasValue && (!await repository.IsGymActiveAsync(command.GymId.Value, cancellationToken) || !await HasPermissionAsync(currentUser, "platform.security.manage", command.GymId, true, cancellationToken)))
        {
            await AuditAsync(context, currentUser.UserId, "iam.authorization", command.GymId, "authz.scope_denied", "failure", "platform.security.manage", cancellationToken);
            return Failure<AccessUserDto>(403, "GYM_SCOPE_DENIED", "The requested Gym is outside the authorized scope.");
        }

        var created = await repository.CreateAccessUserAsync(email, command.DisplayName.Trim(), passwordHasher.Hash(command.InitialPassword), command.RoleId, command.GymId, DateTime.UtcNow, cancellationToken);
        if (created is null)
        {
            return Failure<AccessUserDto>(409, "DUPLICATE_RESOURCE", "The user could not be created without duplicating an identity.");
        }

        await AuditAsync(context, currentUser.UserId, "iam.user", created.UserId, "iam.user.created", "success", null, cancellationToken);
        return AuthResult<AccessUserDto>.Success(created, 201);
    }

    public async Task<AuthResult<AccessStatusDto>> ChangeUserStatusAsync(AuthenticatedUser currentUser, Guid targetUserId, AccessUserStatusCommand command, byte[]? expectedVersion, AuthRequestContext context, CancellationToken cancellationToken = default)
    {
        var permission = await RequirePermissionAsync<AccessStatusDto>(currentUser, "platform.security.manage", context, cancellationToken);
        if (permission is not null)
        {
            return permission;
        }

        if (targetUserId == currentUser.UserId)
        {
            return Failure<AccessStatusDto>(422, "DOMAIN_RULE_VIOLATION", "An administrator cannot disable their own account.");
        }
        if (command.Status is not (Active or Disabled) || string.IsNullOrWhiteSpace(command.Reason))
        {
            return Failure<AccessStatusDto>(400, "VALIDATION_ERROR", "Status and an administrative reason are required.");
        }
        var scope = await ResolveTargetScopeAsync(currentUser, targetUserId, cancellationToken);
        if (!scope.Authorized)
        {
            await AuditAsync(context, currentUser.UserId, "iam.authorization", scope.GymId, "authz.scope_denied", "failure", "platform.security.manage", cancellationToken);
            return Failure<AccessStatusDto>(scope.GymId.HasValue ? 403 : 404, scope.GymId.HasValue ? "GYM_SCOPE_DENIED" : "RESOURCE_NOT_FOUND", "The target user is outside the authorized scope.");
        }

        var target = await repository.FindUserByIdAsync(targetUserId, cancellationToken);
        if (target is null)
        {
            return Failure<AccessStatusDto>(404, "RESOURCE_NOT_FOUND", "The target user was not found in the authorized scope.");
        }

        var statusChanged = !string.Equals(target.Status, command.Status, StringComparison.OrdinalIgnoreCase);
        if (statusChanged && expectedVersion is null)
        {
            return Failure<AccessStatusDto>(409, "CONCURRENCY_CONFLICT", "If-Match is required for a status transition.");
        }

        var changed = await repository.ChangeUserStatusAsync(targetUserId, command.Status, expectedVersion, DateTime.UtcNow, cancellationToken);
        if (changed is null)
        {
            var current = await repository.FindUserByIdAsync(targetUserId, cancellationToken);
            return Failure<AccessStatusDto>(current is null ? 404 : 409, current is null ? "RESOURCE_NOT_FOUND" : "CONCURRENCY_CONFLICT", "The user status could not be changed safely.");
        }

        await AuditAsync(context, currentUser.UserId, "iam.user", targetUserId, command.Status == Disabled ? "iam.user.disabled" : "iam.user.enabled", "success", SafeReason(command.Reason), cancellationToken);
        return AuthResult<AccessStatusDto>.Success(changed);
    }

    public async Task<AuthResult<AccessAssignmentDto>> EnsureRoleAssignmentAsync(AuthenticatedUser currentUser, Guid targetUserId, Guid roleId, Guid? gymId, byte[]? expectedVersion, RoleAssignmentCommand command, AuthRequestContext context, CancellationToken cancellationToken = default)
    {
        var permission = await RequirePermissionAsync<AccessAssignmentDto>(currentUser, "platform.security.manage", context, cancellationToken);
        if (permission is not null)
        {
            return permission;
        }
        if (targetUserId == currentUser.UserId)
        {
            return Failure<AccessAssignmentDto>(422, "DOMAIN_RULE_VIOLATION", "Self-role modification is not allowed.");
        }
        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return Failure<AccessAssignmentDto>(400, "VALIDATION_ERROR", "An administrative reason is required.");
        }

        var catalog = await repository.GetAccessCatalogAsync(cancellationToken);
        var role = catalog.Roles.FirstOrDefault(x => x.RoleId == roleId);
        if (role is null || role.Status != Active || (role.ScopeType == GymScope && !gymId.HasValue) || (role.ScopeType == PlatformScope && gymId.HasValue))
        {
            return Failure<AccessAssignmentDto>(422, "DOMAIN_RULE_VIOLATION", "The selected role and Gym scope do not match.");
        }

        if (currentUser.GymId.HasValue && role.ScopeType != GymScope)
        {
            await AuditAsync(context, currentUser.UserId, "iam.authorization", gymId, "authz.scope_denied", "failure", "platform_role_from_gym_scope", cancellationToken);
            return Failure<AccessAssignmentDto>(403, "GYM_SCOPE_DENIED", "A Gym-scoped administrator cannot assign a platform-scoped role.");
        }

        if (gymId.HasValue && (!await repository.IsGymActiveAsync(gymId.Value) || !await HasPermissionAsync(currentUser, "platform.security.manage", gymId, true, cancellationToken)))
        {
            await AuditAsync(context, currentUser.UserId, "iam.authorization", gymId, "authz.scope_denied", "failure", "platform.security.manage", cancellationToken);
            return Failure<AccessAssignmentDto>(403, "GYM_SCOPE_DENIED", "The requested Gym is outside the authorized scope.");
        }

        var target = await repository.FindUserByIdAsync(targetUserId, cancellationToken);
        if (target is null)
        {
            return Failure<AccessAssignmentDto>(404, "RESOURCE_NOT_FOUND", "The target user was not found.");
        }
        if (target.Status != Active)
        {
            return Failure<AccessAssignmentDto>(422, "DOMAIN_RULE_VIOLATION", "Roles may only be assigned to active users.");
        }

        if (currentUser.GymId.HasValue
            && !(await repository.GetAssignmentsAsync(targetUserId, cancellationToken))
                .Any(x => x.Status == Active && x.ScopeType == GymScope && x.GymId == currentUser.GymId))
        {
            await AuditAsync(context, currentUser.UserId, "iam.authorization", targetUserId, "authz.scope_denied", "failure", "target_user_outside_gym_scope", cancellationToken);
            return Failure<AccessAssignmentDto>(403, "GYM_SCOPE_DENIED", "The target user is outside the authorized Gym scope.");
        }

        var existing = (await repository.GetAssignmentsAsync(targetUserId, cancellationToken)).FirstOrDefault(x => x.RoleId == roleId && x.GymId == gymId && x.ScopeType == role.ScopeType);
        if (existing is not null && existing.Status != Active && expectedVersion is null)
        {
            return Failure<AccessAssignmentDto>(409, "CONCURRENCY_CONFLICT", "If-Match is required to reactivate an assignment.");
        }
        var outcome = await repository.EnsureRoleAssignmentAsync(targetUserId, roleId, gymId, expectedVersion, DateTime.UtcNow, cancellationToken);
        if (outcome.VersionConflict)
        {
            return Failure<AccessAssignmentDto>(409, "CONCURRENCY_CONFLICT", "The role assignment changed before it could be updated.");
        }
        if (outcome.Assignment is null)
        {
            return Failure<AccessAssignmentDto>(404, "RESOURCE_NOT_FOUND", "The role assignment target was not found.");
        }

        await AuditAsync(context, currentUser.UserId, "iam.user_gym_role", outcome.Assignment.AssignmentId, outcome.Outcome == "reactivated" ? "iam.role_assignment.reactivated" : outcome.Outcome == "created" ? "iam.role_assignment.created" : "iam.role_assignment.noop", "success", SafeReason(command.Reason), cancellationToken);
        return AuthResult<AccessAssignmentDto>.Success(outcome.Assignment);
    }

    public async Task<AuthResult<RoleRevocationDto>> RevokeRoleAssignmentAsync(AuthenticatedUser currentUser, Guid targetUserId, Guid assignmentId, byte[]? expectedVersion, RoleRevocationCommand command, AuthRequestContext context, CancellationToken cancellationToken = default)
    {
        var permission = await RequirePermissionAsync<RoleRevocationDto>(currentUser, "platform.security.manage", context, cancellationToken);
        if (permission is not null)
        {
            return permission;
        }
        if (targetUserId == currentUser.UserId)
        {
            return Failure<RoleRevocationDto>(422, "DOMAIN_RULE_VIOLATION", "Self-role modification is not allowed.");
        }
        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return Failure<RoleRevocationDto>(400, "VALIDATION_ERROR", "An administrative reason is required.");
        }

        var assignment = (await repository.GetAssignmentsAsync(targetUserId, cancellationToken)).FirstOrDefault(x => x.AssignmentId == assignmentId);
        if (assignment is null)
        {
            return Failure<RoleRevocationDto>(404, "RESOURCE_NOT_FOUND", "The role assignment was not found in the authorized scope.");
        }
        if (!await IsAssignmentInAuthorizedManagementScopeAsync(currentUser, assignment, cancellationToken))
        {
            await AuditAsync(context, currentUser.UserId, "iam.authorization", assignment.AssignmentId, "authz.scope_denied", "failure", "role_assignment_outside_scope", cancellationToken);
            return Failure<RoleRevocationDto>(403, "GYM_SCOPE_DENIED", "The role assignment is outside the authorized scope.");
        }

        var result = await repository.RevokeRoleAssignmentAsync(targetUserId, assignmentId, expectedVersion, DateTime.UtcNow, cancellationToken);
        if (result.VersionConflict)
        {
            return Failure<RoleRevocationDto>(409, "CONCURRENCY_CONFLICT", "The role assignment changed before it could be revoked.");
        }
        if (!result.Found)
        {
            return Failure<RoleRevocationDto>(404, "RESOURCE_NOT_FOUND", "The role assignment was not found in the authorized scope.");
        }

        await AuditAsync(context, currentUser.UserId, "iam.user_gym_role", assignmentId, result.Changed ? "iam.role_assignment.revoked" : "iam.role_assignment.revoke_noop", "success", SafeReason(command.Reason), cancellationToken);
        return AuthResult<RoleRevocationDto>.Success(new RoleRevocationDto(assignmentId, true));
    }

    private async Task<(Guid? GymId, string ScopeType)?> SelectLoginScopeAsync(Guid userId, CancellationToken cancellationToken)
    {
        var assignments = await repository.GetAssignmentsAsync(userId, cancellationToken);
        foreach (var assignment in assignments.Where(x => x.Status == Active && x.ScopeType == GymScope && x.GymId.HasValue).OrderBy(x => x.GymId))
        {
            if (await repository.IsGymActiveAsync(assignment.GymId!.Value, cancellationToken))
            {
                return (assignment.GymId, GymScope);
            }
        }

        return assignments.Any(x => x.Status == Active && x.ScopeType == PlatformScope && x.Permissions.Count > 0)
            ? (null, PlatformScope)
            : null;
    }

    private async Task<SessionScope?> GetSessionScopeAsync(SessionRecord session, CancellationToken cancellationToken)
    {
        if (session.GymId.HasValue && !await repository.IsGymActiveAsync(session.GymId.Value, cancellationToken))
        {
            return null;
        }

        var assignments = await repository.GetAssignmentsAsync(session.UserId, cancellationToken);
        return assignments.Any(x => IsVisibleInSession(x, session.GymId))
            ? new SessionScope(session.GymId, session.GymId.HasValue ? GymScope : PlatformScope)
            : null;
    }

    private async Task<bool> VerifyFactorAsync(Guid userId, AuthMfaRecord factor, string method, string code, CancellationToken cancellationToken)
    {
        if (method == "recovery_code")
        {
            return await repository.ConsumeRecoveryCodeAsync(userId, HashOpaque(code.Trim().ToLowerInvariant()), DateTime.UtcNow, cancellationToken);
        }

        try
        {
            return totp.Verify(secretProtector.Unprotect(factor.SecretRef), code.Trim(), DateTimeOffset.UtcNow);
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private async Task<bool> VerifyStepUpAsync(Guid userId, string? password, string? code, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(password))
        {
            var credential = await repository.FindPasswordCredentialAsync(userId, cancellationToken);
            if (credential is not null && passwordHasher.Verify(credential.SecretHash, password))
            {
                return true;
            }
        }

        if (!string.IsNullOrWhiteSpace(code))
        {
            var factor = await repository.FindEnabledMfaAsync(userId, cancellationToken);
            if (factor is not null && await VerifyFactorAsync(userId, factor, "totp", code, cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<AuthResult<T>?> RequirePermissionAsync<T>(AuthenticatedUser currentUser, string permissionKey, AuthRequestContext context, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(currentUser, permissionKey, currentUser.GymId, false, cancellationToken))
        {
            await AuditAsync(context, currentUser.UserId, "iam.authorization", null, "authz.permission_denied", "failure", permissionKey, cancellationToken);
            return Failure<T>(403, "PERMISSION_DENIED", "The authenticated user is not authorized for this operation.");
        }

        return null;
    }

    private async Task<(bool Authorized, Guid? GymId)> ResolveTargetScopeAsync(AuthenticatedUser currentUser, Guid targetUserId, CancellationToken cancellationToken)
    {
        var assignments = (await repository.GetAssignmentsAsync(targetUserId, cancellationToken)).Where(x => x.Status == Active).ToArray();
        if (currentUser.GymId.HasValue)
        {
            return (assignments.Any(x => x.ScopeType == GymScope && x.GymId == currentUser.GymId), currentUser.GymId);
        }

        var first = assignments.FirstOrDefault();
        if (first is null)
        {
            return (false, null);
        }

        var authorized = await HasPermissionAsync(currentUser, "platform.security.manage", first.GymId, true, cancellationToken);
        return (authorized, first.GymId);
    }

    private async Task<bool> IsAssignmentInAuthorizedManagementScopeAsync(AuthenticatedUser currentUser, AuthAssignmentRecord assignment, CancellationToken cancellationToken)
    {
        if (currentUser.GymId.HasValue)
        {
            return assignment.ScopeType == GymScope
                && assignment.GymId == currentUser.GymId
                && currentUser.Permissions.Contains("platform.security.manage");
        }

        if (assignment.ScopeType == PlatformScope)
        {
            return currentUser.Permissions.Contains("platform.security.manage");
        }

        return assignment.GymId.HasValue
            && await HasPermissionAsync(currentUser, "platform.security.manage", assignment.GymId, true, cancellationToken);
    }

    private static bool IsVisibleInSession(AuthAssignmentRecord assignment, Guid? gymId)
        => assignment.Status == Active && ((gymId.HasValue && assignment.ScopeType == GymScope && assignment.GymId == gymId) || (!gymId.HasValue && assignment.ScopeType == PlatformScope));

    private static AuthUserDto ToUserDto(AuthUserRecord user)
        => new(user.UserId, user.Email, user.DisplayName, user.Status, user.LastLoginAtUtc, EncodeVersion(user.RowVersion));

    private static AuthSessionDto ToSessionDto(SessionCreated session, AuthUserRecord user, bool requiresMfa, string? challenge)
        => new(session.RawToken, session.SessionId, requiresMfa, challenge, !requiresMfa, session.ExpiresAtUtc, session.IdleExpiresAtUtc, session.AbsoluteExpiresAtUtc, ToUserDto(user));

    private async Task AuditAsync(AuthRequestContext context, Guid? actorUserId, string targetType, Guid? targetId, string action, string result, string? reason, CancellationToken cancellationToken)
        => await repository.WriteAuditAsync(new AuditEntry(context.RequestId, actorUserId, targetType, targetId, action, result, SafeReason(reason)), cancellationToken);

    private static string? ValidateEmail(string email)
        => email.Length is < 3 or > 320 || email.Contains(' ') || email.IndexOf('@') <= 0 || email.LastIndexOf('@') != email.IndexOf('@') || email.EndsWith('@') ? "invalid" : null;

    private static string? ValidatePassword(string password)
        => password.Length < PasswordMinimumLength ? $"Password must contain at least {PasswordMinimumLength} characters." : password.Length > PasswordMaximumLength ? "Password is too long." : null;

    private static string NormalizeEmail(string? email) => email?.Trim().ToLowerInvariant() ?? string.Empty;

    private static string CreateOpaqueToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    private static string HashOpaque(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static string EncodeVersion(byte[]? value) => Convert.ToBase64String(value is { Length: > 0 } ? value : [0]);

    private static string? SafeReason(string? reason)
        => string.IsNullOrWhiteSpace(reason) ? null : reason.Trim()[..Math.Min(reason.Trim().Length, 500)];

    private static string FailureAction(string method) => method == "recovery_code" ? "auth.mfa.recovery_code.failed" : "auth.mfa.totp.verification_failed";
    private static string SuccessAction(string method) => method == "recovery_code" ? "auth.mfa.recovery_code.used" : "auth.mfa.totp.verification_succeeded";

    private static AuthResult<T> Failure<T>(int statusCode, string code, string message, IReadOnlyList<LogicFit.Shared.ApiFieldError>? fields = null)
        => AuthResult<T>.Failure(statusCode, code, message, fields);

    private sealed record SessionScope(Guid? GymId, string ScopeType);
}
