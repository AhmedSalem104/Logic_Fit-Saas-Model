# Seed Manifest

The authoritative manifest is [`database/seeds/manifest.json`](../../database/seeds/manifest.json). It is version `v1`, uses the schema `logicfit.seed.manifest.v1`, and records install order, dependencies, source paths, counts, and SHA-256 checksums. `generated_at_utc` and validation timestamps are metadata only; they are not seed identity.

| Dataset | File | Destination | Records | Unresolved | SHA-256 |
|---|---|---|---:|---:|---|
| muscle-groups | `v1/muscle-groups.json` | `library.muscle_groups` | 4 | 0 | `05bbc11f07ea5442dda1f5edd0e3044d7af5dc540295eeb3f0f84a8a1f34c1d2` |
| muscles | `v1/muscles.json` | `library.muscles` | 297 | 0 | `4b033a1277ca3fab3f504a1135179b962bec10d3a729f6b0fb23391e1e2e41fd` |
| equipment | `v1/equipment.json` | `library.equipment` | 31 | 0 | `f74026609e105946ae3b2e541cb1e5b2bfce35fac011b9b56ea016bad06b9c47` |
| exercise-categories | `v1/exercise-categories.json` | `library.exercise_categories` | 13 | 0 | `71d13b99bcedd049bfc5be5ffa4a7a75565a8ab2995c2cbe6151696978d9a423` |
| levels | `v1/levels.json` | `library.levels` | 6 | 0 | `86c6bea4ec0ab98ff1e357dca48322daca2eeac829bec07ff347cda28d39315d` |
| exercises | `v1/exercises.json` | `library.exercises` | 1,133 | 0 | `4d73462c0ff090288ab296ec512e3ae05f486edbf08dfd15a5a1ebcaab3a75e5` |
| anatomy-mappings | `v1/anatomy-mappings.json` | `library.anatomy_mappings` | 194 | 165 | `111ea14309ffe3b6058c76bf6d7939080044533b735416179d04dd41f52529d2` |
| food-categories | `v1/food-categories.json` | `library.food_categories` | 17 | 0 | `dbf80d7eb620199a20ec8e026ddac247936c3afbbbbb0fa8b81ce35b3d897e62` |
| units | `v1/units.json` | `library.food_units` | 6 | 0 | `b9484c9ea87fa655336942269cec21eaa8a231dc7b58f7f910ea8a8c6ec114c0` |
| foods | `v1/foods.json` | `library.foods` | 367 | 0 | `a83d89a58b962f8a516091f70eeeafc2d392884d1ad991b6bafe107e60749a87` |
| food-conversions | `v1/food-conversions.json` | contract-only | 6 | 0 | `01c1c4a56d98e67085397cd04cea872da9199ea63cf526e3e3afbebc50481f2f` |

The manifest reports validation `GREEN` with three explicit warnings: source slug collisions handled by deterministic suffixes, unavailable Arabic labels for lookup records, and the documented Anatomy audit-summary discrepancy.

