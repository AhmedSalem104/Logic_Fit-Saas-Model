# LogicFit Enum and Vocabulary Mapping

**Date:** 2026-08-25  
**Status:** RESOLVED for source consolidation

## `expert` versus `advanced`

The audited evidence shows two different concepts, not one enum used consistently:

| Concept | TOP GYM evidence | LogicFit canonical vocabulary | Mapping rule |
|---|---|---|---|
| Exercise library difficulty | `data/library/exercises.json`: `beginner`, `intermediate`, `expert`; runtime active rows use the same lowercase values. Legacy compatibility rows include capitalized `Advanced`, `Beginner`, `Intermediate`. | `ExerciseDifficulty = beginner | intermediate | expert` | Preserve original source value. Map legacy exercise value `Advanced` to compatibility `expert` only within this exercise-difficulty scope; retain `legacy_value=Advanced`. |
| Workout plan/member level | `workout_programs.level` and builder/intelligence use `beginner`, `intermediate`, `advanced` (Arabic UI: مبتدئ، متوسط، متقدم). | `PlanLevel = beginner | intermediate | advanced` | Preserve as plan-level vocabulary. Do not replace `advanced` with `expert`. |

## Trace evidence

- `TOP GYM/src/services/library-service.js:174` exposes exercise `difficulty` from the database.
- `TOP GYM/src/services/coaching-service.js:754-758` excludes `legacy-compatibility` rows from the builder catalog and returns difficulty for active candidates.
- `TOP GYM/src/services/intelligence-service.js:422-437` normalizes plan level to beginner/intermediate/advanced and scores exercise difficulty.
- `TOP GYM/public/js/pages/library/library.js:471` and `member-portal-library.js:25` display both `advanced` and `expert` labels.
- `TOP GYM/public/js/pages/coaching/coaching.js:1163` presents the plan level choices مبتدئ/متوسط/متقدم.
- Read-only runtime query on 2026-08-25 found exercise difficulty values: active `beginner` 523, `intermediate` 293, `expert` 57; legacy `Advanced` 29, `Beginner` 166, `Intermediate` 70.

## Related status mapping

TOP GYM program statuses (`draft`, `active`, `paused`, `completed`, `archived`) are legacy operational statuses. They are not silently renamed to LogicFit lifecycle states. The canonical training/nutrition lifecycle is documented separately, and imported legacy values retain provenance.

## Decision

`expert` and `advanced` remain separate canonical concepts because their fields, consumers, and semantics differ. Any generator compatibility ranking must be explicit and tested; a generic string replacement is prohibited.

