# LogicFit Phase 1 Source Consolidation Decisions

**Date:** 2026-08-25  
**Scope:** Phase 1 conflict resolution only.  
**TOP GYM write policy:** no TOP GYM file, database, migration, seed, or configuration was modified.

## Authority used

1. LogicFit Master Bible V10 and its `DECISION_LOCK.md`.
2. Approved Master Bible `DOCS/` and domain documents.
3. Existing Phase 1 audit documents under this `DOCS/` directory.
4. Read-only TOP GYM runtime SQL evidence and source-code evidence.

The decisions below close the interpretation conflict. They do not authorize Phase 2 implementation.

## Decision register

| ID | Topic | Classification | Decision/status |
|---|---|---|---|
| SC-001 | Missing `60_TOP_GYM_SOURCE_AUDIT_PROMPT.md` | Documentation/package drift | The V10 package contains and the completed audit used `05_TOP_GYM_SOURCE_AUDIT/35_TOPGYM_SOURCE_AUDIT.md`. The absent `60_...` filename is recorded as a package drift; no audit restart is required. The requested audit deliverables are already present. **RESOLVED.** |
| SC-002 | `src/data/library` versus `data/library` | Legacy defect | `src/services/library-service.js` resolves `src/data/library`, while verified source files are under root `data/library`. LogicFit extracts only from the verified actual source and records the defect; TOP GYM is not repaired. **RESOLVED / LEGACY DEFECT.** |
| SC-003 | `schema.sql` versus runtime DDL | Runtime truth plus separate LogicFit mapping | Runtime SQL Server state describes the current TOP GYM instance. `database/schema.sql` remains legacy/documentation evidence. LogicFit must not copy either source blindly; its mapping is based on verified runtime behavior plus approved product requirements. **RESOLVED.** |
| SC-004 | Stale legacy Auth/count documentation | Documentation drift | Current auth/RBAC code and runtime behavior are the TOP GYM behavior evidence. Older technical prose is retained as historical evidence and is not reused as a LogicFit contract. **RESOLVED / DOCUMENTATION DRIFT.** |
| SC-005 | 873 versus 1,138 exercises | Runtime truth and historical preservation | The live DB has 1,138 rows: 873 `active` and 265 `legacy-compatibility`. The 265 are preserved as historical source records with stable LogicFit seed keys and status/provenance; they are not silently discarded or treated as default active library records. **RESOLVED.** |
| SC-006 | Missing stable seed keys | LogicFit data requirement | A deterministic, content-identity-based, order-independent seed-key algorithm is approved in `TOP_GYM_SEED_KEY_STRATEGY.md`. TOP GYM numeric primary keys remain provenance only. **RESOLVED.** |
| SC-007 | Training lifecycle gap | New LogicFit product feature | TOP GYM generic statuses are legacy behavior. LogicFit uses one lifecycle for Manual, Automatic, AI, and Hybrid plans: Draft → Review → Approval → Published → Snapshot/Version. **CLASSIFIED / LOGICFIT PRODUCT DECISION.** |
| SC-008 | Nutrition lifecycle gap | New LogicFit product feature | LogicFit uses the same canonical lifecycle for all nutrition creation modes. AI never bypasses canonical food IDs, validation, human approval, or publish rules. **CLASSIFIED / LOGICFIT PRODUCT DECISION.** |
| SC-009 | Portal PDF label versus print behavior | Legacy behavior plus LogicFit product decision | TOP GYM portal behavior is browser print. LogicFit will provide local professional Print and PDF through adapters, with Arabic/RTL support and no paid runtime dependency. **CLASSIFIED / LOGICFIT PRODUCT DECISION.** |
| SC-010 | Nutrition meal cardinality | LogicFit product rule | Domain minimum is 1 and maximum is 12 meals/day. The audited UI common workflow is 3–6 meals; users may add/remove meals within the domain bound. The prompt wording `36` is interpreted as the existing audited `3–6` notation, not literal 36. **RESOLVED.** |
| SC-011 | `expert` versus `advanced` | Separate concepts with compatibility mapping | Exercise-library difficulty and workout-plan level are different fields. LogicFit preserves `ExerciseDifficulty` (`beginner`, `intermediate`, `expert`) and `PlanLevel` (`beginner`, `intermediate`, `advanced`). A legacy `Advanced` exercise difficulty value is mapped to the exercise-difficulty compatibility value `expert` only with original value preserved. **RESOLVED.** |
| SC-012 | Missing Flutter, CRM, and Documents | New LogicFit scope | These are not TOP GYM defects. Flutter is a locked LogicFit client requirement; CRM and Documents are approved LogicFit scope in the feature map/checklist. **CLASSIFIED / LOGICFIT PRODUCT REQUIREMENT.** |
| SC-013 | Missing Control Plane and database-per-Gym | New LogicFit architecture | TOP GYM remains a single-gym legacy source. LogicFit uses SQL Server Control Plane DB + database per Gym + tenant isolation. **CLASSIFIED / LOGICFIT ARCHITECTURAL REQUIREMENT.** |
| SC-014 | SQL Server lock wording | Decision-lock documentation drift | LogicFit SQL Server is locked. The older generic deployment wording in V8 is superseded by the dated amendment recorded in `DECISION_LOCK.md`; TOP GYM's observed SQL Server remains evidence only. **RESOLVED.** |

## Final product decisions approved — 2026-08-25

The following decisions were explicitly approved by the LogicFit Lead after reviewing the existing Phase 1 evidence. They close the three product-decision items without changing TOP GYM.

### SC-015 — Nutrition Calculation Engine

`LOGICFIT_NUTRITION_CALCULATION_ENGINE_DECISION.md` is the authority for `logicfit-nutrition-calculation-engine-v1.0.0`. It records the verified TOP GYM manual formulas as legacy evidence, defines the LogicFit versioned backend engine, rounding and validation boundaries, test vectors, and immutable published calculation snapshots. The separate TOP GYM Intelligence heuristic is not silently adopted as the LogicFit authority.

### SC-016 — Permission-Based Approval

`LOGICFIT_APPROVAL_PERMISSION_MATRIX.md` is the authority for the Training and Nutrition approval permissions. The exact create/edit/submit-review/review/approve/publish permissions are server-side authority; role labels are configurable profiles only. Creator/approver separation is enforced where approval is required, and Platform Admin has no implicit Gym-plan approval or publish permission.

### SC-017 — Public QR Privacy

`LOGICFIT_PUBLIC_QR_PRIVACY_CONTRACT.md` is the authority if the QR surface is retained. The public identifier is an opaque, random, revocable and rotatable token; numeric Member IDs and sensitive member data are never public. The endpoint is allowlisted, tenant-isolated, rate-limited, no-store, and audited.

## Gate interpretation

The documented conflict register is closed by resolution or explicit classification, and the approved product-decision register is complete. **PHASE 1 GREEN.** This task does not start Phase 2; Phase 2 remains stopped until a separate Lead command.
