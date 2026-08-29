# Phase 7 - Gym Provisioning Scope

**Status:** GREEN - Phase 7 provisioning implementation and final local gate verification passed.
**Date:** 2026-08-29
**Task:** Authorized Phase 7 implementation; no Phase 8 or business-module work is included.

## Authority

The repository documentation is authoritative. The applicable evidence is:

- `../MASTER_INDEX.md`;
- Phase 2 database, API, permission, flow, Web, Flutter, seed, and traceability contracts;
- Phase 3 canonical seed contracts;
- Phase 4 local foundation contracts;
- Phase 5 authentication/RBAC contracts;
- Phase 6 Platform Foundation contracts; and
- the final human approvals P7-D-001 through P7-D-015 recorded in
  `10_PHASE_7_DECISIONS.md`.

The root files `DECISION_LOCK.md`, `IMPLEMENTATION_ROADMAP.md`, and
`CODEX_KICKOFF.md` are absent. P6-D-001 says not to reconstruct them; their
absence is not itself a Phase 7 blocker.

## Phase 7 purpose

Phase 7 owns the execution of a new Gym provisioning operation:

1. validate and accept a new organization/Gym request;
2. create the Control Plane organization and Gym registry records;
3. select registered server placement;
4. create one isolated Gym database;
5. apply the official EF Core Gym migrations;
6. run the unchanged Phase 3 canonical library seed;
7. verify the database and Gym context;
8. initialize the first Gym Owner through Phase 5B authentication/RBAC; and
9. activate the Gym only after all required steps succeed.

The workflow is asynchronous. Its public surface is limited to the three
routes in `03_PROVISIONING_API_CONTRACT.md`.

## Explicit exclusions

Phase 7 does not include:

- Members, memberships, attendance, or any other business module;
- billing, payments, invoices, commercial subscription enforcement, or a payment provider;
- backup/restore or disaster-recovery execution;
- public migration, seed, database-create, cancel, or deprovisioning APIs;
- infrastructure provisioning automation, credentials, or private-key management;
- a second authentication, session, migration, seed, audit, or RBAC system;
- Platform Admin Flutter UI; or
- any change to TOP GYM.

## Classification audit

| Classification | Phase 7 result |
|---|---|
| A - locked | Control Plane plus database-per-Gym; Phase 6/7/8 boundaries; EF Core as the only migration mechanism; Phase 3 seed identity; no fresh-provisioning backup/restore; no Platform provisioning Flutter UI. |
| B - implied by a locked contract | Control Plane registry rows, `core.gym_context`, `auth.gym_users`, Owner scope, auditable run/step records, and safe status monitoring. |
| C - contract detail | Exact database columns, idempotency fingerprints, safe DTO fields, step representation, retry flags, and deterministic name normalization are specified in this package. |
| D - product decision required | None remains. The final human approval resolves P7-G-023 by mapping the Gym Owner to the existing `gym-security-admin` role. |
| E - architecture/contract conflict | None remains. No new role key, alias, rename, or unrelated role grant is introduced. |

## Gate result

The approved provisioning behavior is completely specified. The first Gym
Owner is the existing `gym-security-admin` role; no role alias, new role,
rename, or unrelated role grant is introduced by this contract-only update.

## Implementation result

The approved scope is implemented through the ASP.NET Core API, the existing
Control Plane/Gym EF Core model, the local single-reader provisioning worker,
the unchanged .NET canonical seed executor, and the Platform Admin Web flow.
The three approved provisioning routes are the only new public routes. No
Flutter provisioning UI, Phase 8 member data, or external infrastructure
automation was added.
