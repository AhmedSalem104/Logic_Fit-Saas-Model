# SQL Server Foundation

Database engine is Microsoft SQL Server only.

## Control Plane foundation

The official EF migration `20260825144155_InitialControlPlaneFoundation` creates only the platform/auth/operations boundary needed by later phases:

- `platform.organizations`
- `platform.gyms`
- `platform.gym_databases`
- `platform.feature_flags`
- `iam.users`
- `iam.credentials`
- `iam.sessions`
- `iam.mfa_factors`
- `iam.password_reset_tokens`
- `iam.mfa_recovery_codes`
- `migrations.definitions`
- `migrations.runs`
- `audit.events`

No operational member, finance, commerce, training, nutrition, or CRM tables are created.

## Gym foundation

The official EF migration `20260825144011_InitialGymFoundation` creates:

- `core.gym_context`, binding the database to one Control Plane Gym identity.
- `audit.events`, append-only Gym audit boundary.

The same EF migration imports the Phase 3 reference/library target schema so canonical seed data can be installed after migration. It does not create operational business entities.

EF records applied migrations in `dbo.__EFMigrationsHistory`. The former `migrations.schema_migrations` marker was draft runtime state and was removed from the local databases during the Phase 5 correction.

## Isolation

Control Plane foreign keys stay inside the Control Plane database. Gym operational/library tables stay inside the selected Gym database. Cross-database identities are logical Control Plane references and are verified by services; SQL foreign keys do not cross the boundary.
