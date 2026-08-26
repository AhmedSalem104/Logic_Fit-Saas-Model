# Session Implementation

**Status:** IMPLEMENTED — SQL-backed session tests passing

## Transport

The API accepts one opaque bearer session value using the `LogicFitSession` authentication scheme. The raw value is issued only in the protected success response needed by the client and is never persisted, logged, or returned by session-list APIs.

## Persistence

`SqlSessionStore` persists the SHA-256 token hash in Control Plane `iam.sessions` with:

- user and optional Gym scope;
- `staff` or `mfa_pending` session kind;
- MFA verification state;
- created, last-seen, idle-expiry, absolute-expiry, and effective-expiry timestamps;
- revocation and audit timestamps;
- truncated user-agent/IP metadata.

## Lifecycle

| Operation | Behavior |
|---|---|
| Login | Creates a new session; MFA-enabled users start pending with the approved 300-second challenge lifetime. |
| Refresh | Finds the active session, revokes it, and creates a replacement with a new raw token. |
| Request | Touches an active session and recomputes idle expiry without extending the absolute lifetime; pending MFA sessions are not extended beyond the challenge expiry. |
| MFA | Marks the same pending session verified after a valid TOTP/recovery-code check and restores the remaining session lifetime under the absolute policy. |
| Logout | Revokes only the current session. |
| Own-session revoke | Requires ownership, scope, and `auth.sessions.revoke`; repeated authorized revoke is idempotent. |
| Password change/reset | Revokes all user sessions. |
| User disablement | Revokes all sessions for the target user. |

## Session listing safety

`GET /api/v1/auth/sessions` returns safe metadata only and is limited to the current user plus the current/explicitly authorized Gym scope. It never returns raw tokens, token hashes, IP addresses, database names, password data, or other users' sessions.

## Tests

`AuthContinuationApiTests` covers refresh rotation, old-token rejection, list redaction, ownership/repeat revocation, expiration, password invalidation, and pending-MFA restrictions.
