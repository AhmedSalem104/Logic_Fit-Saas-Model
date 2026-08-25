# TOP GYM Source-of-Truth Audit

**Audit date:** 2026-08-25  
**Status:** GREEN â€” audit evidence consolidated; all Phase 1 conflicts are resolved or explicitly classified.  
**Scope:** repository, runtime, SQL schema/migrations, data, screens, flows, training, nutrition, members, measurements, APIs, permissions, media, print/PDF, tests, and configuration.

## Executive result

TOP GYM is an operational legacy application implemented as a Node.js/Express modular monolith with SQL Server persistence and a Vanilla JavaScript/HTML/CSS frontend. It contains real member, membership, attendance, library, training, nutrition, finance, store, backup, portal, and permission behavior.

It is a source of legacy behavior and canonical data evidence only. It is not the LogicFit target architecture: there is no Control Plane plus database-per-gym strategy, no native Flutter client, no React/Vite client, no tenant isolation model, and no LogicFit migration/seed runner.

## Repository and runtime

| Finding | Evidence |
|---|---|
| Express entrypoint and static frontend | `server.js`, `src/app.js`, `public/index.html` |
| Routes â†’ controllers â†’ services/repositories â†’ SQL Server | `src/routes/index.js`, `docs/ARCHITECTURE.md` |
| SQL Server driver and one application pool | `package.json`, `src/database/pool.js` |
| Vanilla JS hash-navigation shell with lazy feature loaders | `public/js/page-tabs.js`, `public/js/feature-loader.js` |
| Local JSON library and local image/anatomy assets | `data/library/`, `public/data/`, `public/assets/` |

## Business evidence captured

- Members: member identity, membership lifecycle, freezes, renewals, payments, receipts, membership portal code, events, and dashboard statuses.
- Measurements: the exact `body_measurements` fields are `measured_at`, `weight_kg`, `height_cm`, `body_fat_percent`, `chest_cm`, `waist_cm`, `hips_cm`, `arms_cm`, `thighs_cm`, and `notes`.
- Training: workout programs, routines, exercises, canonical exercise IDs, sets/reps/weight/rest, RIR/RPE runtime support, tempo/superset/notes, sessions, set logs, and progress summaries.
- Nutrition: diet plans, meals, canonical food IDs, quantity/unit, macro snapshots, targets, calculator fields, meal logs, and progress summaries.
- Library: 873 exercises, 297 muscles, and 367 foods in the inspected local datasets.
- Print/PDF: member reports, receipts, pricing, coaching systems/overview, store receipts, and portal print behavior.

## Confirmed blockers and conflicts

### BLOCKED: SPECIFICATION GAP â€” missing audit prompt

The user-requested `05_TOP_GYM_SOURCE_AUDIT/60_TOP_GYM_SOURCE_AUDIT_PROMPT.md` is absent from the Master Bible package. Only `05_TOP_GYM_SOURCE_AUDIT/35_TOPGYM_SOURCE_AUDIT.md` exists. The available file was followed, but the filename/version relationship is not specified.

### BLOCKED: SPECIFICATION CONFLICT â€” schema.sql versus runtime coaching schema

`database/schema.sql` defines the baseline coaching tables, while `src/services/coaching-service.js` conditionally adds or creates runtime structures/columns, including RIR/RPE, diet calculator columns, `athlete_checkins`, and `coaching_activity_events`. The sources do not describe one approved canonical migration authority for these differences. The audit does not choose one.

### BLOCKED: SPECIFICATION CONFLICT â€” canonical library versus mutable CRUD

`library-service.js` seeds library rows from JSON, but its CRUD paths can update/delete rows without a canonical/custom ownership boundary. The LogicFit contract requires canonical datasets, stable seed keys, idempotent versioned seeds, and protection of organization-owned data. A parity rule cannot be inferred from TOP GYM alone.

### BLOCKED: SPECIFICATION GAP â€” review/publish semantics

TOP GYM has draft/active/paused/completed/archived status values and UI actions that look like review/publish actions, but no explicit human-approval/publish transition and immutable published snapshot model was found in the runtime. LogicFit requires that behavior. No mapping is invented.

### BLOCKED: SPECIFICATION GAP â€” native mobile parity

The repository contains responsive Web UI but no Flutter/Dart iOS/Android client. No mobile-specific legacy behavior can be extracted.

### BLOCKED: SPECIFICATION GAP â€” CRM parity

The inspected route modules do not provide a leads/pipeline/activities/follow-up/conversion CRM implementation. The LogicFit CRM scope therefore has no TOP GYM behavior to copy.

## QA result

Read-only/static checks executed against TOP GYM:

- Unit tests: 10 passed, 0 failed (`permissions.test.js`, `intelligence.test.js`).
- Exercise catalog validator: passed; 873 active records, unique IDs/slugs, 873 image pairs.
- Exercise content validator: passed; no critical issues; 873 exercises and 297 resolvable muscles.
- Muscle asset validator: passed; 297 records, 214 mapped, 83 manual-review, 188 canonical structures.
- JavaScript syntax: 110 files checked, 0 failures.
- Database migrations, smoke tests, authenticated E2E, restore, and browser runtime tests were not executed because they could write to or require operational test state in the legacy system.

## Lead decision

The audit deliverables are produced for review. No LogicFit feature implementation, migration, seed, API contract approval, or parity decision is authorized until the listed blockers are resolved against the Master Bible, Decision Lock, approved DOCS, and this evidence set.

## Source Consolidation Addendum - 2026-08-25

The audit-time blockers are superseded by the Lead decision records in this directory. Runtime SQL is the current TOP GYM database truth; the missing seed path is a legacy defect; 873 active plus 265 legacy rows are preserved; stable keys, LogicFit lifecycles, meal cardinality, enum scope, and local Print/PDF behavior are documented. Flutter, CRM, Documents, Control Plane, per-Gym databases, and tenant isolation are LogicFit scope rather than TOP GYM defects. The previously pending product decisions are approved as SC-015 Nutrition Calculation Engine, SC-016 Permission-Based Approval, and SC-017 Public QR Privacy. **PHASE 1 GREEN.** TOP GYM remains unmodified and Phase 2 is not started in this task.

