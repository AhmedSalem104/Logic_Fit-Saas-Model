# Authentication/RBAC API Implementation

**Status:** IMPLEMENTED — approved route and envelope tests passing

## Route set

The controllers expose exactly the approved `/api/v1` routes listed below. There is no alternate slash-based password-reset route:

| Controller | Routes |
|---|---|
| `AuthController` | login, refresh, logout, me, mfa/verify, mfa/enroll, mfa/disable, mfa/recovery-codes/regenerate, password-reset/request, password-reset/complete, password/change, sessions, sessions/{sessionId}/revoke |
| `AccessController` | catalog, users list/create/status, role assignment, role revocation |

## HTTP behavior

- `401`: no valid authenticated session or safe credential/MFA failure;
- `403`: authenticated but missing exact permission or outside target scope;
- `400`: malformed/invalid request or collection filter;
- `404`: safe absence for an authorized ownership/scope lookup;
- `409`: concurrency or uniqueness conflict;
- `422`: password/security/domain rule violation;
- `429`: ASP.NET Core rate limiter rejection.

All successful and error responses use `AuthApiResults` and the existing LogicFit response envelope. `X-Request-Id` is included in metadata and audit context.

## Validation and concurrency

Administrative status and role-revocation transitions accept the approved opaque `If-Match` row-version. User creation and role assignment use transactional uniqueness/scope validation. Repeated active role assignment and repeated authorized session/role revocation are handled according to the locked idempotency contract.

## API test map

- `AuthenticationApiTests.cs`: login, safe failures, inactive/revoked scope, unauthenticated access, rate limiting, malformed request.
- `AuthContinuationApiTests.cs`: refresh/logout, password, reset-route reconciliation, TOTP/recovery, sessions, expiry, audit redaction.
- `AccessControlApiTests.cs`: catalog counts, listing, create/status, role assignment/revocation, denial and tenant boundary behavior.
