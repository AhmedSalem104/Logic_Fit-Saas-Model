# Seed Test Results

## Local SQL Server test

| Test | Result |
|---|---|
| SQL Server connection (`localhost`, Windows auth) | GREEN |
| Empty dedicated test database created | GREEN — `LogicFit_SeedValidation_v1_03` |
| Harness schema applied | GREEN |
| First v1 seed apply | GREEN; one transaction committed |
| Canonical counts and core FK verification | GREEN |
| Second v1 seed apply | GREEN; no duplicate rows |
| Stable database IDs across repeated apply | GREEN; every seed-key → GUID comparison unchanged |
| Manifest installation metadata | GREEN; 11 domains, food-conversions marked contract-only |

Verified final counts include 1,133 exercises, 297 muscles, 31 equipment rows, 13 exercise categories, 6 levels, 194 anatomy rows, 17 food categories, 6 units, and 367 foods. Relationship tables contain 3,260 exercise-muscle rows and 1,133 exercise-equipment rows. Duplicate exercise and food seed-key queries returned zero.

## Determinism

The generator was run twice with the same `LOGICFIT_SEED_GENERATED_AT` metadata. All `database/seeds/v1/*.json` SHA-256 values were identical. Generation date is not part of any identity or dataset checksum.

## Scope safety

No SQL connection, write, migration, or file write was made against TOP GYM. The existing TOP GYM worktree changes were preserved.

## Phase 4 integration addendum — 2026-08-25

The same canonical package was applied after the Phase 4 Gym migration on `LogicFit_Gym_001_Local` and verified again. The second apply produced no duplicate records.
