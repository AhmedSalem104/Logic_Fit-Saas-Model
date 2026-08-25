# Exercise Seed Mapping

## Counts and status

- `data/library/exercises.json`: 873 active source records.
- `data/library/exercises-dataset.json`: 873 records and byte/semantic duplicate evidence; it is not imported a second time.
- `data/library/exercises-legacy.json`: 265 legacy-compatibility source records.
- Canonical output: 1,133 records — 873 active and 260 legacy-only.
- Five legacy records are exact semantic duplicates of active records and are represented as additional source references on the active row. Their source hashes, source paths, and ordinals remain in `provenance.source_records`.
- Source population remains fully represented: 873 + 265 = 1,138 source references.

Legacy compatibility records have `catalog_status=legacy-compatibility`, `active=false`, and `selectable=false`. Active records are the default generator/library candidates. No TOP GYM numeric primary key is used as a LogicFit key.

## Frozen identity

The exercise identity uses the deterministic semantic tuple:

```text
slug_or_name + normalized category + sorted equipment identities + resolved primary muscle seed key
```

The hash is SHA-256 over canonical normalized JSON; the first 12 hex characters are combined with a human slug. Secondary relationships, instructions, media, and nutrition-like attributes are not identity fields.

## Relationship mapping

`targetMuscleId` and secondary `muscleId` values resolve through the 297-muscle seed map. Equipment and category strings resolve through their normalized lookup datasets. Legacy `Advanced` is mapped to `ExerciseDifficulty=expert` only, with the original value retained in level provenance; `PlanLevel=advanced` remains separate.

## Slug collision rule

The source has 54 collision groups covering 108 distinct records. When the source slug is not unique, the canonical `record.slug` receives `-<first-8-sha256-identity-hex>`, while `source_slug` and the identity fields preserve the original source meaning. This makes the Phase 2 database slug constraint deterministic without using array order or numeric IDs.

## Media

Exercise media is metadata only. The record points to audited `/assets` references and stable LogicFit media keys when the source manifest supplies them. Binary assets are not copied by Phase 3.

