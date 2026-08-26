# MFA/TOTP Implementation

**Status:** IMPLEMENTED — TOTP, recovery-code, and redaction tests passing

## Canonical mechanism

MFA uses Authenticator App/TOTP. Email OTP and paid providers are not used.

The single verification endpoint is:

`POST /api/v1/auth/mfa/verify`

The request accepts `method: "totp" | "recovery_code"`; omitted method defaults to `totp`. No separate recovery-code verification route exists.

## Enrollment

`POST /api/v1/auth/mfa/enroll` creates a pending TOTP factor and returns the provisioning data only for the enrollment interaction. The secret is encrypted with `AesGcmSecretProtector` before persistence. The factor becomes active only after a valid TOTP verification.

## Verification

- Login creates an `mfa_pending` session when an active factor exists.
- The challenge must be bound to the same authenticated pending session and bearer transport.
- A successful TOTP verification changes the session to `staff`/verified and updates last-login time.
- Enrollment verification activates the pending factor and generates recovery-code hashes.
- A recovery code is consumed atomically once; a second use is rejected.

## Recovery codes

Ten cryptographically random recovery codes are generated. Only SHA-256 hashes are stored. Regeneration revokes old codes and returns the new values only in the one-time success response. Disablement revokes factor and recovery material.

## Step-up and audit

MFA disablement and recovery-code regeneration require the approved permission plus a current password or authenticator-code step-up. Audit events record outcome/category only and never contain secrets.

## Implementation map

- `src/LogicFit.Infrastructure/Security/TotpService.cs`
- `src/LogicFit.Infrastructure/Security/RecoveryCodeGenerator.cs`
- `src/LogicFit.Infrastructure/Security/AesGcmSecretProtector.cs`
- `src/LogicFit.Application/Authentication/AuthenticationService.cs`
- `tests/LogicFit.UnitTests/SecurityPrimitivesTests.cs`
- `tests/LogicFit.ApiTests/AuthContinuationApiTests.cs`

## Verification update — 2026-08-26

The pending MFA session uses the approved 300-second challenge lifetime and is not extended by ordinary request touching. Successful TOTP or recovery-code verification completes the same pending session under the normal absolute session policy. Direct Chrome verification covered enrollment, TOTP verification, recovery-code verification, one-time reuse rejection, and the protected security actions without exposing a secret in the browser console or API response.
