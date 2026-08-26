# SQL-Backed Session Policy

Sessions are server-side records in the Control Plane `iam.sessions` table. A session cookie/token must be opaque and contain no sensitive user or permission payload.

Current approved local configuration defaults:

| Policy | Value |
|---|---:|
| Idle timeout | 1,800 seconds (30 minutes) |
| Absolute lifetime | 28,800 seconds (8 hours) |
| MFA challenge lifetime | 300 seconds (5 minutes) |
| Password reset token lifetime | 900 seconds (15 minutes) |

Persisted session state includes user, optional Gym, hashed token, session kind, MFA state, idle/absolute expiry, last-seen, revocation, user-agent/IP metadata, and audit timestamps. Raw tokens are never stored.

Implemented lifecycle behavior:

- create after credential validation;
- mark MFA-pending until MFA is satisfied where required;
- reject expired, idle-expired, absolute-expired, inactive, or revoked sessions;
- invalidate on logout;
- revoke appropriately after password changes, reset, account disablement, or security administration;
- support permission-protected session listing/revocation;
- audit security-sensitive transitions without logging secrets.

Implementation: `src/LogicFit.Infrastructure/Security/SqlSessionStore.cs` and `src/LogicFit.Application/Authentication/AuthenticationService.cs`. Refresh rotates the bearer value, password changes/resets revoke all sessions, and pending MFA sessions cannot access `auth/me` until the challenge is completed.

## Phase 5B API addendum synchronization — 2026-08-26

The canonical self-service session routes are `GET /api/v1/auth/sessions` and `POST /api/v1/auth/sessions/{sessionId}/revoke`. Results are limited to the current user and current/explicitly authorized Gym scope; raw tokens and hashes are never returned. `/api/v1/auth/logout` remains the current-session logout route.
