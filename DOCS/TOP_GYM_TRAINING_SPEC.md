# TOP GYM Training Specification â€” Observed Behavior

**Audit date:** 2026-08-25  
**Status:** YELLOW â€” legacy training behavior and test gaps remain; Phase 1 source-consolidation decisions are finalized and this audit spec is not a LogicFit module gate.  
**Source:** actual TOP GYM implementation; this is not an approved LogicFit contract.

## Exercise library

- 873 canonical-sized exercise records in `data/library/exercises.json`.
- 297 muscle records.
- 265 legacy-compatible exercise records/mappings.
- Bilingual names, descriptions, instructions, tips, and common mistakes are present for the 873 current exercises.
- Every current exercise has start/end image references validated by the local catalog validator.
- `movementPattern`, `repsRange`, `setsRange`, `restSeconds`, `tempo`, and `videoUrl` are absent/empty in the current dataset according to the content audit; builder values are separate plan-entry values.
- No independent Muscle Group, Equipment, Category, or Level tables were found. Body parts and text enums are used.

Evidence: `data/library/exercises.json`, `data/library/muscles.json`, `src/services/library-service.js`, `public/data/exercise-assets.json`.

## Program structure

```text
workout_programs
  â””â”€ workout_routines
       â””â”€ workout_exercises
            â”œâ”€ workout_sessions
            â”‚    â””â”€ workout_set_logs
            â””â”€ library exercise reference
```

Observed program fields: member, name, description, start/end, duration weeks, goal, level, days per week, status, notes, version.

Observed exercise-entry fields: exercise ID, sort order, sets, reps min/max, weight kg, rest seconds, tempo, superset group ID, notes. Runtime also supports RIR 0â€“10 and RPE 1â€“10.

## Exact creation flow

1. Open the coaching/training builder.
2. Load clients and builder catalog.
3. Step 1: member and program context.
4. Step 2: routines/days and canonical exercise IDs.
5. Step 3: review.
6. Validate member/name/start date, dates, duration/days, routine structure, exercise ID, sets/reps/rest/RIR/RPE ranges.
7. `POST /api/workoutprograms` or `PUT /api/workoutprograms/:id`.
8. Create/update parent and child structure in a transaction.
9. Optionally change status, print/PDF, start a session, and record sets.

UI defaults observed: four weeks, three days, three sets, reps 10â€“12, 90 seconds rest, and status `active`. These are UI defaults, not canonical exercise-library facts.

Evidence: `public/js/pages/coaching/coaching.js`, `src/services/coaching-service.js`.

## Validation and execution behavior

- Workout start date is required; end date must not precede start.
- Duration is bounded by the service; automatic end is derived when omitted.
- Days are 1â€“7; routine name is required.
- Exercise IDs are validated against the library; AI output uses catalog IDs.
- Sets/reps/rest/weight/RIR/RPE have runtime ranges.
- Update uses version conflict checks and rebuilds child rows.
- Session start rejects an already active session; set numbers are unique per exercise/session; completed/cancelled ending is supported.
- Progress summary calculates session/set/repetition counts and volume from weight Ã— reps.

## AI and automatic behavior

TOP GYM intelligence is deterministic local logic, not a paid external AI dependency. It ranks/selects existing catalog exercises, normalizes goals/levels, returns a draft with `requiresReview`, and supports refine operations. It does not invent canonical exercise IDs.

The backend nevertheless accepts normal plan status values in create/update payloads. There is no explicit server-side approval/publish gate preventing a caller from submitting an active status.

## Lifecycle status

Observed values: `draft`, `active`, `paused`, `completed`, `archived`.

Not observed as dedicated state/data: review, approved, published, rejected, approver, approval timestamp, publish timestamp, workflow history, or immutable published snapshot.

Edit and delete exist. Duplicate has no route, service function, or confirmed UI action.

**BLOCKED: SPECIFICATION GAP** â€” LogicFit requires human approval and published snapshots; TOP GYM evidence does not implement that lifecycle.

## API and permissions

- Read: `coaching.read`.
- Create: `coaching.create`.
- Update/status: `coaching.update`.
- Delete: `coaching.delete`.
- AI generation/refinement: `intelligence.generate`.
- Library CRUD: `library.read/create/update/delete`.
- Roles observed: Owner and Assistant; no Trainer/Coach role was found.

Routes are listed in `TOP_GYM_API_MAPPING.md` and permission details in `TOP_GYM_PERMISSION_MAPPING.md`.

## Print/PDF

Training print creates Arabic RTL A4 documents with routine/exercise information, instructions and images where available. Saved systems fetch library detail; draft print uses current catalog projection. PDF uses runtime-loaded `html2pdf`/html2canvas/jsPDF.

Evidence: `public/js/integrations/print-enhancements.js`, `public/css/print.css`.

## Training blockers

1. **BLOCKED: SPECIFICATION CONFLICT** â€” `workout_exercises.rir/rpe` are runtime DDL but absent from `database/schema.sql`.
2. **BLOCKED: SPECIFICATION CONFLICT** â€” secondary-muscle ID namespaces differ between list projection and write validation.
3. **BLOCKED: SPECIFICATION CONFLICT** â€” dataset difficulty uses `expert`; UI/AI normalization uses `advanced`.
4. **BLOCKED: SPECIFICATION GAP** â€” no review/approval/publish/snapshot lifecycle.
5. **BLOCKED: SPECIFICATION GAP** â€” no duplicate operation despite QA checklist mention.
6. **BLOCKED: SPECIFICATION GAP** â€” builder catalog projection does not expose all fields some UI references expect.
7. **BLOCKED: SPECIFICATION GAP** â€” native mobile implementation absent.
8. **BLOCKED: SPECIFICATION GAP** â€” offline print/PDF and local font policy unverified.

## QA evidence

The training QA matrix was inspected. The executed checks were shared unit tests and syntax checks; no Training CRUD API/E2E, migration/seed, approval, snapshot, or offline print test was run. Training is not GREEN.

## Source Consolidation Resolution - 2026-08-25

The audit-time blockers are reclassified as follows:

- Runtime RIR/RPE and other runtime-only DDL are covered by TOP_GYM_LOGICFIT_DATABASE_DECISION.md; runtime is current TOP GYM truth and no TOP GYM repair is authorized.
- expert versus advanced is resolved as two separate concepts in LOGICFIT_ENUM_MAPPING.md.
- Review/approval/publish/snapshot is a LogicFit product lifecycle, documented in LOGICFIT_TRAINING_LIFECYCLE_DECISION.md; TOP GYM remains unchanged.
- Native Flutter and local Print/PDF are LogicFit scope/requirements, not TOP GYM parity defects.
- The final LogicFit Approval role matrix is approved in `LOGICFIT_APPROVAL_PERMISSION_MATRIX.md` (SC-016). Exact permissions, state transitions, creator/approver separation, tenant scope, and backend enforcement are documented there; TOP GYM role labels are not copied as LogicFit authority.

