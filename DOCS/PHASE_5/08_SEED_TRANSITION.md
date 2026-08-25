# Seed Transition: Draft Node Seed → Official .NET Seed

## Authority

The Phase 3 canonical JSON package under `database/seeds/v1/` is unchanged. The official executor is the native C# `CanonicalLibrarySeeder`, coordinated by `SeedCoordinator` and invoked by the .NET API command. The duplicate Node SQL seed runner and duplicate Node auth seed package were removed.

Authentication reference data is represented by the native `PermissionCatalog`: 15 permissions, 3 roles, and 14 role-permission assignments. No fake production user is seeded.

## Behavior

- reads the approved manifest and dataset files;
- installs in dependency order;
- derives deterministic GUIDs from stable seed keys, independent of JSON order and TOP GYM numeric IDs;
- upserts by `seed_key` and preserves canonical provenance payloads;
- validates foreign-key relationships before commit;
- records checksums/counts in `library.__seed_installations`;
- treats `food-conversions` as contract-only;
- runs the Gym operation in a SQL Server transaction;
- is safe to run repeatedly and does not copy operational members/customers.

## Verified results

The .NET seed was run twice on a fresh validation pair and on the current local pair. Both runs returned `ValidationPassed=true`; the second run produced no duplicate seed keys. Current canonical counts include 1,133 LogicFit exercise records (the approved output mapping of 1,138 TOP GYM records), 297 muscles, and 367 foods. The full manifest total is 2,074 records including the six contract-only conversion records.
