# Phase 8 Members Implementation

**Status:** GREEN for the approved Members core implementation.
**Date:** 2026-08-30

## Scope delivered

The implementation contains only the approved Gym-scoped Members core:

- list/search/status filter/pagination;
- create with server-generated Gym-unique Member Code;
- detail;
- full PUT profile/status update;
- history-preserving DELETE archive;
- Member-domain timeline;
- server-side RBAC and Gym isolation;
- create idempotency and optimistic concurrency;
- safe audit events; and
- MEM-W-001/002/003 plus F-MEM-001/002 client surfaces.

No Member seed data was added. No membership, attendance, measurement,
training, nutrition, finance, store, CRM, class, document, notification, or
other future-module table/API/UI was added. F-MEM-003 remains Attendance scope.

## Backend and persistence

The implementation is split according to the existing solution boundaries:

- Domain: `src/LogicFit.Domain/Members/Member.cs`.
- Application contracts: `src/LogicFit.Application/Members/MembersContracts.cs`.
- Infrastructure/use case implementation:
  `src/LogicFit.Infrastructure/Members/MembersService.cs`.
- Gym persistence entities:
  `src/LogicFit.Infrastructure/Persistence/Gym/Entities/GymEntities.cs`.
- EF model: `src/LogicFit.Infrastructure/Persistence/Gym/GymDbContext.cs`.
- API controller/result projection:
  `src/LogicFit.Api/Controllers/MembersController.cs` and
  `src/LogicFit.Api/Members/MembersApiResults.cs`.

The Gym database contains `members.members` and
`members.timeline_events`. The Member table uses a stable UUID, logical Gym
scope, normalized profile fields, explicit ACTIVE/INACTIVE/ARCHIVED status,
rowversion, and a database uniqueness constraint for Gym plus Member Code.
The create idempotency hash and request fingerprint are stored as hashes only.
No credential or authentication secret is stored in the Gym database.

The EF migration is:

`20260830084936_Phase8Members`

It is the only schema change for Phase 8 and was applied through EF Core.
Archive never calls `Remove` and never issues a physical delete in the
application path.

## API behavior

Exactly these routes are implemented:

1. `GET /api/v1/gyms/{gymId}/members`
2. `POST /api/v1/gyms/{gymId}/members`
3. `GET /api/v1/gyms/{gymId}/members/{memberId}`
4. `PUT /api/v1/gyms/{gymId}/members/{memberId}`
5. `DELETE /api/v1/gyms/{gymId}/members/{memberId}`
6. `GET /api/v1/gyms/{gymId}/members/{memberId}/timeline`

The API uses the existing response/error envelopes and request-id middleware.
All requests authorize the current Phase 5B session, requested Gym, and
permission before resolving the Gym database. Platform Admin does not receive
implicit access to Gym Member data.

Create requires `Idempotency-Key`. Equivalent replay returns the existing
resource; conflicting reuse returns `409 DUPLICATE_RESOURCE`. Gym-unique
Member Code conflicts are handled by the database constraint. PUT and current
DELETE require the opaque `If-Match` row version; stale writes return
`409 CONCURRENCY_CONFLICT`. DELETE is idempotent for an already archived
Member and returns the archive projection.

The list defaults to ACTIVE plus INACTIVE, page size 25, maximum 100, and
createdAt descending with a stable Member ID tie-breaker. ARCHIVED requires an
explicit status filter. Search is limited to the approved Member Code,
canonical full-name projection, normalized phone, and normalized email
fields. Empty normalized phone search terms are never used as a wildcard.

Timeline output is restricted to MEMBER_CREATED, MEMBER_UPDATED,
MEMBER_ARCHIVED, and MEMBER_STATUS_CHANGED with allowlisted metadata.

## Authorization and audit

The canonical permission catalog now contains the approved five Members
permissions. Existing roles are unchanged:

- `gym-security-admin`: all five Members permissions;
- `gym-authenticated-user`: `members.read` only;
- `platform-security-admin`: no automatic Gym Member business-data access.

The service records safe Member audit actions through the existing audit
repository. Audit metadata contains only identifiers, status transitions, and
allowlisted changed-field information. Passwords, password hashes, MFA
secrets, recovery codes, session tokens, credentials, connection strings, and
private keys are not returned, logged, or audited.

## Web

The existing React application implements the three contracted Web screens in
`apps/web/src/components/MemberPages.tsx`, with API methods in
`apps/web/src/lib/api.ts` and routes in `apps/web/src/App.tsx`.

The Web flow provides list/search/filter/paging, create, detail, edit, archive,
timeline, validation, loading/empty/error/success states, permission-aware
actions, Arabic/RTL, Light/Dark themes, and responsive layout. It uses the
existing API client and never accesses SQL Server.

## Flutter

The existing Flutter application implements only F-MEM-001 and F-MEM-002 in
`apps/mobile/lib/members.dart`, using the existing Dio/API configuration and
session token. It provides safe list/detail/timeline reads, search/status
filter, loading/empty/error/retry states, Arabic/RTL, and Light/Dark themes.
No F-MEM-003 Attendance route or screen was added.

An Android/iOS device was not connected during the final verification run;
the project was nevertheless compiled for Android and Windows and all Flutter
tests passed. This is an environment limitation, not an application claim of
interactive mobile E2E.

## Deferred contract items

`members.export` remains a contracted permission with implementation deferred
as approved. Member Portal authentication and access-code issuance remain
governed by their existing contract; no Portal route or second credential
system was added.
