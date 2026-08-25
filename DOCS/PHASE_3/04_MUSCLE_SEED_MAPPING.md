# Muscle Seed Mapping

`data/library/muscles.json` contains 297 bilingual muscle records. Every output row preserves the Arabic name, English name, body part, description, Arabic description, icon, and a group seed-key relationship.

## Identity and provenance

The key identity is normalized English name + body part + Arabic disambiguator. The runtime/library array index (`1..297`) is stored only as `source_id` with kind `top-gym-runtime-array-index-provenance-only`. It is not a LogicFit primary key or seed key.

Muscle groups are derived from the four verified body-part values: `Arms`, `Core`, `Lower Body`, and `Upper Body`. No Arabic group name was invented because the source does not supply one.

## Media evidence

`public/data/muscle-assets.json` has 297 records: 214 mapped, 83 manual-review, 188 unique canonical structures, and 564 image references. These references remain metadata in the seed record; no image binary is installed by the seed package. Manual-review records are not promoted to a stronger anatomy mapping.

