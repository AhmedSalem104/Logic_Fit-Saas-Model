# Migration Transition: Draft Node SQL → Official EF Core

## Official rule

There is one LogicFit migration system: EF Core migrations in `LogicFit.Infrastructure`. The old Node/Fastify SQL migrations `0001_control_plane_foundation`, `0002_auth_rbac_vertical_slice`, `0001_gym_foundation`, `0002_library_seed_targets`, and `0003_auth_rbac_projection` were draft/legacy implementation migrations and were removed from the executable project.

## Safety sequence performed

1. Inspected database names, tables, columns, constraints/indexes/foreign-key shape, migration markers, seed state, auth counts, and operational row counts.
2. Verified the exact local targets: `LogicFit_ControlPlane_Local` and `LogicFit_Gym_001_Local`.
3. Confirmed there were zero users and zero Gym users before the transition.
4. Created verified COPY_ONLY/CHECKSUM backups:
   - `LogicFit_ControlPlane_Local_Phase5_arch_correction_20260825.bak`
   - `LogicFit_Gym_001_Local_Phase5_arch_correction_20260825.bak`
   under the SQL Server backup directory.
5. Ran the database-name-guarded `database/scripts/phase5-local-ef-baseline.sql`.
6. Recorded the official EF migrations in `dbo.__EFMigrationsHistory`:
   - `20260825144155_InitialControlPlaneFoundation` / `10.0.0`;
   - `20260825144011_InitialGymFoundation` / `10.0.0`.
7. Removed only the obsolete `migrations.schema_migrations` table from these local databases.
8. Ran the guarded anatomy schema alignment described in `03_AUTH_DATABASE.md`.
9. Verified `dotnet --migrate` reports both databases up to date.

## Fresh database proof

Fresh validation databases were created locally and migrated using the official EF migrations. No Node SQL migration was used. The fresh Control Plane and Gym histories contain the corresponding official migration IDs and the complete EF model.

Future schema changes must be created and applied with EF Core migrations. The baseline/alignment scripts are historical local transition evidence, not a second migration runner.
