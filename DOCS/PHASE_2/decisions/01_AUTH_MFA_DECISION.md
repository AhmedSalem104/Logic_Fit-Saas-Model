# Decision 2.01 — Authentication and MFA

**Status:** APPROVED  
**Authority:** Phase 2 Final Gap Resolution  
**Scope:** Platform and Gym staff authentication.

## Decision

- Authentication is server-side and session-based.
- Sessions are persisted in SQL Server; clients receive only the session credential required by the transport and never the session secret in logs.
- Passwords use a secure, adaptive password-hashing adapter. The concrete library, cost parameters, and password-complexity values are **IMPLEMENTATION NOTE** items; plaintext and reversible storage are forbidden.
- Login is rate-limited. Sessions expire, can be invalidated on logout or security action, and are audited without sensitive credential material.
- Password reset uses single-use, expiring, hash-only reset tokens. Raw reset tokens are never persisted or logged.
- MFA is Authenticator App/TOTP. Email OTP is not the primary MFA mechanism.
- TOTP supports enroll, verify, disable, recovery, and backup/recovery codes. TOTP secrets are encrypted or stored through a protected secret reference; raw secrets are never plaintext.
- MFA changes, reset requests/completions, login outcomes, logout, invalidation, and recovery actions are auditable with redaction.

## Contract impact

`iam.sessions`, credentials, MFA factors, password-reset tokens, recovery codes, auth endpoints, server permissions, and authentication flows must implement this policy. Authorization is always server-enforced and permission-based.
