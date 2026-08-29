# Phase 7 - Decision Register

**Date:** 2026-08-29
**Status:** GREEN - final human approvals recorded; all Phase 7 contract gaps
are closed.
**Task:** Contract decisions remain locked; implementation is authorized and is recorded in the implementation documents.

Each approved decision below is recorded individually. “Implementation
impact” describes work that may begin only after the contract gate is GREEN.

## P7-D-001 - Provisioning authorization

- **Problem:** `platform.view` is read-only and cannot authorize provisioning.
- **Evidence:** Final human approval; existing Phase 6 permission boundary.
- **Selected:** Add the dedicated critical/high-risk permission
  `platform.provision` to the forward canonical permission contract. It is
  Control Plane scoped, granted only to the existing Platform Admin role
  `platform-security-admin`, requires verified Phase 5B MFA step-up for
  provisioning actions, and is audited.
- **Options rejected:** Reuse `platform.view`; reuse
  `platform.security.manage`; create a role alias.
- **Impact:** Future permission catalog/EF seed becomes 16 permissions and
  the existing Platform role gains one grant; no code/DB/seed change is made
  by this record.

## P7-D-002 - Organization and Gym creation

- **Problem:** A new provisioning request must not depend on an existing Gym
  ID while Phase 6 registry APIs are read-only.
- **Evidence:** Final human approval and Phase 2 provisioning request shape.
- **Selected:** The request contains organization/Gym input, not existing
  organization or Gym IDs. The system generates IDs and creates the
  organization, Gym registry record, and provisioning run in the Control
  Plane acceptance transaction. Unique slugs and active-operation rules
  prevent duplicates.
- **Impact:** No standalone registry mutation API is introduced; duplicate
  identity returns a safe `409`.

## P7-D-003 - Plan metadata

- **Problem:** Historical prose contains `plan`, but commercial behavior is
  not in Phase 7.
- **Evidence:** Final human approval and Phase 6 no-billing boundary.
- **Selected:** The canonical Phase 7 request has no client-supplied plan
  field. No plan table, subscription state, billing, invoice, payment, or
  gateway is created. If a future technical reference is required by a
  separately approved schema, it remains non-commercial metadata and needs
  its own contract update.
- **Impact:** The historical symbolic `plan` request member is explicitly
  non-canonical for Phase 7.

## P7-D-004 - Asynchronous execution

- **Problem:** Database creation, migration, seed, and verification are
  long-running operations.
- **Evidence:** Final human approval.
- **Selected:** The HTTP request persists the operation and returns `202`
  with its identifier/status. A worker performs the ordered workflow outside
  the HTTP lifecycle.
- **Impact:** No synchronous provisioning implementation and no HTTP timeout
  dependency is allowed.

## P7-D-005 - Lifecycle

- **Problem:** Earlier evidence used broad Pending/Running/Success/Failed
  labels and did not close transitions.
- **Evidence:** Final human approval.
- **Selected:** Run states are exactly `Requested`, `Provisioning`,
  `Migrating`, `Seeding`, `Verifying`, `Active`, `ProvisioningFailed`,
  `MigrationFailed`, `SeedingFailed`, and `VerificationFailed`. Retry returns
  to the failed stage when its `retryable` property is true. Cancellation is
  not implemented; Active is not silently moved backward.
- **Impact:** Technical step statuses remain metadata only and do not add run
  states.

## P7-D-006 - Placement and naming

- **Problem:** Placement and physical database naming were not exact.
- **Evidence:** Final human approval and Phase 6 server registry contract.
- **Selected:** The system validates/selects an active registered server using
  `serverId` in the request. The caller cannot choose a physical database
  name. New names use `LogicFit_Gym_{gymId:N}_{environment}`, with lowercase
  32-hex Gym ID and normalized server environment, checked for the SQL Server
  identifier limit and uniqueness.
- **Impact:** `platform.servers` and `platform.gym_databases.server_id` are
  Phase 7 Control Plane metadata; credentials remain outside tables.

## P7-D-007 - First Gym Owner

- **Problem:** Provisioning must initialize the first Owner without a second
  authentication system.
- **Evidence:** Final human approval, Phase 2 `iam.user_gym_roles` and
  `auth.gym_users` contract, and Phase 5B user-creation contract.
- **Selected:** Owner initialization occurs after verification and before
  activation. It reuses Phase 5B `iam.users`, hash-only credentials,
  `iam.user_gym_roles`, and the Gym `auth.gym_users` projection. The local
  development mechanism is the existing protected admin workflow with an
  admin-supplied initial password; no email delivery is invented. The Owner
  is active only when the existing authentication contract permits it and
  receives the existing canonical `gym-security-admin` role. This role is
  the Phase 7 Gym Owner identity; its existing permissions are unchanged.
- **Owner-role resolution:** The final human approval explicitly maps Gym
  Owner to `gym-security-admin`. No new `gym-owner`, `owner`, `gym-admin`, or
  `platform-admin` role key, alias, rename, or unrelated grant is created.
- **Impact:** The provisioning actor remains the existing
  `platform-security-admin` role with `platform.provision`; a Gym Owner using
  `gym-security-admin` cannot provision another Gym. Phase 5B's runtime role
  catalog and grants are not changed by this contract-only decision.

## P7-D-008 - Partial failure, retry, and cleanup

- **Problem:** A failure may occur after database creation or during process
  restart.
- **Evidence:** Final human approval.
- **Selected:** Retain the operation and partial database metadata; record the
  exact failure state; do not silently delete anything. Retry reuses the same
  operation and target and resumes the failed idempotent step only when the
  database ownership marker matches. A process restart recovers persisted
  accepted runs through the local single-reader worker on startup. This local
  implementation does not claim a distributed worker lease; unknown ownership,
  collision, or integrity failures remain safely non-retryable pending operator
  resolution.
- **Impact:** No destructive automatic cleanup, replacement database, or
  duplicate run/Owner/seed is allowed.

## P7-D-009 - Backup and restore

- **Problem:** General backup/DR policy exists, but fresh provisioning does
  not need backup/restore.
- **Evidence:** Final human approval.
- **Selected:** No backup before provisioning, restore during provisioning,
  backup/restore API, or backup worker is part of Phase 7.
- **Impact:** Backup/DR remains a separate approved operational capability.

## P7-D-010 - Migration and seed authorization

- **Problem:** Migration and seed are required steps but are not separate
  user operations.
- **Evidence:** Final human approval and EF/Phase 3 seed authority.
- **Selected:** EF migration and .NET canonical seed execution are internal
  worker steps under the provisioning authorization. No `/migrate` or `/seed`
  endpoint or separate permission is introduced.
- **Impact:** Public API exposes safe status only.

## P7-D-011 - Audit vocabulary

- **Problem:** Phase 7 audit events and safe metadata were not exact.
- **Evidence:** Final human approval.
- **Selected:** Use only `PROVISIONING_REQUESTED`,
  `PROVISIONING_STARTED`, `PROVISIONING_DATABASE_CREATED`,
  `PROVISIONING_MIGRATING`, `PROVISIONING_SEEDING`,
  `PROVISIONING_VERIFYING`, `PROVISIONING_ACTIVATED`,
  `PROVISIONING_FAILED`, and `PROVISIONING_RETRY_STARTED` in the existing
  audit system. Safe metadata may include operation, Gym, organization,
  server, database, lifecycle state, failure category, request ID, and actor.
- **Impact:** Secrets, credentials, private keys, raw logs, and sensitive
  values remain prohibited.

## P7-D-012 - Phase boundaries

- **Problem:** Platform foundation, provisioning, and Members could overlap.
- **Evidence:** Final human approval and dependency graph.
- **Selected:** Phase 6 owns registry/server/database metadata, overview, and
  monitoring. Phase 7 owns provisioning execution, organization/Gym creation
  during provisioning, database creation/placement, EF migration, seed,
  verification, Owner initialization, lifecycle, and audit. Phase 8 owns
  Members.
- **Impact:** No Phase 6 route duplication and no member/business table or
  feature is admitted.

## P7-D-013 - Provisioning security

- **Problem:** High-impact provisioning must not be callable by Gym users or
  client-side claims.
- **Evidence:** Final human approval and Phase 5B server-side authorization.
- **Selected:** Require authenticated Platform Admin scope,
  `platform.provision`, verified MFA step-up for start/retry, server-side
  target/scope validation, idempotency, audit, and safe secret handling.
- **Impact:** Gym Owner and Gym users receive `403`; no client permission
  decision is trusted.

## P7-D-014 - Canonical provisioning APIs

- **Problem:** The historical three routes lacked complete schemas and
  asynchronous behavior.
- **Evidence:** Final human approval and Phase 2 API catalog.
- **Selected:** Use exactly:
  `POST /api/v1/platform/provisioning`,
  `GET /api/v1/platform/provisioning/{runId}`, and
  `POST /api/v1/platform/provisioning/{runId}/retry`. The full request,
  response, errors, idempotency, state mapping, authorization, and audit are
  in `03_PROVISIONING_API_CONTRACT.md`. No result, migration, seed, cancel,
  database-create, or compatibility route is added.
- **Impact:** Start returns `202`; status is the operation result; retry is
  same-run and controlled.

## P7-D-015 - Platform Admin Web

- **Problem:** Provisioning needs safe progress and failure UX without a
  mobile infrastructure surface.
- **Evidence:** Final human approval and Phase 2 Web/Flutter catalogs.
- **Selected:** Provisioning is Platform Admin Web-only. `PA-W-004` displays
  status, progress, success, failure, safe retry, and all required loading,
  empty, validation, error, RTL, Arabic, theme, responsive, and accessibility
  states through the canonical API.
- **Impact:** No Flutter route or screen is created; no secrets appear in UI.
