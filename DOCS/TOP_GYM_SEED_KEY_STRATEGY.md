# LogicFit Canonical Seed-Key Strategy

**Date:** 2026-08-25  
**Status:** Approved for Phase 2 seed-contract design  
**Purpose:** stable identity independent of TOP GYM numeric IDs and JSON ordering.

## Required properties

Every canonical record must have a key that is deterministic, stable for the chosen semantic identity, unique, human-debuggable, independent of JSON ordering, and independent of TOP GYM numeric primary keys.

## Algorithm

For each domain:

1. Select the domain's documented semantic identity fields. Do not use descriptions, instructions, image URLs, nutrient values, timestamps, array position, or TOP GYM numeric IDs as identity.
2. Normalize each identity value with Unicode NFKC, trim whitespace, lowercase using a locale-independent rule, normalize separators, and preserve meaningful Unicode letters/numbers. Empty values are represented by a fixed sentinel.
3. Normalize unordered relationship arrays by sorting their normalized member values. Ordered arrays retain their documented semantic order.
4. Serialize the normalized identity object using a fixed field order and UTF-8 JSON.
5. Compute `sha256(identity_json)`.
6. Build a human-readable slug from the primary display identity.
7. Emit:

```text
<domain>.<human-readable-slug>.<first-12-lowercase-hex-digest>
```

The digest is deterministic collision resolution, not a random ID. The readable prefix makes support/debugging practical; the digest guarantees uniqueness when two records share a display name.

## Domain identity fields

| Domain | Primary identity | Collision/relationship inputs | Example shape (illustrative only) |
|---|---|---|---|
| Exercise | verified stable `slug` when present; otherwise normalized English name | category, equipment, target muscle seed key, and other approved identity fields | `exercise.barbell-bench-press.<digest>` |
| Muscle | normalized English name + body part | Arabic name only for disambiguation; anatomy mapping is a relationship | `muscle.pectoralis-major.<digest>` |
| Muscle group | normalized canonical group/body-part name | bilingual label for disambiguation | `muscle-group.upper-body.<digest>` |
| Food | normalized English name + serving unit + category | Arabic name for disambiguation; nutrition values are attributes, not identity | `food.chicken-breast-gram.<digest>` |
| Equipment | normalized canonical label | bilingual label where needed | `equipment.barbell.<digest>` |
| Exercise category | normalized canonical label | domain namespace | `exercise-category.strength.<digest>` |
| Level | normalized label + scope (`exercise_difficulty` or `plan_level`) | no cross-domain collapsing | `level.exercise-difficulty.expert.<digest>` |
| Food unit | normalized canonical unit label | conversion family only if approved | `food-unit.gram.<digest>` |

The examples are shapes, not seed records and do not authorize invented data.

## Provenance envelope

The target canonical seed record envelope contains:

```json
{
  "seed_key": "<generated-key>",
  "source": "top-gym",
  "source_id": "<optional-provenance-only-value>",
  "source_path": "<verified-file-or-runtime-source>",
  "destination_table": "<approved-LogicFit-table>",
  "version": "v1",
  "relationships": { "<domain>_seed_keys": [] },
  "validation": { "status": "pending" },
  "record": {}
}
```

The exact destination table is a Phase 2 SQL contract. `source_id` is never the LogicFit identity.

## Determinism and idempotency checks

The seed runner must reject duplicate keys, missing relationship keys, malformed identities, and cross-domain scope collisions. Running the same `v1` input twice must produce the same key set and no duplicate canonical records. A changed semantic identity creates a new key and must be handled as an explicit data migration; it must not be hidden by array order.

