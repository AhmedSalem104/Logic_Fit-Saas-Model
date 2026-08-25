# Phase 3 Gaps, Risks, and Classifications

All data decisions required for the approved Phase 3 seed package are resolved or explicitly classified below; no unresolved data-decision blocker remains.

| Item | Classification | Treatment |
|---|---|---|
| Arabic labels for derived lookup/unit records | IMPLEMENTATION NOTE / source limitation | `name_ar=null` with explicit provenance; no guessed translation. |
| 54 source exercise slug collision groups / 108 records | RESOLVED | Canonical slug receives an identity-hash suffix; `source_slug` is preserved. |
| TOP GYM equipment `is_required` flag absent | IMPLEMENTATION NOTE | Runner preserves it as NULL in the local relation harness; no required/optional business rule is invented. |
| Food conversions absent in TOP GYM | RESOLVED / contract-only | Six identity conversions only; density, serving, piece, and implicit cross-unit conversions are unsupported. |
| Anatomy audit summary says nine ambiguous elements | RESOLVED evidence classification | The verified manifest has 194 documented entries; the nine-count remains audit metadata and no unverified mapping is promoted. |
| Production schema/migration integration | IMPLEMENTATION NOTE | Deferred to Phase 4; local harness is not a production migration. |
| Binary media redistribution/licensing | IMPLEMENTATION NOTE | Only source manifest references are seeded; asset approval/storage adapter remains later scope. |

These notes do not authorize business-feature implementation or a change to TOP GYM.
