# Phase 2 Training Data / API / Screen Contract

**Sources:** Master Bible Training generator/screen documents, completed TOP GYM Training audit, `LOGICFIT_TRAINING_LIFECYCLE_DECISION.md`, `LOGICFIT_APPROVAL_PERMISSION_MATRIX.md`, `TOP_GYM_LOGICFIT_CANONICAL_MAPPING.md`.  
**Status:** canonical contract defined; implementation deferred.

## Canonical aggregate

All Manual, Automatic, AI, and Hybrid modes produce the same `TrainingPlan` shape:

```text
TrainingPlan
  id
  memberId
  name
  goal
  durationDays
  frequencyPerWeek
  startDate / endDate
  planLevel                 -- beginner | intermediate | advanced
  creationMode              -- manual | automatic | ai | hybrid
  source                    -- manual | system_generated | ai | hybrid
  generatorVersion? / modelProviderRef?
  status                    -- draft | review | approved | published | archived
  creatorUserId
  currentVersionNo
  notes?
  days[]
```

`memberId`, plan name, goal, duration, level, frequency, dates, and notes are from the approved Training generator flow. `generatorVersion`, source, actor, timestamps, and version context are provenance requirements. AI/provider values are adapter metadata and never canonical authority.

Each `TrainingDay` contains `dayOrder`, name/focus/notes, and ordered `TrainingExercise` rows. Each exercise row contains:

```text
exerciseId                -- canonical library ID, never AI/free-text ID
sortOrder
sets?
reps?
weight?
rest?
rir?
rpe?
tempo?
superset/group?
notes?
```

The fields are the union of verified TOP GYM runtime support and the approved Master Bible flow. Exact numeric ranges/requiredness beyond structural requirements are an implementation/domain-validation contract item and must not be invented in the UI.

## Vocabulary

- `PlanLevel`: `beginner | intermediate | advanced`.
- Exercise difficulty is separate: `beginner | intermediate | expert`.
- TOP GYM statuses are provenance, not automatic LogicFit publish state.
- Legacy imported `active` remains source status; import policy decides whether review is required.

## Lifecycle/state machine

```text
Draft → Review → Approval (persisted: approved) → Published → immutable Version/Snapshot
  ↑       │          │
  └───────┴── return changes
```

- Draft: creator can edit.
- Review: structural/domain validation has run; Reviewer can return changes or record review.
- Approved: Approver with `training.approve`; creator/approver separation applies.
- Published: Publisher with `training.publish` creates immutable version.
- Version: read-only historical content; a new revision is required for changes.
- No AI or generator path bypasses validation, review, approval, or publish.

## Mode contract

| Mode | Inputs/evidence | Server behavior |
|---|---|---|
| Manual | Member context, goal, duration, level, frequency, dates, notes, days/exercises | Validate and save canonical Draft. |
| Automatic | Member profile, goal/level, days/duration, equipment/preferences, existing plans, performance/adherence/readiness when configured, constraints/exclusions | Deterministic generator selects canonical exercises, validates, saves Draft; never auto-publishes. |
| AI | Structured context and constrained prompt/refinement | AI proposes structured content; server resolves IDs/validates; invalid output rejected; no publish. |
| Hybrid | Manual content plus deterministic/AI sections and overrides | Final output remains canonical; provenance records mode/source and every generated section where required. |

## API/screen/database mapping

| Capability | Screen/API | Tables |
|---|---|---|
| List/read | `TRN-W-001`, `GET /training/plans` | `training.training_plans` |
| Draft build/edit | `TRN-W-002`, POST/PATCH plan | plans/days/exercises + library refs |
| Generate | `TRN-W-002`, POST generate | plans/days/exercises + audit |
| Review/approval | `TRN-W-003`, submit/review/approve | plans/reviews/audit |
| Publish/version | `TRN-W-004`, publish/version GET | plans/versions |
| Workout log | `TRN-W-004`/Flutter session, sessions API | workout_sessions/set_logs |
| Print/PDF | `PRT-W-001`, print/pdf | published version + branding |

## Validation and security

- member belongs to selected Gym;
- all exercise IDs resolve to permitted active canonical/custom records;
- day/order uniqueness and required structural fields;
- lifecycle transition and rowversion are valid;
- exact permission and scope are present;
- self-approval is rejected;
- Published version cannot be mutated;
- AI cannot access unrelated member data or invent canonical IDs;
- audit generation, review, approval, publish, and denied high-risk actions.

## Final gap-resolution consistency — 2026-08-25

The Training contract is unchanged by the operational-domain decisions. Manual, Automatic, AI, and Hybrid creation all produce the same canonical aggregate and follow `Draft → Review → approved (Approval) → Published → immutable Version/Snapshot`. The exact permission keys and creator/approver separation remain authoritative. Auth/MFA and Gym scope are prerequisites; no Platform Admin receives implicit Gym plan approval or publish authority.
