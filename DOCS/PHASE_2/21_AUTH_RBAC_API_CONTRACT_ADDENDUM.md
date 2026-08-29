# Authentication/RBAC API Contract Addendum

**Decision date:** 2026-08-26  
**Status:** APPROVED CONTRACT ADDENDUM  
**Scope:** Phase 5B Authentication, sessions, password management, MFA, RBAC, Gym isolation, and audit only.  
**Canonical API base:** `/api/v1`  
**Related decision record:** `decisions/21_AUTH_RBAC_API_DECISIONS.md`

## Purpose

This addendum closes only the Authentication/RBAC contract gaps identified by the Phase 5B Step 0 traceability checkpoint. It extends the existing Phase 2 API catalog without removing or redefining any existing endpoint. It is a contract document, not an implementation authorization by itself.

The existing catalog remains authoritative for login, refresh, logout, actor resolution, password reset, TOTP enrollment/verification/disablement, and recovery-code regeneration. This document adds the missing Phase 5B operations and makes one explicit extension to the existing MFA verification request.

## Password reset route reconciliation — 2026-08-26

The canonical password-reset routes are the existing locked Phase 2 hyphenated routes:

- `POST /api/v1/auth/password-reset/request`
- `POST /api/v1/auth/password-reset/complete`

The slash-separated form is invalid and is not part of the LogicFit API. This records the approved contract over inconsistent implementation wording; it does not add or rename an endpoint.

## Common contract rules

- All routes below are under `/api/v1`.
- React Web and Flutter use the same API. Neither client connects to SQL Server.
- The API is the authority for credentials, sessions, MFA state, permissions, Gym scope, status transitions, and audit.
- Authenticated requests use the existing opaque server-session bearer transport and `X-Request-Id` convention from `03_API_CONTRACT.md`. No JWT or second session mechanism is introduced.
- The existing success, collection, and error envelopes remain unchanged.
- Existing HTTP error families remain authoritative: `400` validation/state, `401` authentication/session, `403` permission/scope, `404` authorized-scope absence, `409` conflict, `422` domain/security rule, and `429` rate limiting.
- Error messages are safe and do not disclose account existence, session existence outside the authorized scope, password correctness beyond the authenticated operation's safe result, MFA factor details, or role/permission data outside the actor's scope.
- No response, log, audit metadata, or error contains a password, password hash, reset token, TOTP secret, recovery-code value, raw session token, database credential, or unrestricted private metadata.
- `Authorization` is enforced by exact permission and resource scope on the server. Role labels and client-side visibility are not authorization.
- Control Plane routes may inspect authorized target metadata through the registry, but never accept a caller-supplied database name or connection string.

## Phase 5B scope classification

| Capability | Classification | Contract result |
|---|---|---|
| Authenticated password change | **A - required** | New endpoint in this addendum. |
| MFA recovery-code verification | **A - required** | Extend existing `/auth/mfa/verify`; no second verification endpoint. |
| Authenticated session listing | **A - required** | New endpoint in this addendum. |
| Revoke an owned session | **A - required** | New endpoint in this addendum; current-session logout remains `/auth/logout`. |
| Role assignment/revocation | **A - required** | New Control Plane access endpoints in this addendum. |
| User/Gym scope management | **A - required** | Included in access list/create/status/role-assignment contracts. |
| User creation and status | **A - required** | Minimum access-administration operations only. |
| Role-permission catalog mutation | **C - later** | No endpoint in Phase 5B; the 15/3/14 canonical catalog remains seeded/read-only here. |
| Administrative MFA reset | **D - not required for Phase 5B** | Explicitly deferred as a later admin security feature; no endpoint is added. |

## Existing endpoints reused without duplication

The following existing Phase 2 routes are reused exactly as cataloged:

| Method | Route | Phase 5B use |
|---|---|---|
| POST | `/auth/login` | Credential validation and initial/mfa-pending session. |
| POST | `/auth/refresh` | Session rotation where the transport requires it. |
| POST | `/auth/logout` | Current authenticated session invalidation. |
| GET | `/auth/me` | Current user, authorized scopes, and permissions. |
| POST | `/auth/mfa/verify` | TOTP verification, extended below to explicitly support recovery codes. |
| POST | `/auth/password-reset/request` | Generic, rate-limited reset request. |
| POST | `/auth/password-reset/complete` | Single-use reset completion and session invalidation. |
| POST | `/auth/mfa/enroll` | TOTP enrollment/provisioning. |
| POST | `/auth/mfa/disable` | Self-service TOTP disable with permission and step-up. |
| POST | `/auth/mfa/recovery-codes/regenerate` | Recovery-code rotation. |

## New endpoint catalog

| Method / route | Use case | Authentication / permission / scope | DB entities | Audit |
|---|---|---|---|---|
| `POST /auth/password/change` | Change the authenticated user's password | Authenticated; `auth.password.change`; current-user self scope | CP `iam.users`, `iam.credentials`, `iam.sessions` | Required: success/failure, no password metadata |
| `GET /auth/sessions` | List safe active sessions | Authenticated; `auth.sessions.view`; current user's authorized session scope only | CP `iam.sessions`, `iam.users` | No audit for ordinary read; access logging remains redacted |
| `POST /auth/sessions/{sessionId}/revoke` | Revoke one owned session | Authenticated; `auth.sessions.revoke`; session owner and same resolved scope | CP `iam.sessions` | Required: success/denial |
| `GET /platform/access/catalog` | Read canonical roles/permissions matrix | Authenticated; `platform.security.manage`; Control Plane | CP `iam.roles`, `iam.permissions`, `iam.role_permissions` | No mutation audit; access log redacted |
| `GET /platform/access/users` | List users and safe assignments | Authenticated; `platform.security.manage`; Control Plane target scope | CP `iam.users`, `iam.user_gym_roles`, `iam.roles` | No audit for ordinary read |
| `POST /platform/access/users` | Create one user with one initial role assignment | Authenticated; `platform.security.manage`; Control Plane; requested role/Gym scope checked | CP `iam.users`, `iam.credentials`, `iam.user_gym_roles` | Required: create success/failure |
| `PATCH /platform/access/users/{userId}/status` | Activate or disable one user | Authenticated; `platform.security.manage`; target scope checked | CP `iam.users`, `iam.sessions` | Required: status change and denial |
| `PUT /platform/access/users/{userId}/role-assignments/{roleId}` | Ensure one active role assignment | Authenticated; `platform.security.manage`; target role/Gym scope checked | CP `iam.user_gym_roles`, `iam.users`, `iam.roles`, `platform.gyms` | Required when assignment is created/reactivated |
| `POST /platform/access/users/{userId}/role-assignments/{assignmentId}/revoke` | Revoke one role assignment | Authenticated; `platform.security.manage`; assignment owner/target scope checked | CP `iam.user_gym_roles`, `iam.users` | Required: success/denial |

No endpoint is added for direct permission grants, role-definition edits, permission-definition edits, or administrative MFA reset.

## 1. Authenticated password change

### Endpoint

`POST /api/v1/auth/password/change`

### Request

```json
{
  "currentPassword": "string",
  "newPassword": "string"
}
```

Both fields are required JSON strings. The API does not require a password-confirmation field. Web and Flutter may collect a confirmation value for UX and compare it locally before submission, but the backend remains authoritative for the new password policy and never receives or logs an unnecessary duplicate secret.

The exact password complexity/length parameters remain an implementation note under the approved secure adaptive password policy. The implementation must select and document those parameters before coding the endpoint; it may not weaken the approved hashing/security requirements.

### Response

`200 OK` using the standard success envelope:

```json
{
  "data": {
    "changed": true,
    "reauthenticationRequired": true
  },
  "meta": { "requestId": "...", "version": "v1" }
}
```

On success, all active sessions for the user, including the caller's session, are revoked in the same logical operation. The caller must authenticate again. No new token is returned by this endpoint.

### Validation and errors

| HTTP | Code | Condition |
|---:|---|---|
| 400 | `VALIDATION_ERROR` | Missing/empty fields or invalid request shape. |
| 401 | `AUTHENTICATION_REQUIRED` / `SESSION_INVALID` | No valid current session. |
| 403 | `PERMISSION_DENIED` | Current actor lacks `auth.password.change`. |
| 422 | `CURRENT_PASSWORD_INVALID` | Current password does not verify; response does not reveal credential/account details. |
| 422 | `PASSWORD_POLICY_VIOLATION` | New password fails the approved password policy. |
| 429 | `RATE_LIMITED` | Configured authenticated password-change limit is exceeded. |
| 500 | `INTERNAL_ERROR` | Safe failure; no credential details. |

### Audit and security

- Audit actions are `auth.password.change.succeeded` or `auth.password.change.failed` with actor, target user, request ID, result, and safe reason/category only.
- The audit record contains no password, hash, token, or validation value.
- Rate limiting is keyed by authenticated user and request origin using configurable operational values; no fixed unapproved threshold is introduced here.
- The password update and session invalidation must be atomic from the caller's perspective. A failed update must not revoke sessions.

## 2. MFA recovery-code verification

### Canonical choice

Recovery-code verification is part of the existing `POST /api/v1/auth/mfa/verify` endpoint. No dedicated recovery verification route is created.

### Extended request

The existing request `{ "challenge": "...", "code": "..." }` remains valid and means TOTP verification. The addendum adds an optional discriminator:

```json
{
  "challenge": "string",
  "method": "totp" | "recovery_code",
  "code": "string"
}
```

`method` defaults to `totp` when omitted for compatibility with the existing catalog. A caller must send `method: "recovery_code"` to consume a recovery code. The endpoint never tries both methods silently and never creates a second recovery-code path.

### Response

The existing MFA verification session DTO is reused. A successful verification returns the authenticated session result according to the existing `/auth/mfa/verify` contract. No TOTP secret or recovery-code value is returned.

### Rules

- TOTP uses the approved Authenticator App parameters from the MFA decision.
- A recovery code is matched against its stored hash and is marked used exactly once in a transaction.
- Used, revoked, malformed, expired, or unavailable MFA verification material returns the same safe verification failure category; the API does not disclose which condition occurred.
- Recovery-code consumption does not regenerate codes. Regeneration remains `/auth/mfa/recovery-codes/regenerate` and revokes old codes.
- Disabled/no-factor MFA cannot be completed by a recovery code.
- Verification is rate-limited per challenge/session and request origin using configurable operational values.

### Errors and audit

| HTTP | Code | Condition |
|---:|---|---|
| 400 | `VALIDATION_ERROR` | Missing challenge/code or unsupported method. |
| 401 | `MFA_VERIFICATION_FAILED` | Invalid, used, revoked, expired, or unavailable TOTP/recovery material; safe generic result. |
| 409 | `CONCURRENCY_CONFLICT` | Concurrent consumption lost the one-time-use race; caller must retry with a new code. |
| 429 | `RATE_LIMITED` | Verification limit exceeded. |

Audit actions are `auth.mfa.totp.verification_succeeded`, `auth.mfa.totp.verification_failed`, `auth.mfa.recovery_code.used`, and `auth.mfa.recovery_code.failed`. They contain outcome/category and scope only; never the submitted code or secret.

## 3. Active session listing

### Endpoint

`GET /api/v1/auth/sessions`

### Authentication and scope

- Requires an authenticated session and `auth.sessions.view`.
- The default scope is the current session's resolved scope: a Gym session lists sessions for that same authorized Gym; a platform session lists platform-scoped sessions.
- A caller may provide `gymId` only when the server confirms an active assignment that authorizes that Gym. A caller may not use an arbitrary database name or enumerate another Gym.
- The result is limited to the current user. This is not an administrative session search endpoint.

### Query

```text
page=1&pageSize=25&gymId=<authorized-gym-guid>&sort=lastSeenAtUtc:desc
```

Allowed parameters are the standard collection parameters plus `gymId` and the safe sort fields `createdAtUtc`, `lastSeenAtUtc`, and `expiresAtUtc`. `pageSize` is `1..100`; unknown filters/sorts return `INVALID_FILTER`.

### Response

```json
{
  "data": [
    {
      "sessionId": "guid",
      "gymId": "guid-or-null",
      "sessionKind": "staff",
      "mfaVerified": true,
      "createdAtUtc": "2026-08-26T10:00:00Z",
      "lastSeenAtUtc": "2026-08-26T10:10:00Z",
      "idleExpiresAtUtc": "2026-08-26T10:40:00Z",
      "absoluteExpiresAtUtc": "2026-08-26T18:00:00Z",
      "expiresAtUtc": "2026-08-26T18:00:00Z",
      "userAgent": "safe truncated metadata",
      "isCurrent": true
    }
  ],
  "meta": {
    "requestId": "...",
    "page": 1,
    "pageSize": 25,
    "total": 1,
    "hasNext": false,
    "version": "v1"
  }
}
```

Raw session tokens, token hashes, IP addresses, database names, credential fields, and unrelated-user records are never returned. `userAgent` is optional safe metadata and may be omitted or truncated by the implementation.

### Errors

- `401 AUTHENTICATION_REQUIRED` or `SESSION_INVALID` for an invalid caller session.
- `403 PERMISSION_DENIED` when `auth.sessions.view` is absent.
- `403 GYM_SCOPE_DENIED` for an unauthorized requested Gym.
- `400 INVALID_FILTER` for unsupported query fields.

## 4. Revoke an owned session

### Endpoint

`POST /api/v1/auth/sessions/{sessionId}/revoke`

The action-style POST follows the existing Phase 2 convention for explicit state transitions and avoids a second meaning for `/auth/logout`. `/auth/logout` remains the canonical current-session logout endpoint.

### Request

```json
{
  "reason": "optional safe reason"
}
```

The reason is optional for self-service but, when supplied, is limited to safe text and is written to audit. It must not contain secrets.

### Rules and response

- Requires authentication and `auth.sessions.revoke`.
- The target session must belong to the current user and be in the current/explicitly authorized session scope.
- A user cannot revoke another user's session, a session from an unauthorized Gym, or a session found only through cross-Gym enumeration.
- The current session may be revoked; the response is returned once and the next request with that credential is `401 SESSION_INVALID`.
- Repeating the action for an already revoked/expired owned session is idempotent and returns `200` with `revoked: true` if the session remains in the authorized ownership scope.
- A session absent from the authorized scope returns `404 RESOURCE_NOT_FOUND`; the response does not reveal whether the ID exists elsewhere.

Response:

```json
{
  "data": { "sessionId": "guid", "revoked": true },
  "meta": { "requestId": "...", "version": "v1" }
}
```

Errors are `401 AUTHENTICATION_REQUIRED`/`SESSION_INVALID`, `403 PERMISSION_DENIED`, `403 GYM_SCOPE_DENIED`, `404 RESOURCE_NOT_FOUND`, or `400 VALIDATION_ERROR` as applicable. A successful first revocation emits `auth.session.revoked`; an idempotent repeat may emit a redacted no-op audit or access log according to the implementation policy.

## 5. Access catalog and user list

### `GET /api/v1/platform/access/catalog`

Requires an authenticated actor with `platform.security.manage`. It is a Control Plane read and returns the Phase 5B baseline catalog of 15 permissions, 3 roles, and 14 role-permission assignments, including role scope type and safe descriptions. The later Phase 7 `platform.provision` extension is a separate provisioning permission and does not authorize this Phase 5B access-administration operation. It does not permit catalog mutation and does not expose credentials, session material, MFA secrets, or private user data.

### `GET /api/v1/platform/access/users`

Requires `platform.security.manage` and a Control Plane scope. It accepts the standard collection filters plus:

- `gymId`: an explicitly authorized Gym target;
- `scopeType`: `gym` or `platform`;
- `status`: `active` or `disabled`;
- `search`: safe email/display-name search.

Allowed sort fields are `createdAtUtc`, `updatedAtUtc`, and `email`. The response contains safe user data (`userId`, normalized/display-safe email representation, display name, status, timestamps, opaque version) and safe role assignments (`assignmentId`, `roleId`, role name, scope type, Gym ID, status). It never contains password metadata, sessions, reset records, TOTP state/secrets, recovery codes, or unrestricted audit metadata.

Unauthorized Gym filters return `403 GYM_SCOPE_DENIED`. Users/assignments not visible in the authorized scope are not disclosed.

## 6. User creation

### Endpoint

`POST /api/v1/platform/access/users`

### Request

```json
{
  "email": "user@example.test",
  "displayName": "Safe display name",
  "initialPassword": "secret supplied over the protected API",
  "roleId": "guid",
  "gymId": "guid-or-null"
}
```

The endpoint creates one active user and one initial role assignment atomically. `gymId` is required for a Gym-scoped role and must be null for a platform-scoped role; the server derives and validates scope from the role record rather than trusting a client scope label. The initial password is hashed by the approved password adapter and is never returned or logged. MFA begins disabled/unconfigured; enrollment is a separate self-service operation.

### Authorization and restrictions

- Requires `platform.security.manage`.
- A Gym-scoped security administrator may create/assign only a user in the currently authorized Gym and only a Gym-scoped role.
- A platform-scoped security administrator may target an explicitly registered Gym or platform scope for this access operation. This permission does not grant operational access to the target Gym's business data.
- The endpoint uses the canonical existing roles; it cannot create a new role or permission.
- No external email/SMS provider is required. The initial password is supplied through the protected local/admin workflow and must be changed by the user according to the password policy.
- Normalized email uniqueness is enforced transactionally. A duplicate returns `409 DUPLICATE_RESOURCE`; the command never creates a second identity.
- Because this security command is protected by a unique normalized email and an atomic transaction, no second idempotency protocol is introduced. Clients must query the user list after an ambiguous transport failure before retrying.

### Response and errors

`201 Created` with a safe user/initial-assignment DTO. No `Location` URL is required by this contract because the access screen uses the collection route.

Errors: `400 VALIDATION_ERROR`, `403 PERMISSION_DENIED`/`GYM_SCOPE_DENIED`, `409 DUPLICATE_RESOURCE`, `422 DOMAIN_RULE_VIOLATION` for role/scope mismatch, and `429 RATE_LIMITED` for configured administrative creation throttling.

Audit action: `iam.user.created` with actor, target, assignment scope, request ID, result, and safe reason. Password and secret fields are excluded.

## 7. User status management

### Endpoint

`PATCH /api/v1/platform/access/users/{userId}/status`

### Request

```json
{
  "status": "active" | "disabled",
  "reason": "required safe administrative reason"
}
```

The request uses the existing optimistic-concurrency convention: `If-Match` with the user's opaque row-version is required for a state change. The API does not accept a caller-provided database name or scope override.

### Rules

- Requires `platform.security.manage` and target-scope authorization.
- A status update is idempotent when the requested status is already current.
- Disabling a user revokes all of that user's active sessions and prevents new login/MFA completion. Existing role assignments are retained for possible later reactivation; they do not bypass disabled-account checks.
- Self-disable is rejected as a security lockout prevention rule. Self-service password/MFA controls remain separate.
- Activation does not create a role, reset a password, or bypass MFA.

Errors: `401 AUTHENTICATION_REQUIRED`/`SESSION_INVALID`, `403 PERMISSION_DENIED`/`GYM_SCOPE_DENIED`, `404 RESOURCE_NOT_FOUND` within authorized scope, `409 CONCURRENCY_CONFLICT`, `422 DOMAIN_RULE_VIOLATION`, and `400 VALIDATION_ERROR`.

Audit actions: `iam.user.disabled`, `iam.user.enabled`, and `iam.user.status_change_failed`. No secrets are recorded.

## 8. Role assignment

### Endpoint

`PUT /api/v1/platform/access/users/{userId}/role-assignments/{roleId}`

For a Gym role, the request must include `?gymId=<authorized-gym-guid>`. For a platform role, `gymId` must be omitted. The role record's `scope_type` is authoritative.

### Request

```json
{
  "reason": "required safe administrative reason"
}
```

### Rules

- Requires `platform.security.manage` and target scope authorization.
- Gym-scoped actors may manage only Gym-scoped roles for their resolved Gym. They cannot assign platform roles or another Gym's role.
- Platform-scoped actors may manage explicitly targeted platform/Gym assignments through this Control Plane security operation, but that does not create operational Gym permission implicitly.
- The caller cannot assign a role to their own user. Role changes are administrative, not self-service.
- The target user must be active or the command must explicitly be a security-administration operation allowed for a disabled account; the Phase 5B minimum path permits assignment only to an active user.
- The role must be active and one of the existing canonical roles. No new role or permission is created.
- The operation is naturally idempotent: an existing active matching assignment returns its current safe representation; an inactive matching assignment is reactivated; no duplicate active assignment is created.
- Assignment identity is `(userId, gymId, roleId, scopeType)`. The existing unique active assignment constraint and transaction are authoritative; no second idempotency system is introduced.
- An `If-Match` opaque assignment version is required when reactivating an existing inactive assignment.

### Response and errors

`200 OK` with `{ assignmentId, userId, roleId, gymId, scopeType, status, version }` in the standard envelope.

Errors: `400 VALIDATION_ERROR`, `401 AUTHENTICATION_REQUIRED`/`SESSION_INVALID`, `403 PERMISSION_DENIED`/`GYM_SCOPE_DENIED`, `404 RESOURCE_NOT_FOUND`, `409 CONCURRENCY_CONFLICT`, and `422 DOMAIN_RULE_VIOLATION` for role/scope/self-assignment restrictions.

Audit action: `iam.role_assignment.created` or `iam.role_assignment.reactivated`.

## 9. Role revocation

### Endpoint

`POST /api/v1/platform/access/users/{userId}/role-assignments/{assignmentId}/revoke`

### Request

```json
{
  "reason": "required safe administrative reason"
}
```

`If-Match` with the assignment's opaque row-version is required for an active assignment transition.

### Rules

- Requires `platform.security.manage` and target scope authorization.
- The assignment must belong to the path user and the target scope must be visible to the caller.
- The caller cannot revoke their own active role assignment. Self-role modification is not a self-service capability.
- A repeated revoke of an already inactive assignment is idempotent and returns `200` with `revoked: true`.
- An assignment outside the authorized scope is not disclosed; the server returns `403 GYM_SCOPE_DENIED` for a known unauthorized target or `404 RESOURCE_NOT_FOUND` when it is absent from the authorized view, according to the common safe-resource policy.
- Revocation does not delete history. It changes the assignment status and invalidates the affected user's authorization on the next request/session evaluation.

Response:

```json
{
  "data": { "assignmentId": "guid", "revoked": true },
  "meta": { "requestId": "...", "version": "v1" }
}
```

Audit actions: `iam.role_assignment.revoked` or `iam.role_assignment.revoke_failed`.

## 10. Administrative MFA reset classification

Administrative reset of another user's MFA factor is **DEFERRED — LATER ADMIN SECURITY FEATURE**. It is not required to complete the minimum Phase 5B API contract because:

- the approved Phase 5B list explicitly covers self-service enrollment, verification, disablement, and recovery-code lifecycle;
- no dedicated approved permission or exact endpoint exists for a privileged MFA reset;
- using the broad `platform.security.manage` permission to introduce a new high-risk reset operation would expand scope without an approved step-up/confirmation contract.

No endpoint, request, response, or database structure is added for admin MFA reset. If later approved, it must receive a separate contract decision covering target scope, step-up, session invalidation, recovery-code invalidation, confirmation, and audit.

## Permission matrix for this addendum

| Operation | Exact permission | Scope |
|---|---|---|
| Change own password | `auth.password.change` | Current authenticated user |
| Verify TOTP/recovery code | `auth.mfa.verify` | Current challenge/session |
| List own sessions | `auth.sessions.view` | Current user + current/explicitly authorized Gym scope |
| Revoke own session | `auth.sessions.revoke` | Current user + owned session scope |
| Read access catalog | `platform.security.manage` | Control Plane security scope |
| Read access users | `platform.security.manage` | Control Plane + authorized target Gym/platform scope |
| Create user | `platform.security.manage` | Control Plane + requested role/Gym scope |
| Change user status | `platform.security.manage` | Control Plane + target scope |
| Assign/revoke role | `platform.security.manage` | Control Plane + target role/Gym scope |

No new permission key is introduced. The canonical 15 identifiers remain unchanged.

## Web traceability

| API operation | Existing Web screen | Client behavior |
|---|---|---|
| Password change | `SYS-W-001` `/login` auth/security flow and `SYS-W-002` authenticated shell security area | Form, local confirmation UX, server validation, logout/reauthentication after success. |
| MFA recovery verification | `SYS-W-001` MFA step | Method selector/explicit recovery-code entry; one safe verification error state. |
| Session list/revoke | `SYS-W-002` authenticated shell/account-security subsection | Safe session table, current-session marker, revoke action, no raw token. No new route ID. |
| Access catalog/users/status/roles | `PA-W-007` `/platform/access` | Web-only access administration; permission-aware actions and confirmation/reason dialogs. |

The existing Web catalog IDs are reused; no new Web business screen is required by this addendum.

## Flutter traceability

| API operation | Existing Flutter screen | Client behavior |
|---|---|---|
| Password change | `F-AUTH-001` `/login` auth shell | Self-service form/sub-flow through Dio; server response controls session state. |
| MFA recovery verification | `F-AUTH-001` `/login` auth shell | Explicit TOTP/recovery method and safe errors. |
| Session list/revoke | `F-AUTH-001` authenticated auth/security sub-flow | Safe own-session view/revoke where the mobile shell exposes account security; no raw token. |
| Platform access administration | None | **NO CLIENT UI REQUIRED IN PHASE 5B**; `PA-W-007` remains Web-only. |

Flutter does not receive role-management or platform access screens in this contract. GoRouter guards and Riverpod state remain presentation/orchestration only; the API remains authoritative.

## User-flow traceability

| Flow ID | Actor | Screen | API | Result |
|---|---|---|---|---|
| `FLOW-AUTH-004` Password change | Authenticated user with `auth.password.change` | `SYS-W-002` security area / `F-AUTH-001` auth sub-flow | `POST /auth/password/change` | Password changed, all sessions revoked, reauthentication required. |
| `FLOW-AUTH-005` MFA recovery | MFA challenge actor with a valid recovery code | `SYS-W-001` / `F-AUTH-001` MFA step | Existing `POST /auth/mfa/verify` with `method=recovery_code` | One code consumed and session completed, or safe failure. |
| `FLOW-AUTH-006` Session security | Authenticated user | `SYS-W-002` / `F-AUTH-001` security sub-flow | `GET /auth/sessions`, `POST /auth/sessions/{id}/revoke` | Safe owned sessions listed/revoked only. |
| `FLOW-AUTH-007` Access catalog | Security administrator | `PA-W-007` | `GET /platform/access/catalog`, `GET /platform/access/users` | Authorized safe access view. |
| `FLOW-AUTH-008` User create/status | Security administrator | `PA-W-007` | `POST /platform/access/users`, `PATCH /platform/access/users/{id}/status` | User/initial role created or status changed with audit. |
| `FLOW-AUTH-009` Role assignment/revocation | Security administrator | `PA-W-007` | Role assignment PUT and revoke action | Active role scope ensured or revoked with audit. |

## Test contract

### API and security tests

- Password change: valid current password, invalid current password, password policy failure, missing session (`401`), missing permission (`403`), rate limit (`429`), all-session invalidation, no secret in response/log/audit.
- MFA: valid TOTP, valid recovery code, invalid/used/revoked recovery code, one-time race, disabled factor, rate limit, no code/secret leakage, audit outcome.
- Sessions: current-user-only listing, safe metadata allowlist, pagination/filter validation, expired/revoked exclusion, owned revoke, other-user denial, unauthorized Gym denial, current-session invalidation.
- Access catalog/users: permission allowed/denied, safe field allowlist, authorized/unauthorized Gym filters, platform/Gym boundary.
- User creation: valid initial role, duplicate normalized email, role/scope mismatch, initial password hashing, atomic failure, no secret leakage, audit.
- Status: activate/disable, self-disable rejection, stale `If-Match` (`409`), session revocation on disable, idempotent same-status update, audit.
- Role assignment: active assignment idempotency, inactive reactivation with version, duplicate prevention, self-assignment rejection, Gym/platform scope denial, inactive role rejection, audit.
- Role revocation: active/repeated revoke, self-role rejection, stale version, cross-Gym denial, authorization invalidation, audit.

### Web and Flutter tests

- Web: auth/security forms, validation, loading/error/success/disabled states, protected route, session table/revoke, PA-W-007 access administration, permission-aware controls, RTL, Arabic, light/dark, responsive behavior, API integration against the real local API contract.
- Flutter: login/MFA method selection, password flow, session state/logout, protected route, safe errors, RTL/Arabic, light/dark, offline-disabled mutation behavior. No platform access UI test is required because it is Web-only.
- End-to-end: login, MFA/recovery, password change and reauthentication, session list/revoke, user create, role assign/revoke, permission denial, Gym A to unauthorized Gym B denial.

## Database traceability

This addendum uses only existing approved tables; it does not authorize a migration:

| Contract area | Tables |
|---|---|
| Password/session/MFA | `iam.users`, `iam.credentials`, `iam.sessions`, `iam.mfa_factors`, `iam.mfa_recovery_codes`, `iam.password_reset_tokens`, `audit.events` |
| Access catalog | `iam.roles`, `iam.permissions`, `iam.role_permissions` |
| User/Gym assignments | `iam.user_gym_roles`, `platform.gyms`, `platform.gym_databases` for registry validation |
| Audit | Control Plane `audit.events`; Gym audit only for an explicitly approved Gym-scoped event |

No new table, column, permission key, seed identity, or cross-database foreign key is introduced by this contract addendum.

## Contract consistency statement

- Existing routes are reused; only `/auth/mfa/verify` receives the documented optional `method` extension.
- New routes are unique and do not duplicate existing catalog routes.
- No new permission key is added by this Phase 5B addendum; all Phase 5B
  operations use the locked 15-key baseline catalog.
- Session, MFA, password, account status, role, and Gym scope behavior are explicit.
- Platform security permission authorizes the listed Control Plane security operation only; it does not grant implicit Gym business access.
- Administrative MFA reset and role/permission catalog mutation are explicitly deferred, not silently assumed.
- This addendum closes the Phase 5B API contract gaps. Implementation remains a separate explicitly authorized task.

## Later Phase 7 permission transition - 2026-08-29

The statement above is scoped to the Phase 5B baseline: it uses the
15-permission, 3-role, 14-assignment catalog. The approved Phase 7 extension
adds `platform.provision` for asynchronous provisioning only, granted to the
existing `platform-security-admin` role and now applied by the Phase 7 EF/seed
implementation. It is not a Phase 5B access-administration permission and
does not authorize Phase 5B routes.
