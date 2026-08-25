# TOP GYM Source Conflicts â€” Lead Review Register

**Audit date:** 2026-08-25  
**Purpose:** supplemental register for conflicts found after the initial audit documents were created.

| ID | Exact conflict/gap | Evidence | Required action |
|---|---|---|---|
| C-001 | Requested `60_TOP_GYM_SOURCE_AUDIT_PROMPT.md` absent; only `35_TOPGYM_SOURCE_AUDIT.md` exists. | Master Bible package tree | Approve alias/version relationship or provide the missing contract. |
| C-002 | `library-service.js` resolves `src/data/library`, but actual data is root `data/library`. | `src/services/library-service.js:5`; static path proof | Decide/correct seed authority in a controlled change; do not repair during audit. |
| C-003 | `schema.sql` lacks runtime coaching columns/tables. | `database/schema.sql`; `src/services/coaching-service.js:180-368` | Approve one SQL Server migration authority. |
| C-004 | Legacy docs contain stale Auth/library-count descriptions. | `docs/TOP-GYM-TECHNICAL-SPECIFICATION.md`; current auth/library code | Version and reconcile documentation before requirements reuse. |
| C-005 | No Control Plane/database-per-gym/tenant predicates in TOP GYM. | `src/`, `database/` scan | Treat as architectural gap, not a missing field to guess. |
| C-006 | Manual and intelligence nutrition calculations differ. | `public/js/pages/coaching/coaching.js`; `src/services/intelligence-service.js` | Approve backend calculation authority and tests. |
| C-007 | Training/nutrition have generic status only, no approval/publish/snapshot. | coaching service/schema/routes | Approve lifecycle before feature contracts. |
| C-008 | Portal PDF label calls `window.print()`. | `public/js/member-portal.js` | Decide whether LogicFit treats this as print or dedicated PDF. |

The table above records the audit-time state. It is superseded for the current Lead decision by the source-consolidation resolution below. No resolution changed TOP GYM.

## Source Consolidation Resolution â€” 2026-08-25

| ID | Final classification | Resolution / decision record |
|---|---|---|
| C-001 | Documentation/package drift â€” **RESOLVED** | The completed audit used the available V10 `35_TOPGYM_SOURCE_AUDIT.md`; the missing `60_...` filename is recorded as package drift. See `SOURCE_CONSOLIDATION_DECISIONS.md` SC-001. |
| C-002 | Legacy defect â€” **RESOLVED** | Actual source is root `data/library`; the incorrect `src/data/library` reference is not repaired in TOP GYM. See canonical mapping and seed-key strategy. |
| C-003 | Runtime truth â€” **RESOLVED** | Runtime SQL Server describes current TOP GYM state; `schema.sql` remains evidence; LogicFit uses a separate canonical mapping. See `TOP_GYM_LOGICFIT_DATABASE_DECISION.md`. |
| C-004 | Documentation drift â€” **RESOLVED** | Current auth/RBAC/library code and runtime outrank stale legacy prose for TOP GYM behavior. |
| C-005 | LogicFit architectural requirement â€” **CLASSIFIED** | Control Plane DB + database per Gym + tenant isolation belong to LogicFit and are not added to TOP GYM. |
| C-006 | LogicFit product decision â€” **RESOLVED** | `SC-015` approves the versioned LogicFit Nutrition Calculation Engine, including verified legacy formula evidence, explicit rounding/validation policy, test vectors, and immutable published snapshots. |
| C-007 | LogicFit product feature â€” **CLASSIFIED** | Training and nutrition use the canonical lifecycle decision records. |
| C-008 | Legacy behavior plus LogicFit product decision â€” **CLASSIFIED** | TOP GYM portal PDF is browser print; LogicFit supplies local professional Print + PDF. |

## Additional conflict resolutions

- Exercise count `873 + 265 = 1,138` is resolved by runtime `catalogStatus` evidence; see `TOP_GYM_LOGICFIT_CANONICAL_MAPPING.md`.
- Stable keys are resolved by `TOP_GYM_SEED_KEY_STRATEGY.md`.
- Meal cardinality is resolved by `LOGICFIT_NUTRITION_MEAL_RULE.md`.
- `expert` and `advanced` are separate concepts; see `LOGICFIT_ENUM_MAPPING.md`.
- Flutter, CRM, Documents, and Control Plane are LogicFit scope, not TOP GYM defects.

The conflict register is closed by resolution or explicit classification. The overall Phase 1 source-consolidation gate is **GREEN**. Product decisions SC-015, SC-016, and SC-017 are recorded in their dedicated decision documents; Phase 2 is not started in this task.

## Expanded Conflict Register - 2026-08-25

| ID | Sources/evidence | Final classification | Decision record |
|---|---|---|---|
| C-009 | Live dbo.gym_exercises status query; exercises.json; exercises-legacy.json | Runtime truth — RESOLVED | 1,138 total = 873 active + 265 legacy-compatibility; preserve both with provenance. |
| C-010 | TOP GYM library records lack a complete stable seed-key envelope; Master Bible seed requirements | LogicFit data requirement — RESOLVED | Deterministic key algorithm in TOP_GYM_SEED_KEY_STRATEGY.md. |
| C-011 | Nutrition UI min=3/max=6/value=4; backend normalizer accepts 1-12 | LogicFit product rule — RESOLVED | Minimum 1, maximum 12, common UI/default 3-6. |
| C-012 | Exercise difficulty data/runtime; coaching builder; intelligence normalizeLevel | Separate concepts — RESOLVED | ExerciseDifficulty preserves expert; PlanLevel preserves advanced. |
| C-013 | TOP GYM lacks Flutter, CRM, and Documents; Master Bible feature map/checklist includes them | LogicFit product scope — CLASSIFIED | New LogicFit features, not TOP GYM defects. |
| C-014 | TOP GYM lacks Control Plane/database-per-Gym/tenant predicates; Master Bible locks them | LogicFit architecture — CLASSIFIED | Do not add to TOP GYM; implement only under LogicFit architecture later. |
| C-015 | Master Bible V8 generic DB wording versus current LogicFit SQL Server lock | Documentation/decision-lock drift — RESOLVED | Dated Decision Lock amendment makes Microsoft SQL Server authoritative for LogicFit. |

The expanded register is closed by resolution or explicit LogicFit classification. SC-015, SC-016, and SC-017 close the previously pending product-decision items without changing TOP GYM.

## Final Product Decision Closure — 2026-08-25

- `SC-015`: Nutrition Calculation Engine approved in `LOGICFIT_NUTRITION_CALCULATION_ENGINE_DECISION.md`.
- `SC-016`: Permission-Based Approval Matrix approved in `LOGICFIT_APPROVAL_PERMISSION_MATRIX.md`.
- `SC-017`: Public QR Privacy Contract approved in `LOGICFIT_PUBLIC_QR_PRIVACY_CONTRACT.md`.

**PHASE 1 GREEN.** Every documented conflict is resolved or explicitly classified. No TOP GYM file or database was modified.

