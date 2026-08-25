# LogicFit Nutrition Calculation Engine Decision

**Decision ID:** SC-015  
**Status:** APPROVED  
**Approved:** 2026-08-25  
**Scope:** LogicFit Nutrition calculation authority; this decision does not modify TOP GYM.

## Decision

LogicFit will implement a versioned, backend-authoritative Nutrition Calculation Engine. The engine is independent from Web and Flutter UI. Clients may preview inputs and results, but the backend validates inputs, executes the selected engine version, persists the result, and owns the published snapshot.

The first approved engine is `logicfit-nutrition-calculation-engine-v1.0.0`.

## Legacy evidence and boundary

The verified TOP GYM manual Web calculator is reliable legacy evidence for the following formulas:

- Male BMR: `10 * weight_kg + 6.25 * height_cm - 5 * age_years + 5`.
- Female BMR: `10 * weight_kg + 6.25 * height_cm - 5 * age_years - 161`.
- Activity factors: sedentary `1.2`, light `1.375`, moderate `1.55`, high `1.725`, very high `1.9`.
- TDEE: `BMR * activity_factor`.
- Manual calorie adjustment is signed and added to TDEE.
- The observed manual macro split is protein `30%`, carbohydrate `40%`, fat `30%`.

Evidence is recorded in `TOP_GYM_NUTRITION_SPEC.md`, with source locations in `public/js/pages/coaching/coaching.js:1046-1067`. TOP GYM Intelligence has a separate heuristic path (including a fallback TDEE and goal adjustments); it is legacy evidence only and is not silently adopted as the LogicFit authoritative formula.

## v1.0.0 canonical formulas

All arithmetic uses decimal-safe calculations and retains full precision until the specified output boundary.

1. `bmr_raw` uses the sex-specific Mifflin–St Jeor formula above.
2. `tdee_raw = bmr_raw * activity_factor`.
3. `target_raw = max(0, tdee_raw + signed_calorie_adjustment_kcal)`.
4. `target_calories_kcal = round_half_up(target_raw, 0)`.
5. Default macro energy split is protein `0.30`, carbohydrate `0.40`, fat `0.30`.
6. `protein_g = round_half_up(target_calories_kcal * 0.30 / 4, 0)`.
7. `carbohydrate_g = round_half_up(target_calories_kcal * 0.40 / 4, 0)`.
8. `fat_g = round_half_up(target_calories_kcal * 0.30 / 9, 0)`.

`target_calories_kcal` is authoritative. Independently rounded macro grams may produce a small energy difference; the v1 validation tolerance is ±10 kcal when macro energy is reconstructed. No unsupported clinical minimum, maximum, unit conversion, or density conversion is inferred from TOP GYM.

Food item scaling follows the verified TOP GYM snapshot behavior:

`factor = assigned_quantity / source_serving_size`

Each supported source nutrient is scaled by `factor` and rounded half-up to 3 decimal places. v1 supports calories, protein, carbohydrate, and fat; fiber, sugar, and sodium may be carried when present in the canonical food record. A unit mismatch without an approved conversion is a validation error, not an implicit conversion.

## Persistence and historical behavior

Every calculated plan records:

- engine identifier and exact formula version;
- normalized calculation inputs and units;
- unrounded audit values where required and persisted rounded outputs;
- BMR, TDEE, calorie target, macro targets, and rounding policy;
- food source version, serving basis, assigned quantities, item snapshots, meal totals, and daily totals.

When a plan is Published, these values become an immutable calculation Snapshot/Version. A later engine version creates a new revision or new plan; it never recalculates or mutates a previously published historical plan.

## Approved test vectors

The following vectors are part of the decision contract and must be implemented as unit/API regression tests in the later Nutrition vertical slice:

| Case | Inputs | Expected result |
|---|---|---|
| A | Male, 80 kg, 180 cm, age 30, moderate, adjustment 0 | BMR 1780.00; TDEE 2759.00; target 2759 kcal; protein 207 g; carbs 276 g; fat 92 g |
| B | Female, 60 kg, 165 cm, age 28, light, adjustment -300 | BMR 1330.25; TDEE 1829.09; target 1529 kcal; protein 115 g; carbs 153 g; fat 51 g |
| C | Per 100 g: 123.456 kcal, 7.891 g protein, 12.345 g carbs, 4.444 g fat; assigned 133 g | Factor 1.33; 164.196 kcal; 10.495 g protein; 16.419 g carbs; 5.911 g fat |
| D | Publish with v1.0.0, then introduce a later engine version | Published v1.0.0 snapshot remains byte-for-byte domain-equivalent and is not recalculated |

## Authority and traceability

The backend engine, not a client formula, is the authority. The future trace is:

`React/Flutter input → REST API → Nutrition Use Case → Versioned Calculation Engine → validated snapshot → SQL Server`

Any change to formula, rounding, supported units, or snapshot shape requires a new decision/version, tests, documentation, and migration/release review.
