# TOP GYM Nutrition Specification â€” Observed Behavior

**Audit date:** 2026-08-25  
**Status:** YELLOW â€” legacy nutrition behavior and test gaps remain; Phase 1 source-consolidation decisions are finalized and this audit spec is not a LogicFit module gate.  
**Source:** actual implementation, not an approved LogicFit nutrition contract.

## Food library

`data/library/foods.json` contains 367 records. Actual fields:

```text
nameAr, nameEn, category, calories, protein, carbs, fat,
fiber, sugar, sodium, servingSize, servingUnit
```

Observed serving units: gram 327 records and ml 40 records. Observed food categories include Protein, Vegetables, Grains, Fruits, Dairy, Mixed Dishes, Nuts, Fats, Legumes, Beverages, Supplements, Dips & Spreads, Sweeteners, Seeds, Dairy Alternatives, Sweets, and Salads.

`gym_foods` stores the corresponding values plus `id`, `source_id`, timestamps. No separate food-category table, food-unit table, serving-conversion table, or stable LogicFit seed key was found.

## Diet data model

`diet_plans`: member, name, description, start/end, meals per day, target calories/protein/carbs/fats, status, notes, version. Runtime additionally adds calorie goal/adjustment, calculator inputs, BMR, and TDEE.

`diet_meals`: diet plan, name, meal time, sort order, notes.

`diet_meal_items`: meal, food ID, sort order, assigned quantity, free-text serving unit, calculated calories/protein/carbs/fats, notes.

`meal_logs`: member, meal item, consumed quantity/time, calculated calories/protein/carbs/fats, notes.

Evidence: `database/schema.sql`, `src/services/coaching-service.js`.

## Exact manual creation flow

1. Open the trainee/coaching profile.
2. Open Nutrition Builder.
3. Load client, catalog, and latest measurement where available.
4. Enter plan context, dates, target calories/macros, goal, calculator values, and notes.
5. Create 3â€“6 meals in the UI.
6. Add foods from the catalog, quantity, serving unit, and notes.
7. Reorder meals/items and inspect live totals.
8. Review the draft.
9. Submit `POST /api/dietplans` or `PUT /api/dietplans/:id`.
10. Service validates food IDs and writes plan/meal/item rows in a transaction with nutrition snapshots.

The backend accepts 1â€“12 meals, while the final UI builder limits the visible builder to 3â€“6.

**BLOCKED: SPECIFICATION CONFLICT** â€” UI and service meal cardinality differ.

## Nutrition calculations

### Item snapshot

Observed formula:

```text
factor = assigned_quantity / food.serving_size
```

The service scales and rounds calories, protein, carbs, and fat. Fiber, sugar, and sodium are in the food library but are not carried into diet item snapshots, meal logs, totals, or print output.

The serving unit is a string; no conversion table or conversion rules were found.

### Manual calculator

The Web builder applies Mifflinâ€“St Jeor:

```text
male   = 10*w + 6.25*h - 5*age + 5
female = 10*w + 6.25*h - 5*age - 161
```

It uses activity factors 1.2, 1.375, 1.55, 1.725, and 1.9 and derives a 30% protein / 40% carbohydrate / 30% fat split in the observed UI code.

### Backend and intelligence

The backend stores client-provided calculator values; it does not establish one authoritative consistency check for submitted BMR/TDEE. Local intelligence uses a separate heuristic: measurement-based calculation where possible, fallback TDEE 2200, fat-loss adjustment âˆ’400, weight-gain adjustment +300, minimum calories 1200, and weight-based protein. This is not the same calculation path as the manual builder.

**BLOCKED: SPECIFICATION CONFLICT** â€” manual UI and intelligence/server behavior are not one centralized calculation authority.

## Lifecycle

Observed statuses: `draft`, `active`, `paused`, `completed`, `archived`.

Implemented: create, edit, delete, status update, meal log create/list, print/PDF. Not found as dedicated behavior/data: review, approval, publish, rejection, approver metadata, publication timestamp, immutable snapshot, duplicate, substitution domain, or daily macro-totals endpoint.

The UI action that looks like â€œapprove/saveâ€ performs the normal POST/PUT save path; it is not evidence of a separate approval transition.

## APIs and permissions

| Operation | Endpoint family | Permission |
|---|---|---|
| Diet list/detail/create/update/status/delete | `/api/dietplans` and `/api/diet-plans` | `coaching.read/create/update/delete` by method |
| Catalog | `/api/coaching/catalog` | `coaching.read` |
| Measurements | `/api/clients/:id/measurements` | coaching permissions by method |
| Meal logs | `/api/meal-logs` | `coaching.read/create` |
| Food library | `/api/library/foods` and generic `/api/library/:type` | library permissions |
| AI diet suggestion/refine | `/api/intelligence/diet-suggestions`, `/api/intelligence/refine` | intelligence permissions |

No separate nutrition review, approval, publish, duplicate, substitution, or daily-total permission was found. Roles are Owner and Assistant.

## Print/PDF

Nutrition print/PDF is Arabic/RTL/A4 and shows target calories/macros, calculated calories/macros, meal ordering, and food rows. It does not show fiber or serving conversions. PDF uses runtime-loaded `html2pdf` with html2canvas/jsPDF configuration; a local/offline guarantee is not established because the library is loaded from a CDN.

Evidence: `public/js/integrations/print-enhancements.js`, `public/css/print.css`.

## QA evidence

The nutrition QA checklist was inspected, including meal count, reorder, quantity, missing food, calculation, goal, and logging cases. No dedicated executed Nutrition API/E2E test set covers those cases. Shared unit tests passed, but Nutrition is not GREEN.

## Nutrition blockers

1. **BLOCKED: SPECIFICATION CONFLICT** â€” runtime diet calculator columns are absent from `database/schema.sql`.
2. **BLOCKED: SPECIFICATION CONFLICT** â€” UI allows 3â€“6 meals; backend accepts 1â€“12.
3. **BLOCKED: SPECIFICATION CONFLICT** â€” manual and intelligence calculations differ; no centralized authoritative calculator.
4. **BLOCKED: SPECIFICATION GAP** â€” no review/approval/publish/snapshot lifecycle.
5. **BLOCKED: SPECIFICATION GAP** â€” no categories/units/conversions domain.
6. **BLOCKED: SPECIFICATION GAP** â€” fiber is dropped after the food-library layer.
7. **BLOCKED: SPECIFICATION GAP** â€” no daily macro totals API.
8. **BLOCKED: SPECIFICATION GAP** â€” no substitution or duplicate operation.
9. **BLOCKED: SPECIFICATION GAP** â€” no dedicated Nutrition automated coverage was executed.
10. **BLOCKED: SPECIFICATION GAP** â€” permanent link to the measurement used for calculator inputs is not stored.

## Source Consolidation Resolution - 2026-08-25

- Runtime diet calculator columns are current TOP GYM runtime evidence; schema.sql remains legacy evidence. See TOP_GYM_LOGICFIT_DATABASE_DECISION.md.
- LogicFit meal cardinality is resolved as minimum 1, maximum 12, with a common UI/default workflow of 3-6. See LOGICFIT_NUTRITION_MEAL_RULE.md.
- The LogicFit backend centralized calculation boundary and exact v1 formula policy are approved in `LOGICFIT_NUTRITION_CALCULATION_ENGINE_DECISION.md` (SC-015). TOP GYM manual formulas are verified legacy evidence used by the approved v1 reference; the separate Intelligence heuristic remains legacy evidence and is not the LogicFit authority. Formula versioning, rounding, validation, test vectors, and immutable published snapshots are documented there.
- The full LogicFit lifecycle is documented in LOGICFIT_NUTRITION_LIFECYCLE_DECISION.md.
- Categories, units, daily totals, substitutions, and other absent domains are LogicFit scope to be specified in Phase 2; they are not invented from TOP GYM absence.

