# Food Seed Mapping

`data/library/foods.json` supplies all 367 canonical food records. Each record preserves the bilingual name, source category, exact serving quantity/unit, and the source nutrition values: calories, protein, carbs, fat, fiber, sugar, and sodium.

## Canonical output

- Output: 367 records; no source deduplication was required.
- Categories: 17 normalized lookup records.
- Serving units: `gram` for 327 foods and `ml` for 40 foods.
- Nutrition values remain embedded in `foods.json`; no `nutrition-values.json` or separate nutrition table is introduced.
- `serving_quantity` and `calculation_quantity` are equal to the exact source basis. The source has gram serving sizes `1`, `5`, and `100`, and milliliter serving size `100`; no unsupported normalization to 100 is performed.

## Identity and relationships

Food identity is normalized English name + serving unit + category + Arabic disambiguator. Category and unit references use stable seed keys. Numeric source array indices are provenance only. All nutrients are required to be finite and non-negative by the validator, but their values are not medically recalculated or corrected.

