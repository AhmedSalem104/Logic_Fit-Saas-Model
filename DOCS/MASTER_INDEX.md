# LogicFit Project Documentation Index

This is the project implementation index under the official LogicFit root.
For Phase 6, P6-D-001 approves the existing repository documentation as the
authoritative working source; absent root kickoff files are not reconstructed.

| Phase | Status | Entry point |
|---|---|---|
| Phase 0 - Orientation | GREEN | `PHASE_0_ORIENTATION_REPORT.md` |
| Phase 1 - Audit/Source Consolidation | GREEN | `PHASE_1_SOURCE_CONSOLIDATION_REPORT.md` |
| Phase 2 - Contracts | GREEN | `PHASE_2/PHASE_2_STATUS_REPORT.md` |
| Phase 3 - Canonical Seed Data | GREEN | `PHASE_3/PHASE_3_STATUS_REPORT.md` |
| Phase 4 - Local Technical Foundation | GREEN | `PHASE_4/PHASE_4_STATUS_REPORT.md` |
| Phase 5 - Backend Architecture Correction | GREEN - official .NET backend | `PHASE_5/PHASE_5_STATUS_REPORT.md` |
| Phase 5B Authentication + RBAC | YELLOW - implementation complete; Android interactive Flutter UAT pass; iOS interactive UAT unavailable on Windows; final gate pending | `PHASE_5/PHASE_5_STATUS_REPORT.md` |
| Phase 6 - Platform Foundation | YELLOW - read-only implementation complete; Chrome adapter verification pending | `PHASE_6/PHASE_6_STATUS_REPORT.md` |
| Phase 7 - Gym Provisioning | GREEN - implementation and direct Chrome verification complete; external adapter limitation documented | `PHASE_7/PHASE_7_STATUS_REPORT.md` |
| Phase 8 - Members | BLOCKED - contract audit gaps recorded; implementation not authorized | `PHASE_8/PHASE_8_CONTRACT_STATUS_REPORT.md` |

## Key authorities

- Phase 2 database/API contracts: `PHASE_2/01_DATABASE_CONTRACT.md`,
  `PHASE_2/03_API_CONTRACT.md`.
- Phase 3 seed package: `PHASE_3/02_SEED_MANIFEST.md`,
  `PHASE_3/09_SEED_RUNNER.md`.
- Phase 4 local runbook: `PHASE_4/11_LOCAL_DEVELOPMENT.md`.
- Repository structure and IDE prerequisite:
  `PHASE_4/01_REPOSITORY_ARCHITECTURE.md`,
  `PHASE_4/13_ENVIRONMENT_CONFIGURATION.md`.
- Official backend correction: `PHASE_5/01_BACKEND_CORRECTION.md`.
- Official migration/seed transition: `PHASE_5/07_MIGRATION_TRANSITION.md`,
  `PHASE_5/08_SEED_TRANSITION.md`.
- Phase 5B Authentication/RBAC API addendum:
  `PHASE_2/21_AUTH_RBAC_API_CONTRACT_ADDENDUM.md`, decision record
  `PHASE_2/decisions/21_AUTH_RBAC_API_DECISIONS.md`.
- Phase 5B implementation traceability: `PHASE_5/12_AUTH_RBAC_TRACEABILITY.md`.
- Phase 5B implementation records: `PHASE_5/13_AUTH_IMPLEMENTATION.md` through
  `PHASE_5/21_AUTH_TEST_RESULTS.md`; final verification/readiness:
  `PHASE_5/22_FINAL_VERIFICATION_AND_READINESS.md`.
- Phase 6 contract closure: `PHASE_6/PHASE_6_CONTRACT_STATUS_REPORT.md`,
  `PHASE_6/11_PHASE_6_CONTRACT_GAP_REGISTER.md`,
  `PHASE_6/10_PHASE_6_DECISIONS.md`, and
  `PHASE_6/decisions/00_DECISION_INDEX.md`.
- Phase 7 provisioning contract audit: `PHASE_7/PHASE_7_CONTRACT_STATUS_REPORT.md`,
  `PHASE_7/10_PHASE_7_DECISIONS.md`, and
  `PHASE_7/11_PHASE_7_CONTRACT_GAP_REGISTER.md`.
- Phase 8 Members contract audit: `PHASE_8/PHASE_8_CONTRACT_STATUS_REPORT.md`,
  `PHASE_8/15_PHASE_8_DECISIONS.md`, and
  `PHASE_8/16_PHASE_8_GAP_REGISTER.md`.

The official backend is ASP.NET Core Web API / C# / .NET 10 / EF Core / SQL
Server in `LogicFit.sln`. The former Fastify/TypeScript backend was
draft/non-canonical and has been removed. React and Flutter remain the
approved clients.

No business module is GREEN until its vertical-slice gate is separately
completed.

Phase 5B verification update (2026-08-27): the approved Auth/RBAC API, Web
flows, automated suites, direct Chrome verification, and Android interactive
Flutter UAT for the available mobile scope are passing. iOS interactive UAT
is unavailable on this Windows workstation. Phase 6 implementation and all
business modules remain unauthorized.

Phase 6 contract-closure update (2026-08-28): P6-D-001 through P6-D-010 are
approved. The Phase 6 Contract Gate is GREEN for the bounded read-only
Platform Foundation contract; implementation now exists and its final gate is
YELLOW only because the external Chrome browser adapter could not initialize.
See `PHASE_6/12_PLATFORM_IMPLEMENTATION.md`,
`PHASE_6/13_PLATFORM_API_IMPLEMENTATION.md`,
`PHASE_6/14_PLATFORM_WEB_IMPLEMENTATION.md`,
`PHASE_6/15_PLATFORM_TEST_RESULTS.md`, and
`PHASE_6/PHASE_6_STATUS_REPORT.md`.

Phase 7 contract-closure update (2026-08-29): P7-D-001 through P7-D-015 have
been recorded from the final human approval. The closed contract covers
asynchronous Gym provisioning, organization/Gym registry creation, registered
server placement, deterministic database naming, EF migration and Phase 3
seed orchestration, verification, Owner initialization, lifecycle/retry,
audit, and the three existing provisioning routes. `platform.provision` is
approved as a Phase 7 permission and is a forward contract change; the
Phase 5B runtime catalog remains unchanged until implementation. The final
human approval resolves P7-G-023 by mapping Gym Owner to the existing
`gym-security-admin` role. No new role, alias, rename, or unrelated grant is
introduced. The authorized implementation now adds the provisioning Control
Plane model/migrations, local asynchronous workflow, exactly three APIs, and
the Platform Admin Web flow. Phase 3 seed data and TOP GYM remain unchanged;
the implementation and verification record is in `PHASE_7/PHASE_7_STATUS_REPORT.md`.

Phase 8 Members contract-audit update (2026-08-29): the locked core scope is
Member list/create/detail/update/history-preserving delete-or-archive/timeline
in the Gym database, with the existing five `members.*` permission identifiers,
MEM-W-001 through MEM-W-003, and F-MEM-001/F-MEM-002. No Member seed data or
membership/attendance/business-module behavior is included. The contract is
BLOCKED by the exact lifecycle/delete semantics, concrete role grants,
duplicate/idempotency policy, complete DTO/query schemas, timeline event scope,
and the Phase 2 profile-tab boundary. See `PHASE_8/PHASE_8_CONTRACT_STATUS_REPORT.md`.
