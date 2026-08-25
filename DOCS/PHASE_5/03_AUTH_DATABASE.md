# Authentication/RBAC Database Transition

## Scope

The Control Plane owns platform identity, credentials, sessions, MFA/recovery state, roles, permissions, assignments, migration metadata, and platform audit. A Gym database owns the Gym-side user projection, Gym context, Gym audit, and operational/library data. No shared operational database was introduced.

## EF Core entities and schemas

Control Plane foundation/auth entities:

- `platform.organizations`, `platform.gyms`, `platform.gym_databases`, `platform.feature_flags`;
- `iam.users`, `iam.credentials`, `iam.sessions`, `iam.mfa_factors`, `iam.password_reset_tokens`, `iam.mfa_recovery_codes`;
- `iam.roles`, `iam.permissions`, `iam.role_permissions`, `iam.user_gym_roles`;
- `migrations.definitions`, `migrations.runs`;
- `audit.events`.

Gym foundation/auth entities:

- `core.gym_context`;
- `auth.gym_users`;
- `audit.events`;
- Phase 3 canonical library tables under `library`.

The exact columns, keys, indexes, constraints, timestamps, status fields, and row-version mappings are in `ControlPlaneDbContext`, `GymDbContext`, and their EF migrations.

## Local database evidence

Before the transition, the local databases were inspected:

- `LogicFit_ControlPlane_Local` contained the draft auth catalog (15 permissions, 3 roles, 14 assignments), zero users, and two rows in `migrations.schema_migrations`.
- `LogicFit_Gym_001_Local` contained one Gym context, zero Gym users, the Phase 3 library data, and three rows in `migrations.schema_migrations`.
- Neither database had EF history before the transition.

The local databases were backed up with COPY_ONLY/CHECKSUM before changes. The exact backup names are recorded in `07_MIGRATION_TRANSITION.md`.

## Retained and removed structures

Retained: all entities required by the approved Phase 2 foundation/auth/library contract, including `migrations.definitions` and `migrations.runs` as Control Plane operational metadata.

Removed locally: the obsolete `migrations.schema_migrations` marker table after its state was recorded and the EF history was baselined. No user rows or Gym operational data were deleted.

One legacy schema drift was found in `LogicFit_Gym_001_Local.library.anatomy_mappings`: the draft database lacked `name_ar` and `name_en`. The guarded local alignment script recovered names from the verified canonical provenance payload and added the contract columns. Fresh databases receive them from the official EF migration.
