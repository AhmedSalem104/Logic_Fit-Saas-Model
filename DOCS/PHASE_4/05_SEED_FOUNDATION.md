# Seed Foundation

Phase 3 remains the single canonical seed package. The official executor is the native .NET `CanonicalLibrarySeeder`; no Node seed runner remains in the LogicFit runtime.

## Local flow

```text
fresh Control Plane DB
  → control-plane migration
fresh Gym DB
  → Gym migration 0001
  → reference/library target migration 0002
  → Phase 3 validator
  → Phase 3 dependency-ordered seed
  → verify
  → repeat seed
```

The arrow flow above records the original Phase 4 sequencing; the current implementation uses the same approved dependency order through `CanonicalLibrarySeeder`. Seed installations remain canonical/system-owned and idempotent. The .NET coordinator records the 11 Phase 3 dataset installations and rejects duplicate keys, invalid references, and canonical/custom collisions.

The Phase 3 food-conversions dataset remains contract-only because the approved schema has no destination table for unsupported conversion assumptions. See `DOCS/PHASE_5/08_SEED_TRANSITION.md`.
