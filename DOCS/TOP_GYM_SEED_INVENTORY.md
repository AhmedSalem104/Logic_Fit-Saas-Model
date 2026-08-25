# TOP GYM Seed Inventory

**Audit date:** 2026-08-25  
**Scope:** identify actual source datasets and seed behavior. Phase 1 was audit-only; the separately authorized Phase 3 realization is recorded below.

## Dataset inventory

| Dataset | File | Observed records | Key observations |
|---|---|---:|---|
| Exercises | `data/library/exercises.json` | 873 | `sourceId`, `upstreamId`, slug, bilingual content, target/secondary muscles, equipment/category/difficulty, image references. |
| Exercise duplicate dataset | `data/library/exercises-dataset.json` | 873 | Same canonical-sized dataset; must be deduplicated/verified before LogicFit import. |
| Legacy exercise compatibility | `data/library/exercises-legacy.json` | 265 | Legacy shape/compatibility dataset. |
| Muscles | `data/library/muscles.json` | 297 | Bilingual labels/body parts and anatomy metadata. |
| Foods | `data/library/foods.json` | 367 | Bilingual names, category, serving size/unit, calories/protein/carbs/fat/fiber/sugar/sodium. |
| Exercise media manifest | `public/data/exercise-assets.json` | 873 records; 265 project links | Start/end image mapping and upstream revision metadata. |
| Muscle media manifest | `public/data/muscle-assets.json` | 297 | 214 mapped, 83 manual review, 188 canonical structures, 564 downloaded images. |
| Anatomy mapping | `public/data/anatomy-muscle-mapping.json` | mapping manifest | 132 system muscles mapped, 165 unmapped, 9 ambiguous elements. |

## Observed distributions

- Exercise categories: strength 581, stretching 123, plyometrics 61, powerlifting 38, olympic weightlifting 35, strongman 21, cardio 14.
- Exercise difficulty: beginner 523, intermediate 293, expert 57.
- Food serving units: gram 327, ml 40.
- Food categories: Protein 73, Vegetables 59, Grains 44, Fruits 42, Dairy 34, with the remaining categories recorded in the source JSON.

These values are extracted dataset counts, not invented seed data.

## TOP GYM seed/runtime behavior

`src/services/library-service.js` reads the JSON files and seeds only when all three library tables are empty. Observed behavior:

1. Muscles and foods receive `source_id` derived from array index.
2. Exercises use `sourceId` with an index fallback.
3. Target muscle is resolved through source ID.
4. Full source item metadata is stored in exercise metadata JSON.
5. Partial-library state does not seed each missing table independently.
6. Sync updates/inserts current rows and marks absent exercises as legacy compatibility instead of deleting them.
7. Organization/custom ownership is not modeled as a separate canonical boundary in the CRUD paths.

## Required LogicFit seed fields not present in TOP GYM source

The inspected raw JSON records do not contain a formal LogicFit `seed_key`, destination table, source URI/ID envelope, relationship manifest, or seed version per record. Those must be derived only after an approved Phase 2 contract; they must not be invented during this audit.

Audit-time status superseded by TOP_GYM_SEED_KEY_STRATEGY.md: the LogicFit deterministic seed-key strategy is approved for source consolidation; implementation remains Phase 2.
## Seed integrity observations

Read-only validators passed:

- Exercise catalog: 873 active, unique source/upstream IDs and slugs, 873 image pairs.
- Exercise content: no critical issues; 873 exercises and 297 resolvable muscles.
- Muscle assets: 297 records, 214 mapped, 83 manual review.

Phase 1 did not create a LogicFit seed runner or seed data. Phase 3 now provides the canonical package under `database/seeds/v1/`, the manifest under `database/seeds/manifest.json`, and the SQL Server runner under `tools/seed/`.

## Source Consolidation Resolution - 2026-08-25

The audit-time stable-key gap is resolved by TOP_GYM_SEED_KEY_STRATEGY.md. The Phase 3 implementation uses that deterministic strategy and remains independent of TOP GYM numeric IDs and JSON ordering.

## Phase 3 realization — 2026-08-25

The package contains 1,133 canonical exercise rows representing 1,138 source references, 297 muscles, 367 foods, 194 verified anatomy mappings plus 165 explicit unsupported records, six units, and embedded nutrition values. See `DOCS/PHASE_3/PHASE_3_STATUS_REPORT.md`.


