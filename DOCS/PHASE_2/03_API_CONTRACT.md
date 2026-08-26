# Phase 2 API Contract

**API style:** versioned REST over JSON  
**Canonical base:** `/api/v1` (**ARCHITECTURE REQUIREMENT proposed by this contract; not copied from TOP GYM**)  
**Status:** design contract; no routes implemented.

## Authority and client rule

React Web and Flutter use the same contracts. The API is the business authority for validation, permissions, calculations, state transitions, persistence, and Gym isolation. Client-side validation is UX only.

## Request context

### Authenticated Gym request

```text
Authorization: Bearer <access-token>
X-Request-Id: <client/generated-or-server-id>
```

The canonical Gym context is the path segment `/gyms/{gymId}`. A client may send a display header, but the server ignores it for authorization and resolves the target from the route plus the actor's granted scope. The target Gym is checked before repository access.

### Platform request

Platform routes use the authenticated actor's Control Plane scope. A target `gymId` in a request is a resource identifier, not permission; every target is authorized independently.

### Public QR request

`GET /api/v1/qr/{token}` is unauthenticated by design and follows `16_QR_CONTRACT.md`: opaque token only, generic invalid result, rate limit, `Cache-Control: no-store`, and minimal public-safe response. `GET` is chosen because lookup has no state-changing side effect; issuance/rotation/revocation remain authenticated mutations.

## Standard response envelope

Success:

```json
{
  "data": {},
  "meta": { "requestId": "...", "version": "v1" }
}
```

Collection:

```json
{
  "data": [],
  "meta": {
    "requestId": "...",
    "page": 1,
    "pageSize": 25,
    "total": 0,
    "hasNext": false
  }
}
```

Error:

```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Safe human-readable message",
    "fieldErrors": [{ "field": "name", "code": "REQUIRED" }]
  },
  "meta": { "requestId": "...", "version": "v1" }
}
```

## HTTP and error contract

| HTTP | Error code family | Meaning |
|---:|---|---|
| 400 | `VALIDATION_ERROR`, `INVALID_FILTER`, `INVALID_STATE_TRANSITION` | Request shape or domain validation failed. |
| 401 | `AUTHENTICATION_REQUIRED`, `SESSION_INVALID` | No valid authenticated session. |
| 403 | `PERMISSION_DENIED`, `GYM_SCOPE_DENIED` | Actor lacks exact permission/scope. |
| 404 | `RESOURCE_NOT_FOUND` | Resource is absent within the authorized scope; do not leak cross-Gym existence. |
| 409 | `CONCURRENCY_CONFLICT`, `DUPLICATE_RESOURCE`, `PUBLISH_CONFLICT` | Version/idempotency/state conflict. |
| 422 | `DOMAIN_RULE_VIOLATION`, `CALCULATION_INVALID`, `CANONICAL_REFERENCE_INVALID` | Shape is valid but business/domain rule fails. |
| 429 | `RATE_LIMITED` | Public QR/auth/expensive operation throttled. |
| 500/503 | `INTERNAL_ERROR`, `DEPENDENCY_UNAVAILABLE` | Safe error only; request ID supports diagnostics. |

## Collection query contract

Where applicable, collections accept:

```text
page >= 1
pageSize in 1..100
search
status
from / to
sort=<approved-field>:asc|desc
```

Each endpoint documents its allowed filter/sort fields. Unknown fields return `INVALID_FILTER`; no SQL fragments are accepted from clients.

## Mutation contract

- `Idempotency-Key` is required for retryable create/generate/publish/provision/migration/backup/restore operations.
- Updates require `If-Match`/opaque version when the entity carries `rowversion`.
- Backend revalidates the current state in the same transaction as the transition.
- Successful state-changing operations emit an audit event where the endpoint catalog marks `audit=required`.
- API responses contain DTOs, never password hashes, secrets, raw QR tokens, storage credentials, or unrestricted internal metadata.

## Permission contract

Every authenticated endpoint has an exact permission in `09_PERMISSION_CONTRACT.md`; a route is never authorized only by a role label or frontend visibility. Public QR is the sole unauthenticated endpoint in this package and is constrained by its privacy contract.

## Data and use-case layering

```text
REST route/controller
  → request schema + authorization
  → application use case
  → domain service / calculation / state machine
  → repository
  → Control Plane or selected Gym SQL Server database
```

The endpoint catalog names the use case, service boundary, and tables touched. The exact framework/library is an implementation note for Phase 4 and is not a contract dependency.

## Final gap-resolution API policy — 2026-08-25

### Authentication and portal

- Staff/platform authentication uses SQL-backed server sessions. Login, refresh where transport requires it, logout, invalidation, password reset, MFA, and recovery are server-authoritative and rate-limited as applicable.
- Password reset request/complete never returns or logs a raw reset token. TOTP is the primary MFA mechanism; Email OTP is not a primary factor. MFA and reset actions emit redacted audit events.
- Member Portal access is a separate public/rate-limited member-code exchange that creates a scoped portal session. It does not create a member username/password identity and it does not grant staff permissions.

### Domain decisions now part of the API contract

- Finance is operational Gym finance only; money is explicit `EGP` by default, configurable per Gym, and uses server-side SQL `DECIMAL(19,4)` values. Full/partial refunds require permission, reason, original link, and audit.
- Store uses Weighted Average Cost, explicit tax default `0%`, configurable payment methods, disabled-by-default credit sales, explicit sale status, and transactional stock/return effects.
- Classes use one-time/weekly recurrence with boundaries, configurable capacity, FIFO waitlist, separate no-show, and a two-hour default cancellation cutoff.
- CRM uses the six canonical default stages and owned/due/completable follow-ups; overdue is server-derived and visible.
- Documents use `StorageAdapter`; notifications use the local in-app adapter; reports are on-demand/server-side; monitoring thresholds and backup/DR targets are Control Plane operational contracts.

The client must treat all these values as API data. React and Flutter cannot calculate authoritative totals, state transitions, permission decisions, or cross-Gym routing.

## Phase 4 foundation addendum — 2026-08-25

Only infrastructure endpoints are implemented at this stage: health, readiness, version, and safe development diagnostics. The Phase 2 business endpoint catalog remains design-only until its corresponding vertical slice is authorized.

## Phase 5B Authentication/RBAC addendum — 2026-08-26

The approved `21_AUTH_RBAC_API_CONTRACT_ADDENDUM.md` extends this same `/api/v1` envelope, error, permission, scope, idempotency, concurrency, and secret-redaction contract for the Phase 5B Authentication/RBAC operations. It does not create a second API contract or change the client boundary.

Password reset uses only the locked hyphenated routes `POST /api/v1/auth/password-reset/request` and `POST /api/v1/auth/password-reset/complete`. A slash-separated route form is non-canonical and must not be implemented or referenced by clients, tests, or documentation.
