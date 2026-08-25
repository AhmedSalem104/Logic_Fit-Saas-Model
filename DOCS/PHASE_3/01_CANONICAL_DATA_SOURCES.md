# Canonical Data Sources and Boundaries

| Dataset | Verified source | Source evidence | Output | LogicFit destination | Classification |
|---|---|---:|---:|---|---|
| Muscle groups | `data/library/muscles.json.bodyPart` | 297 muscle rows | 4 | `library.muscle_groups` | TOP GYM-derived lookup |
| Muscles | `data/library/muscles.json` + `public/data/muscle-assets.json` | 297 + 297 manifest rows | 297 | `library.muscles` | TOP GYM source; runtime array ID is provenance only |
| Equipment | `exercises.json` + `exercises-legacy.json` equipment values | 31 normalized output labels | 31 | `library.equipment` | TOP GYM-derived; no unsupported synonym merge |
| Exercise categories | active and legacy exercise category values | 13 normalized output labels | 13 | `library.exercise_categories` | TOP GYM-derived; case variants normalize |
| Levels | exercise difficulty files plus audited plan-level UI/service vocabulary | 3 + 3 concepts | 6 | `library.levels` | Two separate approved concepts |
| Exercises | `data/library/exercises.json`, duplicate evidence file, and `exercises-legacy.json` | 873 + 873 duplicate evidence + 265 legacy | 1,133 canonical rows / 1,138 source refs | `library.exercises` | 5 exact semantic legacy duplicates are merged with provenance |
| Anatomy mappings | `public/data/anatomy-muscle-mapping.json` | 194 verified mapping entries | 194 installable / 165 unsupported metadata rows | `library.anatomy_mappings` | Source-backed only |
| Food categories | `data/library/foods.json.category` | 367 food rows | 17 | `library.food_categories` | TOP GYM-derived lookup |
| Units | food source values plus Master Bible approved unit list | gram 327, ml 40; six approved units | 6 | `library.food_units` | Four contract metadata units have no TOP GYM records |
| Foods | `data/library/foods.json` | 367 | 367 | `library.foods` | Nutrition values remain embedded |
| Food conversions | No TOP GYM conversion table/file; Phase 2 conversion contract | 0 source conversions | 6 same-unit identity records | No Phase 2 table | Contract-only; cross-unit/density conversions unsupported |

## Data safety

The generator reads only the paths above. No member/customer or private operational data is copied. Numeric TOP GYM identifiers are retained only inside provenance and are never LogicFit identities.

