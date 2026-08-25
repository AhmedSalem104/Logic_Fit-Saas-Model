# Phase 2 Nutrition Data / API / Screen Contract

**Sources:** Master Bible Nutrition generator/screen documents, completed TOP GYM Nutrition audit, `LOGICFIT_NUTRITION_CALCULATION_ENGINE_DECISION.md` (SC-015), `LOGICFIT_NUTRITION_MEAL_RULE.md` (SC-010), `LOGICFIT_NUTRITION_LIFECYCLE_DECISION.md`.  
**Status:** canonical contract defined; implementation deferred.

## Canonical aggregate

```text
NutritionPlan
  id
  memberId
  name
  goal
  periodStart / periodEnd
  creationMode              -- manual | automatic | ai | hybrid
  source
  status                    -- draft | review | approved | published | archived
  creatorUserId
  calculationEngineVersion
  target                    -- BMR/TDEE/calories/macros + inputs reference
  meals[1..12]
  currentVersionNo
  notes?
```

Each `NutritionMeal` has a stable `mealOrder` from 1 to 12, name, optional scheduled time, notes, and ordered `NutritionMealFood` rows. Each food row has canonical `foodId`, quantity, supported unit, factor, source food version, calculated nutrient snapshot, and optional fiber/sugar/sodium when the source record contains them.

## Calculation authority

The backend uses exactly `logicfit-nutrition-calculation-engine-v1.0.0` from SC-015:

- Mifflin–St Jeor BMR by sex;
- activity factors `1.2`, `1.375`, `1.55`, `1.725`, `1.9`;
- `TDEE = BMR × activity_factor`;
- `target = max(0, TDEE + signed_adjustment)` then half-up rounded;
- default macros protein 30%, carbohydrates 40%, fat 30%; grams divide by 4/4/9 and half-up round;
- food item factor `assigned_quantity / source_serving_size`, nutrient snapshots half-up rounded to 3 decimals;
- decimal-safe arithmetic and persisted engine/formula version.

The UI may preview but cannot be authoritative. TOP GYM Intelligence's separate heuristic is not adopted as a competing formula.

## Lifecycle

```text
Draft → Review → Approval (persisted: approved) → Published → immutable Version/Snapshot
  ↑       │          │
  └───────┴── return changes
```

All creation modes use the same model/lifecycle. AI may propose meals, quantities, substitutions, and notes only through canonical Food IDs and supported nutrition values. Calculation/validation failure blocks publish. Published plans preserve target outputs, food values, engine version, rounding policy, inputs, meal totals, and daily totals; future engine/library changes never recalculate them.

## Meal/cardinality contract

- Minimum: 1 meal/day.
- Maximum: 12 meals/day.
- Common UI/default workflow: 3–6 meals/day.
- Meal ordering is explicit and backend-validated.
- Duplicate food rows/substitutions are allowed only if the later domain contract explicitly defines the behavior; no silent duplicate/merge rule is invented here.

## API/screen/database mapping

| Capability | Screen/API | Tables |
|---|---|---|
| List/read | `NUT-W-001`, GET plans | nutrition plans/targets |
| Calculate | `NUT-W-002`, POST calculate | calculations/targets |
| Draft build/generate | `NUT-W-002`, POST/PATCH/generate | plans/meals/meal foods + library refs |
| Review/approval | `NUT-W-003`, lifecycle endpoints | reviews/plans/calculations |
| Publish/version | `NUT-W-004`, publish/version GET | versions/calculations/meals |
| Meal log | `NUT-W-004`/Flutter, logs endpoint | meal_logs/food_logs |
| Print/PDF | `PRT-W-001`, print/pdf | immutable nutrition version + branding |

## Validation/security

- member/context belongs to selected Gym;
- calculation inputs use approved units and required engine version;
- food IDs resolve to active canonical/custom records with source/version/nutrition basis;
- meal count is 1–12 and order is unique;
- quantities are positive and unsupported unit/density conversion fails safely;
- target/planned totals and tolerance are backend-calculated;
- lifecycle/permission/rowversion/self-approval checks are server-side;
- Published version is immutable and snapshots values used;
- AI cannot invent IDs/values or access unrelated member data;
- calculations, generation, review, approval, publish, and denials are audited.

## Final gap-resolution consistency — 2026-08-25

The Nutrition contract continues to use the approved independent Calculation Engine and its versioned BMR/TDEE/Calories/Macros/Rounding rules. Published plans preserve calculation and food/nutrition snapshots and never recalculate historical values. Meals remain 1–12, unsupported food-unit conversion fails validation, and Manual/Automatic/AI/Hybrid creation follows `Draft → Review → approved (Approval) → Published → immutable Version/Snapshot` with the exact permission matrix.
