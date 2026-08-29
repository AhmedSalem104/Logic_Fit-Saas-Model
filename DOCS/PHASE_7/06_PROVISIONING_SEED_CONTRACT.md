# Phase 7 - Provisioning Migration and Seed Contract

**Status:** GREEN - approved EF migration and canonical seed execution are wired into provisioning and verified locally.
**Migration authority:** EF Core only.
**Seed authority:** the unchanged Phase 3 canonical seed package and the
existing .NET runtime seed executor.

## New Gym initialization order

After database creation, the workflow performs:

1. apply the complete approved EF Core Gym migration set to the generated
   database;
2. create/bind the single `core.gym_context` record to the Control Plane Gym
   ID;
3. run the Phase 3 canonical library datasets in their approved dependency
   order;
4. verify migration history, schema, context binding, canonical counts,
   deterministic identities, and foreign-key integrity;
5. initialize the first Owner through the existing Phase 5B identity/RBAC
   path; and
6. activate only after verification succeeds.

EF migration and seed execution are internal workflow steps. They are not
public `/migrate` or `/seed` APIs and do not receive invented permissions.

## Phase 3 datasets

The following package remains authoritative and is not regenerated or
modified:

```text
database/seeds/v1/
  muscle-groups
  muscles
  equipment
  exercise-categories
  levels
  exercises
  anatomy-mappings
  food-categories
  units
  foods
```

The canonical identity and expected reference counts remain:

- exercises: 1,133;
- muscles: 297;
- foods: 367; and
- anatomy mappings: 194.

Phase 7 runs these reference/library seeds for each newly created Gym in the
existing approved order. Authentication and Gym context records are
initialization records, not replacements for the canonical seed datasets.
No demo members, memberships, payments, transactions, or other business
data are seeded.

## Determinism and idempotency

- Stable seed keys and deterministic IDs are preserved exactly from Phase 3.
- Foreign keys are inserted in dependency order.
- Existing matching seed keys are left intact; a rerun does not duplicate
  or replace canonical identity.
- A seed failure records `SeedingFailed`, a safe category/code, attempt, and
  audit event. A controlled retry reruns only the failed/idempotent step.
- Seed version is recorded in `platform.gym_databases.seed_version` after
  successful verification.
- Phase 3 JSON, seed identity, counts, and canonical source provenance are
  not changed by Phase 7.

## Failure rules

Migration failure maps to `MigrationFailed`; seed failure maps to
`SeedingFailed`. A partial database is retained, never silently dropped, and
is reusable only when the provisioning ownership marker matches. No backup or
restore is performed during fresh provisioning. EF Core remains the only
schema migration mechanism and the .NET seed executor remains the only
runtime seed mechanism.

## Implementation verification

Provisioning invokes `GymDbContext.Database.MigrateAsync` and then the
existing `CanonicalLibrarySeeder` against the generated Gym database. The
isolated API test verifies the four Phase 3 reference counts and reruns the
same seeder to prove zero duplicate canonical records. The Phase 3 JSON files
remain unchanged.
