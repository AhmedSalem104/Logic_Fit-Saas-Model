# Phase 8 Members Decision Register

**Status:** GREEN — P8-G-001 through P8-G-006 explicitly resolved

## P8-D-001 — Member lifecycle and archive

- **Problem:** `status` and DELETE behavior needed a canonical lifecycle.
- **Approved decision:** statuses are `ACTIVE`, `INACTIVE`, and `ARCHIVED`; DELETE means archive; there is no physical delete or Phase 8 restore API.
- **Impact:** `members.members`, PUT/DELETE APIs, list filtering, audit, Web/Flutter, security tests.
- **Resolves:** P8-G-001.

## P8-D-002 — Members role grants

- **Problem:** The five permission identifiers lacked concrete grants for the existing roles.
- **Approved decision:** `gym-security-admin` gets all five; `gym-authenticated-user` gets `members.read`; `platform-security-admin` gets no automatic Gym Member access.
- **Impact:** Phase 5B RBAC projection/seed implementation later, API authorization, clients, tests.
- **Resolves:** P8-G-002.

## P8-D-003 — Duplicate, uniqueness, idempotency, and concurrency

- **Problem:** Member creation and concurrent mutation behavior was unspecified.
- **Approved decision:** Member ID is system-generated/immutable; the existing Portal contract's Member Code is unique within a Gym and is not changed by normal PUT; phone/email are not globally unique; equivalent Idempotency-Key replay returns the original result; conflicting replay and duplicate code use existing `409 DUPLICATE_RESOURCE`; stale PUT/DELETE uses `409 CONCURRENCY_CONFLICT`; archive is idempotent.
- **Impact:** Constraints/indexes, API, EF transaction boundaries, audit, tests.
- **Resolves:** P8-G-003.

## P8-D-004 — Canonical API verb, DTOs, and query contract

- **Problem:** Complete Member API schemas were absent and older references used PATCH.
- **Approved decision:** Members use GET collection, POST collection, GET item, PUT item, DELETE item/archive, and GET item/timeline. Page default is 25, maximum 100; default sort is `createdAt` descending with stable Member ID tie-breaker; approved search fields are `memberCode`, `firstName`, `lastName`, `displayName`, `phone`, and `email`; default status excludes ARCHIVED. DTOs and safe field allowlists are defined in `03_MEMBERS_API_CONTRACT.md`.
- **Precedence:** The explicit final Phase 8 human approval supersedes the older PATCH shorthand for this Members contract. `DOCS/PHASE_2/04_API_ENDPOINT_CATALOG.md`, `08_USER_FLOW_CONTRACT.md`, `15_MEMBER_CONTRACT.md`, and the Phase 5 readiness reference are reconciled to PUT; no runtime implementation changes are made.
- **Impact:** API catalog, Web/Flutter calls, validation, tests, documentation.
- **Resolves:** P8-G-004 and documentation drift.

## P8-D-005 — Member-domain timeline

- **Problem:** The earlier timeline description included future-domain events.
- **Approved decision:** core events are exactly `MEMBER_CREATED`, `MEMBER_UPDATED`, `MEMBER_ARCHIVED`, and `MEMBER_STATUS_CHANGED`; order is `occurredAt` descending then `eventId` descending; pagination is default 25/max 100; metadata is safe and allowlisted.
- **Impact:** timeline table/projection, API, privacy, clients, tests.
- **Resolves:** P8-G-005.

## P8-D-006 — Core profile boundary

- **Problem:** Existing screen catalogs listed future-domain tabs in the Member profile.
- **Approved decision:** Phase 8 profile exposes only core identity/profile, status, Member Code where contractually required, and Member timeline. Memberships, Attendance, Measurements, Training, Nutrition, Finance, Documents, and other modules have no implementation or placeholder.
- **Impact:** Web/Flutter screen content, API calls, privacy, E2E.
- **Resolves:** P8-G-006.

## P8-D-007 — Export

- **Problem:** `members.export` exists without an approved endpoint/output contract.
- **Approved decision:** retain the permission assignment but mark export `CONTRACTED PERMISSION / IMPLEMENTATION DEFERRED`; do not add a route.
- **Impact:** permission traceability only.
- **Status:** Resolved as deferred.

## P8-D-008 — Portal and seed boundaries

- **Problem:** Protect Portal authentication and prevent operational demo data.
- **Approved decision:** preserve the existing Member Code Portal flow and safe projection; no Portal auth change; no Member seed data.
- **Impact:** future Portal contract, provisioning initialization, database tests.
- **Status:** Resolved.
