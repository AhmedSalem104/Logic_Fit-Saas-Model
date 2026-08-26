# Authenticator App / TOTP MFA Policy

The primary MFA mechanism is Authenticator App / TOTP. Email OTP is not the primary mechanism and no paid external provider is required for local development.

## Foundation behavior

- enrollment creates a random secret and an `otpauth://` provisioning URI;
- the secret is not logged and must not be returned after enrollment is completed;
- verification uses six digits, a 30-second period, RFC 6238 HMAC-SHA1, and a small clock-skew window;
- enabling MFA requires successful verification;
- disabling MFA requires the approved permission and a fresh security check;
- backup/recovery codes are random, single-use, revocable, and regenerable;
- TOTP secrets and recovery-code values are never stored or logged in plaintext.

## Audit events

The full slice must audit enrollment, enablement, disablement, failed/successful verification where approved, and recovery-code use/regeneration. Administrative MFA reset is deferred as a later security feature; it is not part of Phase 5B. Event records contain actor, target, Gym/platform scope, request ID, and outcome, never secrets.

The native `TotpService`, `RecoveryCodeGenerator`, and `AesGcmSecretProtector` are consumed by the implemented enrollment, verification, disablement, and recovery-code application flows. Web and Flutter MFA states are implemented; the remaining release gate is fresh live UAT against the local fixture.

## Phase 5B API addendum synchronization — 2026-08-26

Recovery-code verification uses the existing `POST /api/v1/auth/mfa/verify` contract with the explicit `method=recovery_code` discriminator; there is no second recovery verification endpoint. Administrative reset of another user's MFA factor is explicitly **DEFERRED — LATER ADMIN SECURITY FEATURE** because no approved Phase 5B route, dedicated permission, or step-up contract exists. Self-service enrollment, verification, disablement, regeneration, one-time recovery-code use, and redacted audit remain in scope.
