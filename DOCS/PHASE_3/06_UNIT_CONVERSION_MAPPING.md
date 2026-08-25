# Unit and Conversion Mapping

## Units

`units.json` contains the six approved units: `gram`, `kilogram`, `milliliter`, `liter`, `piece`, and `serving`. TOP GYM evidence directly covers `gram` and `ml` (`milliliter`); the remaining approved units are explicit Master Bible/Phase 2 contract metadata and have zero TOP GYM food records. Arabic labels are `null` where no approved source label exists; no translation was guessed.

Each unit has a stable key, code, dimension, base quantity `1`, and self base-unit code. This metadata does not imply cross-unit conversion.

## Conversions

`food-conversions.json` is a contract-only artifact because Phase 2 defines no `library.food_conversions` destination table. It contains six deterministic identity conversions (`unit → same unit`, factor `1`, exact rounding) to make supported behavior explicit.

The following remain explicitly unsupported and fail backend validation until a source-backed relationship is approved:

- gram/kilogram conversion when a food-specific quantity relationship is required;
- milliliter/liter conversion where source basis is not explicit;
- piece or serving to mass/volume;
- any density or medically/nutritionally inferred conversion.

No conversion is silently applied to a food record. Published Nutrition plans will use the approved calculation-engine snapshot rules in later phases.

