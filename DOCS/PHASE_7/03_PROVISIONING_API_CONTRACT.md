# Phase 7 - Provisioning API Contract

**Status:** GREEN - the locked three-route contract is implemented and verified locally.
**Base:** `/api/v1`.
**Transport:** JSON over the existing opaque SQL-backed session bearer.
**Public operations:** exactly three; no compatibility routes.

## Shared rules

All endpoints use the existing LogicFit success/error envelopes and request
ID behavior from the Phase 2 API contract. Responses never contain passwords,
password hashes, MFA material, recovery codes, session tokens, credentials,
connection strings, private keys, raw provider payloads, or raw worker logs.

`platform.provision` is the exact permission for all three routes. Start and
retry require an authenticated Platform Admin with a verified Phase 5B MFA
session (`mfa_verified=true`) because the existing authentication architecture
supports step-up. Status requires the same authenticated Platform scope and
permission but is read-only and does not create a second step-up mechanism.
Gym users and Gym Owners are denied even if the client hides the route.

The required `Idempotency-Key` is an opaque 1-128 character header on start
and retry. The server stores only its hash. It is scoped to the authenticated
actor and configured environment. The body is canonicalized for a request
fingerprint.

## 1. Request provisioning

### Endpoint

```text
POST /api/v1/platform/provisioning
```

### Request headers

- `Authorization: Bearer <session-token>`;
- `X-Request-Id`: optional existing request-ID convention; and
- `Idempotency-Key`: required.

### Request body

```json
{
  "organization": {
    "name": "Example Fitness Group",
    "slug": "example-fitness-group"
  },
  "gym": {
    "name": "Example Fitness Downtown",
    "slug": "downtown",
    "timezoneName": "Africa/Cairo"
  },
  "serverTarget": {
    "serverId": "00000000-0000-0000-0000-000000000000"
  },
  "owner": {
    "email": "owner@example.test",
    "displayName": "Gym Owner",
    "initialPassword": "supplied-through-the-protected-local-workflow"
  }
}
```

`organization`, `gym`, `serverTarget`, and `owner` are required. Names use
the catalog lengths; slugs are trimmed, lower-case, and limited to ASCII
letters, digits, and single hyphens. `timezoneName` must be an allowlisted
IANA timezone. `serverId` must identify a registered selectable server. Owner
email/display name/initial password use the existing Phase 5B user-creation
and password policy. The password is write-only input: it is hashed by the
existing password adapter, never returned or logged.

The request MUST NOT contain `organizationId`, an existing `gymId`,
`databaseName`, a connection string, credentials, or a commercial `plan`
object. Phase 7 does not accept a plan field; no commercial subscription
behavior exists. The Owner role is selected server-side as the existing
canonical `gym-security-admin` role; the client cannot choose or override it.

### Response

```http
202 Accepted
```

```json
{
  "data": {
    "operationId": "00000000-0000-0000-0000-000000000000",
    "organizationId": "00000000-0000-0000-0000-000000000000",
    "gymId": "00000000-0000-0000-0000-000000000000",
    "status": "Requested",
    "currentStep": null,
    "requestedAtUtc": "2026-08-29T00:00:00Z",
    "statusUrl": "/api/v1/platform/provisioning/00000000-0000-0000-0000-000000000000"
  },
  "meta": { "requestId": "request-id", "version": "v1" }
}
```

The HTTP request creates the run and Control Plane organization/Gym registry
records, then returns. It never performs database creation, migration, seed,
verification, Owner initialization, or activation inline.

### Idempotency

- Same actor, environment, key, and request fingerprint: return the original
  operation representation with `202`; no duplicate rows or worker operation.
- Same key with a different fingerprint: `409 IDEMPOTENCY_KEY_REUSED`.
- A simultaneous request with a different key for the same organization slug
  or organization/Gym slug identity: `409 DUPLICATE_RESOURCE`; no second run.
- A retry after an ambiguous transport result must reuse the same key.

### Errors

`400 VALIDATION_ERROR`, `401 AUTHENTICATION_REQUIRED` or `SESSION_INVALID`,
`403 PERMISSION_DENIED` or `GYM_SCOPE_DENIED`, `409 DUPLICATE_RESOURCE` or
`IDEMPOTENCY_KEY_REUSED`, `422 DOMAIN_RULE_VIOLATION` for a non-selectable
server/invalid registry rule, and `429 RATE_LIMITED` under the existing
expensive Platform mutation policy. A dependency failure after acceptance is
stored on the operation and is observed through the status route; it is not a
second synchronous API result.

Audit: `PROVISIONING_REQUESTED`, including operation, organization, Gym,
actor, request ID, and safe server target metadata.

## 2. Retrieve provisioning status/result

### Endpoint

```text
GET /api/v1/platform/provisioning/{runId}
```

`runId` is a GUID operation identifier. No pagination is used because the
response represents one fixed operation and its bounded steps.

### Response

```json
{
  "data": {
    "operationId": "00000000-0000-0000-0000-000000000000",
    "organizationId": "00000000-0000-0000-0000-000000000000",
    "gymId": "00000000-0000-0000-0000-000000000000",
    "status": "Migrating",
    "currentStep": "EfCoreMigrations",
    "attemptNo": 1,
    "requestedAtUtc": "2026-08-29T00:00:00Z",
    "startedAtUtc": "2026-08-29T00:00:02Z",
    "completedAtUtc": null,
    "server": {
      "serverId": "00000000-0000-0000-0000-000000000000",
      "environment": "local",
      "status": "active"
    },
    "database": {
      "databaseId": "00000000-0000-0000-0000-000000000000",
      "databaseName": "LogicFit_Gym_00000000000000000000000000000000_local",
      "status": "Migrating",
      "schemaVersion": null,
      "seedVersion": null
    },
    "ownerInitialized": false,
    "retryable": false,
    "failure": null,
    "steps": [
      {
        "stepKey": "OrganizationCreation",
        "status": "Success",
        "attemptNo": 1,
        "startedAtUtc": "2026-08-29T00:00:02Z",
        "completedAtUtc": "2026-08-29T00:00:02Z",
        "retryable": false,
        "failureCategory": null
      }
    ]
  },
  "meta": { "requestId": "request-id", "version": "v1" }
}
```

The full fixed step order is `RequestValidation`,
`OrganizationCreation`, `GymRegistryCreation`, `ServerPlacement`,
`DatabaseCreation`, `EfCoreMigrations`, `CanonicalSeeding`, `Verification`,
`OwnerInitialization`, and `Activation`. Step status is technical execution
metadata (`Pending`, `Running`, `Success`, `Failed`) and is not an additional
run lifecycle state.

The operation `status` is exactly one of `Requested`, `Provisioning`,
`Migrating`, `Seeding`, `Verifying`, `Active`, `ProvisioningFailed`,
`MigrationFailed`, `SeedingFailed`, or `VerificationFailed`.

Failure data contains only `failureCategory`, safe `errorCode`, failed step,
occurred time, and `retryable`. It never contains a stack trace or raw worker
payload. A missing operation inside an unauthorized scope returns
`404 RESOURCE_NOT_FOUND` without existence leakage. Other errors are
`401`, `403`, and `429` according to the shared contract.

Audit is not emitted for ordinary polling; request/access logging remains
redacted. The Phase 6 monitoring snapshot may consume safe registry status,
but no second monitoring system is introduced.

## 3. Retry a failed operation

### Endpoint

```text
POST /api/v1/platform/provisioning/{runId}/retry
```

### Request

Headers are the authenticated session, request ID, and a required new
`Idempotency-Key`. Body:

```json
{ "reason": "Transient SQL Server connection failure; retrying the failed step." }
```

`reason` is required, safe text, maximum 500 characters. The target step,
server, Gym, database, owner, and request payload cannot be changed by retry.

### Response

```http
202 Accepted
```

```json
{
  "data": {
    "operationId": "00000000-0000-0000-0000-000000000000",
    "status": "MigrationFailed",
    "retryAccepted": true,
    "failedStep": "EfCoreMigrations",
    "nextStep": "EfCoreMigrations",
    "nextAttemptNo": 2,
    "retryable": true
  },
  "meta": { "requestId": "request-id", "version": "v1" }
}
```

The failure status remains visible until the worker starts the resumed step;
there is no `Retrying` lifecycle state. A same-key replay returns the same
accepted result. A different retry while an attempt is active returns
`409 CONCURRENCY_CONFLICT`. A retryable flag of false returns
`409 INVALID_STATE_TRANSITION`. Other errors are `401`, `403`, `404`, `422`
for an invalid reason, and `429` where rate-limited.

Retry resumes the same operation and reuses a database only when its
ownership marker matches the operation. If ownership cannot be proved, the
operation remains failed for operator resolution; it is never silently
dropped or overwritten.

Audit: `PROVISIONING_RETRY_STARTED`, including operation, failed step, next
attempt, actor, request ID, and safe reason only.

## Route reconciliation

These are the only Phase 7 public routes. The following are explicitly not
admitted:

- `/api/v1/platform/provisioning/start`;
- `/api/v1/platform/provisioning/cancel`;
- `/api/v1/platform/provisioning/{runId}/migrate`;
- `/api/v1/platform/provisioning/{runId}/seed`;
- database-create, backup/restore, or deprovisioning routes; and
- any duplicate route without the `/api/v1` base.

## Implementation mapping

`ProvisioningController` implements these exact routes and delegates all
authorization, validation, state, idempotency, audit, and safe DTO mapping to
the application service. API tests cover accepted/replay/conflict/status/
retry/error behavior. The Web client uses the same three routes and does not
create a compatibility path.
