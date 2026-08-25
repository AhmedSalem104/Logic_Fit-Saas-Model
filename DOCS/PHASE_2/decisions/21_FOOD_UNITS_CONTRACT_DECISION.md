# Decision 2.21 — Food Units and Conversions

**Status:** APPROVED  
**Authority:** Phase 2 Final Gap Resolution plus Master Bible seed contract  
**Scope:** Canonical food seed and Nutrition quantities.

## Decision

- The canonical seed representation remains `units.json` → `library.food_units`; nutrition values remain embedded in `foods.json` → `library.foods`.
- A conversion is valid only when explicit metadata exists for the food/unit relationship. Unsupported serving, piece, density, or other conversions fail validation; no general-knowledge conversion is invented.
- Seed keys and conversion identities are deterministic and independent of JSON ordering and TOP GYM numeric IDs.
- Phase 3 may extract only verified source conversions and explicitly approved canonical metadata.
