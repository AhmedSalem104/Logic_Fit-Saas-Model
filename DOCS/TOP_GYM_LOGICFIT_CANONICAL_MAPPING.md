# TOP GYM → LogicFit Canonical Mapping

**Date:** 2026-08-25  
**Status:** Source mapping approved; canonical seed package realized in Phase 3. No LogicFit business feature or production migration was created.

## Source authority and path correction

| Source | Verified result | LogicFit treatment |
|---|---|---|
| `TOP GYM/src/services/library-service.js` | Resolves `path.join(__dirname, '..', 'data', 'library')`, which becomes `TOP GYM/src/data/library`. | Record as a legacy source-path defect. Do not repair TOP GYM. |
| `TOP GYM/data/library` | Actual verified JSON library source. | Use as the extraction source for LogicFit canonical data. |
| `TOP GYM/database/schema.sql` | Checked-in legacy baseline. | Evidence only; not the live-state authority. |
| TOP GYM runtime SQL Server | Verified live tables, columns, values, and status flags. | Authority for current legacy runtime behavior. |

## Verified source files and records

| Canonical domain | Verified source file(s) | Observed records | LogicFit target JSON (Phase 2 implementation target) |
|---|---|---:|---|
| Exercises | `data/library/exercises.json` and `exercises-dataset.json` | 873 active source records | `database/seeds/v1/exercises.json` |
| Legacy exercises | `data/library/exercises-legacy.json`; runtime compatibility rows | 265 historical records | Same versioned exercise source envelope with `catalog_status=legacy-compatibility`; default selection disabled until approved contract |
| Muscles | `data/library/muscles.json` | 297 | `database/seeds/v1/muscles.json` |
| Muscle groups | Derived only from verified body-part/group labels; no independent TOP GYM group file was found | source-derived lookup set | `database/seeds/v1/muscle-groups.json` |
| Equipment | Verified exercise values | source-derived lookup set | `database/seeds/v1/equipment.json` |
| Exercise categories | Verified exercise values | source-derived lookup set | `database/seeds/v1/exercise-categories.json` |
| Levels | Verified exercise difficulty and workout plan level values | separate concepts; see enum decision | `database/seeds/v1/levels.json` with domain scope |
| Anatomy mappings | `public/data/anatomy-muscle-mapping.json` and muscle media manifest | mapping manifest; 132 system muscles mapped, 165 unmapped, 9 ambiguous | `database/seeds/v1/anatomy-mappings.json` |
| Foods | `data/library/foods.json` | 367 | `database/seeds/v1/foods.json` |
| Food categories | Verified `foods.json` category values | source-derived lookup set | `database/seeds/v1/food-categories.json` |
| Food units | Verified serving units: gram 327, ml 40 plus four approved contract units | 6 canonical units | `database/seeds/v1/units.json` |
| Nutrition values | Food calories/protein/carbs/fat/fiber/sugar/sodium and serving fields | 367 food records embedded in foods | `database/seeds/v1/foods.json` |
| Food conversions | No TOP GYM conversion source; six same-unit contract identities | 6 contract-only records; no DB destination | `database/seeds/v1/food-conversions.json` |

The Phase 3 package is implemented under `database/seeds/`; its manifest and checksums are documented in `DOCS/PHASE_3/02_SEED_MANIFEST.md`. Production migrations and business feature integration remain later phases.

## Exercise count and status mapping

Read-only SQL on 2026-08-25 produced:

| Runtime `metadata_json.catalogStatus` | Difficulty distribution | Count | LogicFit interpretation |
|---|---|---:|---|
| `active` | `beginner` 523, `intermediate` 293, `expert` 57 | **873** | Canonical active candidates |
| `legacy-compatibility` | `Advanced` 29, `Beginner` 166, `Intermediate` 70 | **265** | Historical compatibility records; preserve provenance and original status, do not silently discard |
| **Total** | — | **1,138** | Complete live exercise source population |

The source JSON active catalog also validates to 873 records. The runtime status flag, not a narrative count in an older document, is the definitive current classification.

## Mapping rules

1. Every imported record receives a LogicFit deterministic `seed_key`; TOP GYM `id`/`source_id` is never used as the LogicFit primary key or seed key.
2. The active/legacy classification is retained as source provenance and a canonical status mapping. Legacy rows remain available for historical traceability and compatibility analysis.
3. Relationships are resolved by LogicFit seed keys, not by array positions or TOP GYM numeric IDs.
4. Missing canonical datasets (equipment, categories, units, and groups) are derived only from verified source values during the seed phase and must pass duplicate/FK validation.
5. Organization-owned edits must not overwrite canonical seed rows.
6. Seed execution must be versioned, deterministic, idempotent, FK-safe, and duplicate-safe.

## Phase 3 realization

The final package preserves 1,138 exercise source references as 1,133 canonical rows: 873 active, 260 legacy-only, and five legacy references merged into active rows with source hashes. Stable keys are independent of TOP GYM numeric IDs and JSON order.

## Classification

The seed-path conflict and exercise-count conflict are **RESOLVED**. The source path is a **LEGACY DEFECT**. The final SQL table names/columns belong to the LogicFit database contract and are intentionally not invented in Phase 1.
