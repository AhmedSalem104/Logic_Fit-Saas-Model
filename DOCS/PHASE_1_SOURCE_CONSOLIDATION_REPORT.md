# Phase 1 Source Consolidation Report

**Date:** 2026-08-25  
**Phase:** 1 — TOP GYM Source Consolidation  
**Audit restart:** no  
**Phase 2:** not started

## 1. Status

**GREEN — conflict register and approved product-decision register are complete.**

The Phase 1 audit evidence was reused. TOP GYM was not modified.

## 2. Conflicts resolved

- Runtime SQL Server state versus `schema.sql`: runtime is current TOP GYM truth; `schema.sql` is evidence; LogicFit mapping is separate.
- `src/data/library` versus `data/library`: classified and resolved as a legacy source-path defect; actual verified data is the source for LogicFit extraction.
- Exercise count: 1,138 live rows = 873 active + 265 legacy compatibility; all are preserved in the canonical source mapping with status/provenance.
- Stable seed identity: deterministic, human-readable, SHA-256 disambiguated keys independent of numeric IDs and JSON order.
- Training and nutrition lifecycle: LogicFit canonical Draft → Review → Approval → Published → Snapshot/Version for all creation modes.
- PDF: TOP GYM portal behavior is browser print; LogicFit local professional Print + PDF is a separate product decision.
- Nutrition meals: min 1, max 12, common UI/default 3–6.
- `expert`/`advanced`: separate ExerciseDifficulty and PlanLevel concepts with a documented compatibility mapping.
- Nutrition Calculation Engine: `SC-015` approved; the versioned backend engine, formulas, rounding, test vectors, and immutable published snapshots are documented in `LOGICFIT_NUTRITION_CALCULATION_ENGINE_DECISION.md`.
- Permission-Based Approval: `SC-016` approved; the Training/Nutrition permission catalog, state matrix, role profiles, self-approval rule, and Platform Admin boundary are documented in `LOGICFIT_APPROVAL_PERMISSION_MATRIX.md`.
- Public QR privacy: `SC-017` approved; opaque random tokens, revocation/rotation, minimal allowlisted output, forbidden fields, tenant isolation, and security tests are documented in `LOGICFIT_PUBLIC_QR_PRIVACY_CONTRACT.md`.

## 3. Legacy defects/documentation drift

- Legacy library source-path defect in `library-service.js`.
- Stale TOP GYM technical descriptions of Auth/library counts.
- Static schema/runtime DDL divergence, retained as legacy architecture evidence and not repaired.
- TOP GYM's absence of Control Plane, per-Gym databases, tenant isolation, native Flutter, CRM, and document storage is not a TOP GYM defect against LogicFit scope; it is classified as new LogicFit architecture/product scope.

## 4. LogicFit decisions created

- `TOP_GYM_LOGICFIT_DATABASE_DECISION.md` and canonical alias.
- `TOP_GYM_LOGICFIT_CANONICAL_MAPPING.md`.
- `TOP_GYM_SEED_KEY_STRATEGY.md`.
- `LOGICFIT_TRAINING_LIFECYCLE_DECISION.md`.
- `LOGICFIT_NUTRITION_LIFECYCLE_DECISION.md`.
- `LOGICFIT_PRINT_PDF_DECISION.md`.
- `LOGICFIT_NUTRITION_MEAL_RULE.md`.
- `LOGICFIT_ENUM_MAPPING.md`.
- `LOGICFIT_NUTRITION_CALCULATION_ENGINE_DECISION.md`.
- `LOGICFIT_APPROVAL_PERMISSION_MATRIX.md`.
- `LOGICFIT_PUBLIC_QR_PRIVACY_CONTRACT.md`.
- `SOURCE_CONSOLIDATION_DECISIONS.md`.

## 5. Evidence reviewed

- Existing Phase 1 audit deliverables listed in `PHASE_1_STATUS_REPORT.md`.
- Read-only runtime snapshot for TOP GYM SQL Server.
- Read-only `sys.columns`, `metadata_json.catalogStatus`, exercise difficulty, and workout-plan level queries.
- TOP GYM source paths, JSON datasets, services, controllers, UI, database schema, and migrations already captured by the completed audit.
- Master Bible `DECISION_LOCK.md`, `MASTER_INDEX.md`, architecture/business-logic/seed docs, training/nutrition generator decisions, product feature map/checklist, and relevant feature scope docs.

## 6. Tests and checks run

- Read-only SQL metadata/status queries: passed; no DDL/DML/migration/seed.
- Existing Phase 1 validators and syntax checks: retained as reported in `PHASE_1_STATUS_REPORT.md`.
- Documentation package/file/reference verification: passed; all required consolidation records are present and the TOP GYM worktree is clean.
- No business feature, API, migration, seed, Web, Flutter, or production code was implemented.

## 7. Product decisions finalized

The three explicit product decisions identified by the previous consolidation review are approved and recorded:

1. `SC-015` — versioned Nutrition Calculation Engine with verified legacy formula evidence, explicit LogicFit rounding/validation policy, and immutable published snapshots.
2. `SC-016` — permission-based Training/Nutrition approval matrix with creator/approver separation and no implicit Platform Admin authority.
3. `SC-017` — Public QR Privacy Contract using opaque random tokens and a minimal public-safe allowlist.

No product-decision blocker remains in Phase 1. The approved decisions are implementation contracts for the later vertical slices; they are not feature implementation in this task.

## 8. Risks

- Treating `schema.sql` as a fresh-runtime contract would omit runtime coaching structures.
- Treating all 1,138 exercises as active would expose historical compatibility records; treating only 873 as the entire source would lose history.
- Changing identity fields later can create new seed keys; the Phase 2 seed contract must freeze semantic identity fields.
- The live snapshot is environment-specific and is not canonical LogicFit seed data.
- Later implementation risks remain: the Nutrition engine must match the approved vectors; authorization scope, token hashing/storage, revocation/rotation, and snapshot immutability require Phase 2 integration/security tests; the live TOP GYM snapshot remains environment-specific evidence.

## 9. Gate and recommendation

Phase 1 source consolidation and product-decision closure are **GREEN**. This report does not authorize implementation; the separately initiated Phase 2 contract package is maintained under `DOCS/PHASE_2/` and has its own gate/status report.
