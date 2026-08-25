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

The full slice must audit enrollment, enablement, disablement, failed/successful verification where approved, recovery-code use/regeneration, and authorized MFA reset. Event records contain actor, target, Gym/platform scope, request ID, and outcome, never secrets.

The native `TotpService` and `RecoveryCodeGenerator` provide primitives only. Enrollment, verification endpoints, UI, and UAT remain pending.
