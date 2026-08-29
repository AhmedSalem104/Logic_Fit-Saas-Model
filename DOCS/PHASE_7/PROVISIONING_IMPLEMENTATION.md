# Phase 7 Provisioning Implementation

**Status:** GREEN - implementation and final local verification passed.
**Scope:** Gym provisioning only. Phase 8 and all business modules remain out of scope.

## Delivered vertical slice

The approved asynchronous provisioning workflow is implemented in the
existing .NET architecture:

```text
Platform Admin API
        |
        v
Control Plane acceptance transaction
        |
        v
single-reader local provisioning queue/worker
        |
        +-- registered server validation
        +-- deterministic SQL Server database creation
        +-- Gym EF Core migrations
        +-- unchanged Phase 3 canonical .NET seed executor
        +-- schema/context/seed verification
        +-- Phase 5B Owner/RBAC initialization
        +-- final Gym activation and audit
```

The API persists the operation and returns `202 Accepted`; database creation,
migrations, seeding, verification, Owner initialization, and activation do
not run in the HTTP request lifecycle. The worker recovers persisted
non-terminal operations when the application starts. The local queue has one
reader, and Control Plane serializable acceptance plus unique constraints
protect against duplicate acceptance in the supported local runtime.

## Public surface

`ProvisioningController` exposes exactly:

- `POST /api/v1/platform/provisioning`;
- `GET /api/v1/platform/provisioning/{runId}`; and
- `POST /api/v1/platform/provisioning/{runId}/retry`.

There are no public migration, seed, database-create, cancel, restore, or
compatibility routes.

## Persistence and migrations

The Control Plane model adds the approved server placement and provisioning
run/step metadata, extends the existing Gym and Gym-database registry
relations, and persists the internal owner/retry replay data needed for safe
same-operation recovery. The additive migrations are:

- `20260829095350_Phase7ProvisioningFoundation`;
- `20260829105045_Phase7LifecycleStates`.

The lifecycle migration preserves the pre-existing local registry status
values while admitting the closed Phase 7 state vocabulary. No operational
data is dropped and no existing local database is recreated.

## Security and idempotency

- `platform.provision` is seeded only for `platform-security-admin`.
- Start and retry require Platform scope, that permission, and verified MFA;
  status is read-only and uses the same Platform authorization without a
  second step-up.
- The request cannot supply a Gym ID, physical database name, connection
  string, credentials, or Owner role.
- The Owner is initialized through the existing Phase 5B IAM/RBAC path as
  `gym-security-admin`.
- A request idempotency hash is scoped to actor/environment; a fingerprint
  detects reuse with different input. Retry uses a new idempotency key but
  the same run and target.
- Failure metadata and audit events contain only safe identifiers/categories.
  Passwords, tokens, MFA data, recovery codes, SQL credentials, and provider
  payloads are not logged or returned.

## Seed and verification

The workflow calls `GymDbContext.Database.MigrateAsync` followed by the
existing `CanonicalLibrarySeeder`. Verification checks the target Gym
context, migration history, seed version/installations, and canonical counts.
The Phase 3 seed JSON and deterministic identities are unchanged.

## Web and Flutter boundary

`PlatformProvisioningPage` implements the approved Platform Admin Web form
and operation status/retry view. It uses the existing API client, protected
route, theme, and permission state; it never accesses SQL Server. No Phase 7
Flutter provisioning route or screen was added.

## Final verification

The isolated local end-to-end run used a temporary Control Plane/Gym pair and
verified invalid login, valid login, `202 Accepted`, status polling through all
ten workflow steps, `Active` completion, Arabic/RTL rendering, and clean
Chrome console/network results. The temporary databases were removed after
verification. The main local databases were not reset.

The supported local worker is single-reader and recovers accepted operations
on process startup. Distributed multi-host worker leasing is outside this
local Phase 7 implementation and remains an operational follow-up before
horizontal deployment.
