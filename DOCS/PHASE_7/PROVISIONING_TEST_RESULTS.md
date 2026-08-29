# Phase 7 Provisioning Test Results

**Status:** GREEN - implementation and final local verification pass; the external browser adapter remains a non-application limitation.
**Date:** 2026-08-29

## Backend and API

| Check | Result | Evidence |
|---|---|---|
| Solution build | PASS | CLI build: 0 warnings/0 errors; Visual Studio Community 2026 18.9.2: 8 succeeded, 0 failed |
| Unit tests | PASS | 5/5 |
| Integration tests | PASS | 2/2 |
| API tests | PASS | 32/32 |
| Isolated provisioning tests | PASS | `ProvisioningApiTests` 4/4 |
| Provisioning acceptance/idempotency | PASS | 202, same-key replay, different-body conflict |
| MFA step-up | PASS | missing verified MFA returns `403 MFA_REQUIRED` |
| Startup recovery | PASS | accepted operation is recovered and safely failed on invalid server |
| Provisioning success | PASS | database creation, EF migration, canonical seed, verification, Owner, activation |
| Owner isolation | PASS | `gym-security-admin` Owner receives `403` on Platform provisioning |

The isolated provisioning fixture creates uniquely named local test databases,
migrates/seeds them, and drops only those exact test databases during cleanup.
No test database remained after the run.

The Visual Studio build command exited successfully and compiled all eight
projects. Visual Studio's command-line NuGet restore manager emitted a
non-blocking design-time nomination diagnostic while the projects loaded; the
solution build itself completed with 8 succeeded/0 failed, and the supported
`dotnet restore`/CLI build resolved every package without warnings or errors.

## Database and seed checks

- Control Plane migration history contains the two Phase 7 migrations after
  the existing Phase 5/6 history.
- No pending EF model changes were reported.
- Local canonical runtime seed verification reports 16 permissions, 3 roles,
  15 role assignments, and 2,074 canonical library records.
- Existing Phase 3 reference counts remain 1,133 exercises, 297 muscles, 367
  foods, and 194 anatomy mappings.
- The main local Control Plane remains at one organization, one Gym, one
  server, one database registry, zero provisioning runs, 16 permissions, 3
  roles, and 15 role-permission assignments. The main local Gym remains at one
  context, 11 seed installations, and the four canonical reference counts.
- The Control Plane EF history is `InitialControlPlaneFoundation`,
  `Phase6PlatformAuditScope`, `Phase7ProvisioningFoundation`, and
  `Phase7LifecycleStates`; the Gym history remains `InitialGymFoundation`.
- The Phase 2-reserved `migrations.*` catalog tables remain untouched, but no
  Phase 7 runtime code, runner, API, or worker uses them. New Gym provisioning
  uses only EF Core `MigrateAsync` and `dbo.__EFMigrationsHistory`.
- Running the .NET seed path twice is idempotent; no duplicate canonical
  records were observed.
- The Phase 3 seed package was not modified.

## Web and Flutter

- Web typecheck/build/tests passed after the provisioning page was added; the
  Web test suite is 10/10.
- The Web tests cover the approved API namespace, idempotency header, request
  redaction, active status, failure status, and retry.
- Flutter has no Phase 7 provisioning UI by contract. Final `flutter analyze`
  passed with no issues and `flutter test` passed 3/3.
- Direct Chrome verification passed on the isolated local Web/API pair. It
  covered invalid and valid login, the Platform Admin provisioning form, the
  `202` acceptance response, status polling, all ten successful steps, the
  Arabic/RTL status page, and the `Active` success state. Chrome reported no
  console errors or failed API requests.
- The in-app browser-control adapter still fails before page initialization
  with `Cannot redefine property: process`. This was not worked around in
  LogicFit and is classified as an external adapter limitation; direct Chrome
  is the application evidence.

## Security and scope checks

- No Fastify/Node backend, Node migration runner, or Node runtime seed path
  was added.
- Only `platform.provision` was added to the canonical runtime permission
  catalog; no role key was added and no Phase 3 identity changed.
- No Phase 8/member/business tables or APIs were created.
- TOP GYM was not modified.
