# Authentication/RBAC API Addendum Decisions

**Decision date:** 2026-08-26  
**Status:** APPROVED  
**Scope:** Close the Phase 5B Authentication/RBAC API contract gaps only.  
**Authority:** Phase 2 contracts, Phase 5 approved authentication/RBAC intent, and the explicit Phase 5B API Contract Addendum authorization.

These decisions extend the API catalog; they do not authorize implementation in this record. No business-module permission, seed identity, database table, or client architecture is changed.

## ADR-21-01 - Scope classification

| Field | Decision |
|---|---|
| Problem | The Phase 5B request listed several operations that the original API catalog did not describe completely. |
| Options | A: add every seemingly useful endpoint; B: classify each capability against Phase 5B scope and add only required operations; C: defer the whole slice. |
| Selected | **B.** Add only the minimum required operations and explicitly defer role/permission catalog mutation and administrative MFA reset. |
| Reason | Preserves the no-guessing rule, avoids scope expansion, and closes the operations required for authentication, sessions, and RBAC. |
| Security impact | Prevents undocumented privilege and cross-Gym operations from being introduced. |
| API | Addendum sections for password, sessions, access catalog/users, roles, and status. |
| Web | Reuses `SYS-W-001`, `SYS-W-002`, and `PA-W-007`. |
| Flutter | Reuses `F-AUTH-001` for self-service auth; access administration remains Web-only. |
| Tests | Scope classification and absence of deferred endpoints are checked in documentation/API consistency tests. |

## ADR-21-02 - Authenticated password-change route

| Field | Decision |
|---|---|
| Problem | `auth.password.change` and the flow intent existed, but no route or DTO existed. |
| Options | Add `POST /auth/password/change`; add a password subresource with a different route; overload password-reset completion. |
| Selected | `POST /api/v1/auth/password/change`. |
| Reason | It is a distinct authenticated operation and must not be confused with public reset completion. The route follows the existing action-oriented auth convention. |
| Security impact | Requires current session, exact permission, current-password verification, safe errors, rate limiting, and no secret output. All active sessions are revoked on success. |
| API | Request is `currentPassword` + `newPassword`; no API confirmation field; response says changed and reauthentication required. |
| Web | `SYS-W-001`/`SYS-W-002` security sub-flow; client may compare confirmation locally. |
| Flutter | `F-AUTH-001` self-service sub-flow. |
| Tests | Valid/invalid current password, policy failure, 401/403/429, atomic all-session invalidation, redaction. |

## ADR-21-03 - Password confirmation

| Field | Decision |
|---|---|
| Problem | The missing endpoint did not state whether a confirmation secret is part of the API request. |
| Options | Require `confirmNewPassword` in the API; do not require it in the API and allow client UX confirmation; omit confirmation everywhere. |
| Selected | Do not require a confirmation field in the API; Web/Flutter may compare it locally. |
| Reason | Confirmation is duplicate client UX data, not an authoritative business value. The backend validates only the new password according to the approved policy. |
| Security impact | Reduces secret surface and prevents a second password value from being logged or persisted. |
| API | Request remains exactly two password fields. |
| Web | Local mismatch prevents submission and displays field validation. |
| Flutter | Same local UX validation; API remains authoritative. |
| Tests | Client mismatch test plus backend policy/current-password tests. |

## ADR-21-04 - Password-change session behavior

| Field | Decision |
|---|---|
| Problem | The contract required explicit session behavior after a password change. |
| Options | Keep the current session; rotate only the current session; revoke all active sessions and require reauthentication. |
| Selected | Revoke all active sessions, including the caller's session, after a successful change. |
| Reason | A password change is a security boundary; invalidating every prior session is the least ambiguous approved behavior and matches the session policy's invalidation rule. |
| Security impact | Compromised or stale sessions cannot remain active after credential rotation. |
| API | `200` returns `{changed:true,reauthenticationRequired:true}` and no token. |
| Web | Return to login/auth shell after success. |
| Flutter | Clear local session state and navigate to auth shell. |
| Tests | Old session returns `401 SESSION_INVALID`; failed password change does not revoke sessions. |

## ADR-21-05 - Recovery-code verification transport

| Field | Decision |
|---|---|
| Problem | MFA recovery-code consumption had no route. |
| Options | Add a dedicated recovery endpoint; extend `/auth/mfa/verify`; accept a code on multiple endpoints. |
| Selected | Extend the existing `POST /api/v1/auth/mfa/verify` with optional `method` (`totp` or `recovery_code`), defaulting to `totp`. |
| Reason | Keeps one MFA challenge/session verification boundary and avoids competing mechanisms. The default preserves the existing `{challenge,code}` request. |
| Security impact | Explicit method selection prevents silent dual verification; recovery codes are hash-matched and consumed once transactionally. |
| API | Existing response is reused; invalid/used/revoked/expired material has one safe failure category; no regeneration during verification. |
| Web | `SYS-W-001` MFA step. |
| Flutter | `F-AUTH-001` MFA step. |
| Tests | Valid TOTP, valid recovery code, reuse failure, concurrent consumption, disabled factor, rate limiting, redacted audit. |

## ADR-21-06 - Session listing

| Field | Decision |
|---|---|
| Problem | `auth.sessions.view` and session policy required safe session visibility but had no route. |
| Options | No session visibility; add `GET /auth/sessions`; expose sessions through `/auth/me`. |
| Selected | `GET /api/v1/auth/sessions`. |
| Reason | Keeps actor resolution and session-management concerns separate and permits standard collection pagination. |
| Security impact | Self-only result, current/explicitly authorized Gym scope, no raw token/hash/IP/database details. |
| API | Standard `page`, `pageSize`, safe sort fields, optional authorized `gymId`; safe session metadata and current marker. |
| Web | `SYS-W-002` account-security subsection; no new screen ID. |
| Flutter | `F-AUTH-001` account-security sub-flow where exposed; no platform admin session UI. |
| Tests | Self-only, pagination/filter validation, expired/revoked exclusion, unauthorized Gym denial, secret allowlist. |

## ADR-21-07 - Owned-session revocation

| Field | Decision |
|---|---|
| Problem | `auth.sessions.revoke` had no canonical route, while `/auth/logout` already owns current-session logout. |
| Options | `DELETE /auth/sessions/{id}`; `POST /auth/sessions/{id}/revoke`; overload `/auth/logout` with a target ID. |
| Selected | `POST /api/v1/auth/sessions/{sessionId}/revoke`. |
| Reason | Existing catalog uses action-style POST for explicit state transitions; it avoids changing the meaning of logout and avoids DELETE-body transport ambiguity. |
| Security impact | Enforces ownership and current/authorized scope; cross-user/cross-Gym targets are denied or safely hidden. |
| API | Optional safe reason; repeated owned revoke is idempotent; current session can be revoked. |
| Web | `SYS-W-002` session list action. |
| Flutter | `F-AUTH-001` session action where exposed. |
| Tests | Own/other-user/session scope, repeat revoke, current-session invalidation, 401/403/404 behavior. |

## ADR-21-08 - Access catalog read surface

| Field | Decision |
|---|---|
| Problem | `PA-W-007` requires a role/permission matrix, but no read endpoint was named. |
| Options | Return the catalog through `/auth/me`; add separate role and permission routes; add one combined access catalog route. |
| Selected | `GET /api/v1/platform/access/catalog`. |
| Reason | The screen needs one immutable canonical reference view; the combined route avoids unnecessary endpoint proliferation. |
| Security impact | Requires `platform.security.manage`; returns only the locked 15 permissions, 3 roles, and 14 assignments; no mutation or private identity data. |
| API | Control Plane read; no direct role/permission edits in Phase 5B. |
| Web | `PA-W-007` role/permission matrix. |
| Flutter | No mobile UI; Web-only. |
| Tests | Permission, scope, count, canonical-key, and response-redaction checks. |

## ADR-21-09 - Access user list

| Field | Decision |
|---|---|
| Problem | Access administration needs a safe user table and target selection. |
| Options | No list route; expose users through `/auth/me`; add `GET /platform/access/users`. |
| Selected | `GET /api/v1/platform/access/users`. |
| Reason | A security-admin table is a distinct Control Plane read and must be paginated and scope-filtered. |
| Security impact | Exact `platform.security.manage`, target Gym authorization, safe fields only; no sessions, credentials, MFA, reset, or unrestricted audit data. |
| API | Standard collection filters plus authorized `gymId`, `scopeType`, `status`, and safe search/sort. |
| Web | `PA-W-007`. |
| Flutter | No mobile UI; Web-only. |
| Tests | Scope filters, permission denial, pagination, data allowlist, cross-Gym non-enumeration. |

## ADR-21-10 - User creation

| Field | Decision |
|---|---|
| Problem | The minimum UAT requires creating a user who can then receive a role and authenticate. |
| Options | Defer user creation; create an unassigned user; create an active user with one initial role assignment. |
| Selected | `POST /api/v1/platform/access/users` creates one active user and one initial role assignment atomically. |
| Reason | Avoids unusable identities and avoids a partial create/assignment state. It remains minimum access administration, not a full user-management module. |
| Security impact | Requires `platform.security.manage`, role/Gym scope validation, secure initial password hashing, no external delivery provider, normalized-email uniqueness, and audit. |
| API | `email`, `displayName`, `initialPassword`, `roleId`, and `gymId`; MFA starts unconfigured; no secrets in response. Duplicate email is `409`. |
| Web | `PA-W-007` create-user dialog. |
| Flutter | No mobile UI; Web-only. |
| Tests | Atomicity, duplicate email, role/scope mismatch, password hashing, no secret leakage, audit. |

## ADR-21-11 - User status

| Field | Decision |
|---|---|
| Problem | Account status validation and admin enable/disable were required but had no operation contract. |
| Options | No status operation; delete identities; add a status patch preserving history. |
| Selected | `PATCH /api/v1/platform/access/users/{userId}/status` with `active`/`disabled`. |
| Reason | Preserves identity/history and matches the approved status field/soft-security model. |
| Security impact | Disable revokes all active sessions and blocks future login/MFA; assignments remain retained but cannot bypass status. Self-disable is rejected. |
| API | Required safe reason and `If-Match` opaque user version; same-status update is idempotent. |
| Web | `PA-W-007` status action/confirmation. |
| Flutter | No mobile UI; Web-only. |
| Tests | Activate/disable, self-disable, stale version, session invalidation, repeated status, audit. |

## ADR-21-12 - Role assignment route and scope

| Field | Decision |
|---|---|
| Problem | RBAC needed role assignment with Gym scope while preserving platform/Gym separation. |
| Options | Add a generic role POST with client scope labels; use a deterministic role-assignment PUT; expose role management under each business module. |
| Selected | `PUT /api/v1/platform/access/users/{userId}/role-assignments/{roleId}` with an authorized `gymId` query for Gym roles; omit it for platform roles. |
| Reason | The URI identifies one `(user,role,scope)` assignment and is naturally idempotent without introducing a second idempotency store. The role's persisted `scope_type` is authoritative. |
| Security impact | Gym actors can manage only Gym roles in their Gym; platform actors may perform this explicit Control Plane security operation but gain no implicit Gym business access; self-assignment is rejected. |
| API | Safe reason, active canonical role only, existing active assignment is returned, inactive matching assignment is reactivated with `If-Match`. |
| Web | `PA-W-007` grant-role action. |
| Flutter | No mobile UI; Web-only. |
| Tests | Idempotency, role/scope mismatch, self-assignment, cross-Gym denial, inactive-role rejection, audit. |

## ADR-21-13 - Role revocation route

| Field | Decision |
|---|---|
| Problem | Role revocation needed a route, ownership rule, and historical behavior. |
| Options | Hard-delete assignment; `DELETE` assignment; explicit revoke action that changes status. |
| Selected | `POST /api/v1/platform/access/users/{userId}/role-assignments/{assignmentId}/revoke`. |
| Reason | Existing action-style conventions are used, and the assignment record remains auditable rather than being hard-deleted. |
| Security impact | Exact permission/scope, path-user ownership, self-role revocation rejection, next-request authorization invalidation. |
| API | Required safe reason and `If-Match`; repeated revoke returns an idempotent success. |
| Web | `PA-W-007` revoke action/confirmation. |
| Flutter | No mobile UI; Web-only. |
| Tests | Active/repeated revoke, stale version, self-role, cross-Gym, authorization invalidation, audit. |

## ADR-21-14 - Role/permission definition mutation

| Field | Decision |
|---|---|
| Problem | The broad `platform.security.manage` description could be read as authorizing role/permission catalog editing. |
| Options | Implement definition CRUD now; implement only read catalog; defer all catalog mutation. |
| Selected | Defer role/permission definition mutation. Phase 5B provides read-only canonical catalog data. |
| Reason | Phase 5B requires role assignment/revocation and permission evaluation, not a configurable IAM-design module. The canonical 15/3/14 data must not drift during this slice. |
| Security impact | Prevents privilege creation/change without a separate approved security contract. |
| API | `GET /platform/access/catalog` only; no POST/PATCH/DELETE for role/permission definitions. |
| Web | `PA-W-007` matrix is read-only for definitions; assignment actions only. |
| Flutter | No mobile UI. |
| Tests | Confirm mutation routes are absent and catalog counts/keys remain canonical. |

## ADR-21-15 - Administrative MFA reset

| Field | Decision |
|---|---|
| Problem | It was unclear whether an administrator may reset another user's MFA factor in Phase 5B. |
| Options | Add an admin reset using `platform.security.manage`; overload self-service disable; defer until a dedicated admin security contract. |
| Selected | **DEFERRED - LATER ADMIN SECURITY FEATURE.** |
| Reason | The approved Phase 5B minimum explicitly covers self-service enrollment, verification, disablement, recovery, and audit. No exact reset permission, route, confirmation, or step-up contract exists. |
| Security impact | Avoids introducing an undocumented high-risk capability. User disablement still revokes sessions; it does not silently reset MFA. |
| API | No administrative MFA reset route. |
| Web | No reset action in `PA-W-007`. |
| Flutter | No UI. |
| Tests | Confirm absence; self-service MFA and status-disable behavior remain tested. |

## ADR-21-16 - Error, audit, and idempotency reconciliation

| Field | Decision |
|---|---|
| Problem | New endpoints needed to remain compatible with the existing envelope, status families, audit policy, and mutation rules. |
| Options | Add a second error envelope; use existing families and safe extension codes; introduce an independent client-side error model. |
| Selected | Reuse the existing envelope and families. New safe condition codes (`CURRENT_PASSWORD_INVALID`, `PASSWORD_POLICY_VIOLATION`, `MFA_VERIFICATION_FAILED`) remain inside the approved HTTP families. |
| Reason | Prevents client/API drift and preserves request-ID diagnostics. |
| Security impact | Safe generic errors and redacted audit only; no account/secret leakage. |
| API | `401/403/404/409/422/429` only where defined; no raw secrets. |
| Web | Maps the existing error envelope to form/notification states. |
| Flutter | Maps the same envelope through Dio. |
| Tests | Envelope, status, redaction, duplicate, stale-version, and rate-limit checks. |

## Final decision

The addendum at `DOCS/PHASE_2/21_AUTH_RBAC_API_CONTRACT_ADDENDUM.md` is the single Phase 5B API extension. It closes the documented contract gaps without changing Phase 2 database contracts, Phase 3 seeds, or the approved 15 permission identifiers. Implementation must wait for the separate Phase 5B implementation authorization.
