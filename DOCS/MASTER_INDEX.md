# LogicFit Project Documentation Index

This is the project implementation index under the official LogicFit root. The external Master Bible and Decision Lock remain the higher authority; this file does not replace them.

| Phase | Status | Entry point |
|---|---|---|
| Phase 0 — Orientation | GREEN | `PHASE_0_ORIENTATION_REPORT.md` |
| Phase 1 — Audit/Source Consolidation | GREEN | `PHASE_1_SOURCE_CONSOLIDATION_REPORT.md` |
| Phase 2 — Contracts | GREEN | `PHASE_2/PHASE_2_STATUS_REPORT.md` |
| Phase 3 — Canonical Seed Data | GREEN | `PHASE_3/PHASE_3_STATUS_REPORT.md` |
| Phase 4 — Local Technical Foundation | GREEN | `PHASE_4/PHASE_4_STATUS_REPORT.md` |
| Phase 5 — Backend Architecture Correction | GREEN — official .NET backend | `PHASE_5/PHASE_5_STATUS_REPORT.md` |
| Phase 5B Authentication + RBAC | IN PROGRESS — API/Web/Flutter implementation and automated tests passing; final UAT gate pending | `PHASE_5/PHASE_5_STATUS_REPORT.md` |
| Phase 6+ | NOT STARTED | Requires explicit Lead authorization |

Key authorities:

- Phase 2 database/API contracts: `PHASE_2/01_DATABASE_CONTRACT.md`, `PHASE_2/03_API_CONTRACT.md`.
- Phase 3 seed package: `PHASE_3/02_SEED_MANIFEST.md`, `PHASE_3/09_SEED_RUNNER.md`.
- Phase 4 local runbook: `PHASE_4/11_LOCAL_DEVELOPMENT.md`.
- Repository structure and IDE prerequisite: `PHASE_4/01_REPOSITORY_ARCHITECTURE.md`, `PHASE_4/13_ENVIRONMENT_CONFIGURATION.md`.
- Official backend correction: `PHASE_5/01_BACKEND_CORRECTION.md`.
- Official migration/seed transition: `PHASE_5/07_MIGRATION_TRANSITION.md`, `PHASE_5/08_SEED_TRANSITION.md`.
- Phase 5B Authentication/RBAC API addendum: `PHASE_2/21_AUTH_RBAC_API_CONTRACT_ADDENDUM.md`, decision record `PHASE_2/decisions/21_AUTH_RBAC_API_DECISIONS.md`.
- Phase 5B implementation traceability: `PHASE_5/12_AUTH_RBAC_TRACEABILITY.md`.
- Phase 5B implementation records: `PHASE_5/13_AUTH_IMPLEMENTATION.md` through `PHASE_5/21_AUTH_TEST_RESULTS.md`.

The official backend is ASP.NET Core Web API / C# / .NET 10 / EF Core / SQL Server in `LogicFit.sln`. The former Fastify/TypeScript backend was draft/non-canonical and has been removed. React and Flutter remain the approved clients.

No business module is GREEN until its vertical-slice gate is separately completed.

Phase 5B implementation update (2026-08-26): the approved Auth/RBAC API, Web flows, automated suites, and direct Chrome verification are passing. Flutter analyzer/widget/Windows launch verification is passing; interactive Android/iOS authentication UAT and the reviewed Git checkpoint remain pending. Phase 6 and all business modules remain unauthorized.
