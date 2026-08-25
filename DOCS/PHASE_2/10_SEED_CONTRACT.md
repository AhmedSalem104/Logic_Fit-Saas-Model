# Phase 2 Canonical Seed Contract

**Status:** Approved contract; realized by the Phase 3 package under `database/seeds/`.  
**Phase 3 input:** this document plus `TOP_GYM_SEED_KEY_STRATEGY.md`.  
**Source boundary:** verified TOP GYM actual data paths under `data/library/` and runtime evidence; do not copy the defective `src/data/library` reference.

## Seed manifest contract

Phase 3 will create:

```text
database/seeds/manifest.json
database/seeds/v1/
  muscle-groups.json
  muscles.json
  equipment.json
  exercise-categories.json
  levels.json
  exercises.json
  anatomy-mappings.json
  food-categories.json
  units.json
  foods.json
```

Phase 3 also creates `v1/food-conversions.json` as a contract-only validation artifact. It has no destination table because the approved Phase 2 model has no `library.food_conversions` table; the SQL Server runner does not install it as an operational table.

The manifest records `dataset`, `seed_version`, source path/reference, record count, checksum, dependency order, destination table, schema version, and validator version. This phase creates none of these files.

## Canonical file representation decision

The Phase 1 mapping used the descriptive names `food-units.json` and `nutrition-values.json` as possible implementation targets. The higher-authority Master Bible seed layout and Food Seed Specification use `units.json` and place nutrition values in the Food record. Therefore the Phase 2 canonical representation is:

- file/dataset key: `units.json` → `library.food_units`;
- file/dataset key: `foods.json` → `library.foods`, including the approved nutrition value fields;
- no separate `nutrition-values.json` file or `library.nutrition_values` table is created by this contract.

This is a documented source/documentation normalization, not silent source-data loss. If normalization or shared nutrition values later requires a separate table, it requires a new approved contract and snapshot/migration impact review.

## Stable key algorithm

Every record has a deterministic human-debuggable base key generated from normalized semantic identity fields, independent of JSON order and TOP GYM numeric IDs. A collision disambiguator is derived from a canonical normalized content hash, not a random ID. Original source IDs are retained only as provenance.

The semantic identity field set is frozen per dataset before Phase 3 extraction. If a collision cannot be resolved without a new identity rule, Phase 3 must stop with a documented contract change; it must not use array position or random keys.

## Dataset contract

| Dataset | Source evidence | Destination | Dependencies | Version/validation |
|---|---|---|---|---|
| `muscle-groups` | TOP GYM muscle/group data and approved audit mapping | `library.muscle_groups` | none | `seed_key`, Arabic/English identity, unique key, active/status. |
| `muscles` | TOP GYM actual muscle records/assets | `library.muscles` | muscle-groups | FK resolution, unique keys, source/provenance, no order identity. |
| `equipment` | TOP GYM exercise equipment values/library | `library.equipment` | none | normalized name identity, unique keys, active. |
| `exercise-categories` | TOP GYM categories/library | `library.exercise_categories` | none | normalized names, unique keys, active. |
| `levels` | TOP GYM exercise difficulty + plan-level vocabulary | `library.levels` | none | Separate `ExerciseDifficulty` (`beginner/intermediate/expert`) and `PlanLevel` (`beginner/intermediate/advanced`); compatibility provenance. |
| `exercises` | `data/library/exercises.json` + `exercises-legacy.json` and runtime status | `library.exercises` | muscles, equipment, categories, levels, anatomy/media refs | 1,138 source records preserved as 873 active candidates + 265 legacy-compatibility with status/provenance; no numeric PK reuse. |
| `anatomy-mappings` | TOP GYM anatomy/muscle assets and mapping manifest | `library.anatomy_mappings` | muscles | asset key/path validation; missing media explicit unavailable. |
| `food-categories` | TOP GYM food library/category evidence | `library.food_categories` | none | unique normalized identity, source/version. |
| `units` | Master Bible approved units: gram, kilogram, milliliter, liter, piece, serving | `units.json` → `library.food_units` | none | dimension/base quantity required; no unapproved density conversion. |
| `foods` | TOP GYM `data/library` food records and nutrition values | `foods.json` → `library.foods` | food-categories, units | source/version, nonnegative nutrition, serving/calculation basis, unique identity; historical plans snapshot values. |

## Derived-dataset identity fields

The seed key strategy's semantic identity fields are extended for the required lookup datasets as follows:

| Dataset | Semantic identity before hashing | If evidence is insufficient |
|---|---|---|
| Food category | Normalized canonical English label + domain namespace; Arabic label is a disambiguator. | Do not merge records without source-backed identity evidence; preserve separate canonical keys. |
| Anatomy mapping | Resolved muscle `seed_key` + normalized body region + view + approved asset key/reference. | Keep mapping unresolved/unpublished; do not use array position. |
| Equipment/category | Normalized canonical label + domain namespace. | Preserve separate records until a source-backed merge rule exists. |

## Ownership and idempotency

- Seed rows are marked canonical/system-owned and include `seed_version`/source metadata.
- A normal seed run upserts only matching canonical records by stable key and approved version. It never overwrites Gym-owned custom records.
- Material changes create a new seed version or an explicit migration. Deactivation preserves historical references.
- Seed execution is FK-safe in dependency order and uses a transaction per dataset or approved recoverable unit.
- Duplicate seed keys, unresolved FKs, missing required source/version, invalid nutrition, broken media refs, and changed canonical identity fail validation.

## Consumer contract

Training generators may select only active resolvable exercise IDs. Nutrition generators may select only active resolvable food IDs and supported units. Member Portal and library screens consume the same canonical rows. Published plans retain source versions and nutrient/prescription snapshots.

## Final gap-resolution seed note — 2026-08-25

The approved Finance, Store, Classes, CRM, Auth, and Operations decisions do not create seed files in Phase 2. Canonical library seed destinations and stable-key rules remain unchanged. Phase 3 may seed CRM default stages and configurable payment-method concepts only through the approved versioned/idempotent runner; tenant-owned settings and transactions are never overwritten. Food conversions remain explicit-source-only per `decisions/21_FOOD_UNITS_CONTRACT_DECISION.md`.
