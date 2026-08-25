# TOP GYM → LogicFit Gap Report

**Audit date:** 2026-08-25  
**Status:** GREEN for Phase 1 source consolidation. Phase 2 and Phase 3 were later authorized as separate contract/seed tasks; Phase 3 is GREEN. This report remains a gap classification record, not a feature implementation record.

## Consolidated gap matrix

| Area | TOP GYM evidence | LogicFit authority | Classification |
|---|---|---|---|
| Frontend | Vanilla HTML/CSS/JS hash SPA | React + TypeScript + Vite + Tailwind + Router + Query | **BLOCKED: SPECIFICATION GAP** for mobile/web parity contract; legacy behavior can be audited only. |
| Mobile | No Flutter/Dart client | Real Flutter iOS/Android app where approved | **BLOCKED: SPECIFICATION GAP** — no legacy mobile evidence. |
| Database scope | One SQL Server connection/pool and custom tables | Control Plane DB + database per gym | **BLOCKED: SPECIFICATION GAP** — no migration from legacy scope can be assumed. |
| Tenant isolation | No tenant/org/gym predicates in custom code | Mandatory tenant isolation | **BLOCKED: SPECIFICATION GAP** / security risk. |
| Migrations | schema.sql + 005/006/007 + runtime ensure DDL; no history registry | Versioned, validated migration system | **BLOCKED: SPECIFICATION CONFLICT** — authority is not unified. |
| Library seed path | Runtime resolves `src/data/library`; actual files are root `data/library` | Deterministic versioned seed runner | **BLOCKED: SPECIFICATION CONFLICT** — static defect verified. |
| Seed identity | Exercise source IDs; muscle/food source IDs derived from array order | Stable `seed_key`, no overwrite of custom data | **BLOCKED: SPECIFICATION GAP**. |
| Exercise catalog | 873 current + 265 legacy compatibility; docs contain historical count conflicts | TOP GYM audit evidence then approved canonical LogicFit seed | **BLOCKED: SPECIFICATION CONFLICT** — 265 vs 873 narrative must be versioned. |
| Taxonomies | Equipment/category/level/body part stored as values; no normalized groups/tables | Canonical seed datasets named by Bible | **BLOCKED: SPECIFICATION GAP**. |
| Training schema | RIR/RPE runtime-only additions absent from schema.sql | SQL Server canonical schema/migrations | **BLOCKED: SPECIFICATION CONFLICT**. |
| Nutrition schema | Calculator columns runtime-only | SQL Server canonical schema/migrations | **BLOCKED: SPECIFICATION CONFLICT**. |
| Calculations | Manual UI and intelligence use different BMR/TDEE/macro paths | Backend authoritative calculations | **RESOLVED — SC-015**; verified manual formula evidence is separated from the approved versioned LogicFit engine. |
| Training lifecycle | Generic draft/active/etc.; no approval/publish/snapshot | Human review/approval/publish/snapshot | **RESOLVED — SC-007 and SC-016**; LogicFit lifecycle and permission matrix are approved. |
| Nutrition lifecycle | Same generic status; no review/approval/publish/snapshot | Canonical nutrition plan lifecycle | **RESOLVED — SC-008 and SC-015**; lifecycle and calculation engine are approved. |
| Canonical protection | Library CRUD can mutate/delete seeded rows; no org/custom boundary | Canonical data protected from organization-owned data | **BLOCKED: SPECIFICATION GAP**. |
| Measurements | Exact legacy set captured; check-ins runtime-only | No invented measurements; approved LogicFit model | Evidence usable; runtime schema conflict remains. |
| Members/privacy | Portal code; `/qr/:id` privacy boundary unresolved in legacy evidence; no documents subsystem | Backend security, storage adapter, tenant isolation | **RESOLVED — SC-017** for the LogicFit QR contract; Documents and tenant isolation remain LogicFit Phase 2 scope, not TOP GYM parity behavior. |
| CRM | No leads/pipeline/activity/conversion route/service found | CRM required in LogicFit scope | **BLOCKED: SPECIFICATION GAP** — no parity source. |
| Print/PDF | Browser print + html2pdf CDN; portal PDF label calls print | Local Arabic/RTL PDF core, adapters allowed | **BLOCKED: SPECIFICATION GAP** — offline behavior not proven. |
| External integrations | Manual WhatsApp; external fonts/CDN; no paid AI/SMS/WhatsApp API | Adapter boundary, local-first/no paid dependency | **BLOCKED: SPECIFICATION GAP** for LogicFit implementation. |
| Platform control plane | No Organizations/Servers/Provisioning/Global Migration center | Approved Platform Admin scope | **BLOCKED: SPECIFICATION GAP**. |
| Backup/restore | Legacy custom compressed JSON archive and runtime routes | Local testable backup/restore and approved production runbook | Behavior evidence only; **BLOCKED: SPECIFICATION GAP** for parity. |
| QA | Shared unit/validators; no full training/nutrition/migration/multi-DB suite executed | Full unit/integration/API/E2E/security/seed/migration/UAT | **BLOCKED: SPECIFICATION GAP**. |

## Explicit conflict list for Lead decisions

1. Static `database/schema.sql` versus runtime coaching DDL.
2. Legacy docs versus current auth/RBAC code.
3. Legacy docs/counts versus current 873+265 catalog behavior.
4. Root data layout versus `library-service.js` path resolution.
5. Manual calculator versus intelligence calculator.
6. UI meal count versus backend meal count.
7. Secondary muscle ID namespaces between read/write paths.
8. `expert` dataset difficulty versus `advanced` UI/AI vocabulary.
9. Generic `active` status versus LogicFit approval/publish semantics.
10. Portal PDF label versus actual browser-print behavior.

Per the Master Bible and user instruction, none of these conflicts is resolved by choosing an implementation source ad hoc.

## What can be reused as evidence

- Exact member and measurement fields.
- Actual training/nutrition flow shape and observed UI field names.
- Actual API route inventory and permission families, subject to redesign.
- Extracted datasets and media manifests, subject to stable-key/seed approval.
- Legacy print content expectations (Arabic, RTL, A4, exercise references).

## What must not be copied without approval

- Single-database architecture or absence of tenant predicates.
- Runtime DDL as a migration strategy.
- Array position as canonical identity.
- Frontend calculations as business authority.
- Generic `active` as publish/approval.
- External CDN/PDF assumptions.
- Legacy permission names as the LogicFit RBAC model.

## Gate

Phase 1 audit documentation is complete for review. LogicFit business implementation, API/DB contract approval, seed creation, and Phase 2 are not authorized by this report.

## Source Consolidation Reclassification — 2026-08-25

The classifications above were the audit-time state. The Lead source-consolidation decisions now apply:

| Gap family | Final classification | Consolidated outcome |
|---|---|---|
| Schema versus runtime DDL | Runtime truth for TOP GYM; LogicFit product architecture | Runtime SQL is current legacy truth; `schema.sql` is evidence; LogicFit mapping is separate. |
| Seed path and source identity | Legacy defect + LogicFit data requirement | Use verified root `data/library`; deterministic seed keys are documented; TOP GYM is unchanged. |
| 873 versus 1,138 exercises | Runtime truth | 873 active candidates and 265 legacy-compatibility records are both preserved with provenance. |
| Training/nutrition lifecycle | LogicFit product feature | Draft → Review → Approval → Published → Snapshot/Version for all creation modes. |
| PDF label/print behavior | Legacy behavior + LogicFit product decision | TOP GYM portal behavior is print; LogicFit local Print + PDF is required. |
| Meal cardinality | LogicFit product rule | Minimum 1, maximum 12; common UI/default 3–6. |
| `expert`/`advanced` | Separate concepts | ExerciseDifficulty and PlanLevel remain distinct. |
| Flutter, CRM, Documents, Control Plane, database-per-Gym | LogicFit scope/architecture | These are not TOP GYM defects or parity conflicts. |

See `SOURCE_CONSOLIDATION_DECISIONS.md` for the complete register. The formerly pending product decisions are now approved: SC-015 Nutrition Calculation Engine, SC-016 Permission-Based Approval Matrix, and SC-017 Public QR Privacy Contract. They are LogicFit contracts for later implementation, not TOP GYM behavior.

## Gate

Source consolidation and product-decision closure are complete. **PHASE 1 GREEN.** Phase 2 remains stopped in this task and requires a separate Lead command.
