# TOP GYM Live SQL Read-only Snapshot

**Read date:** 2026-08-25  
**Operation:** metadata/count queries only; no DDL, DML, migration, seed, backup, restore, or application startup.  
**Source:** TOP GYM `.env` connection as configured in the legacy repository.

## Connection metadata

| Field | Observed |
|---|---|
| Database | `db62278` |
| SQL Server name | `S14` |
| Client | Node `mssql` using TOP GYM configured connection |

This is a legacy configured SQL Server connection, not the LogicFit local database. Its environment classification is not inferred from the connection string.

## Read-only row counts

| Table | Rows observed |
|---|---:|
| `dbo.members` | 111 |
| `dbo.memberships` | 117 |
| `dbo.gym_attendance` | 149 |
| `dbo.gym_muscles` | 297 |
| `dbo.gym_foods` | 367 |
| `dbo.gym_exercises` | 1,138 |
| `dbo.workout_programs` | 4 |
| `dbo.diet_plans` | 1 |
| `dbo.body_measurements` | 0 |

The observed library count is consistent with `873 active + 265 legacy-compatibility` exercise rows reported by the DB audit. The snapshot does not establish which organization or environment owns the configured database.

## Interpretation boundary

These counts are a dated runtime observation only. They do not override source JSON counts, schema evidence, the Master Bible, or approved LogicFit decisions. No personal rows or secret configuration values were printed.

**BLOCKED: SPECIFICATION GAP** â€” the legacy connectionâ€™s environment/ownership classification and tenant boundary are not encoded in TOP GYM source.

## Source Consolidation Resolution - 2026-08-25

The snapshot is retained as dated runtime evidence. Its missing environment/ownership classification is not guessed. Control Plane ownership, database-per-Gym registration, and tenant isolation are LogicFit architectural requirements and will not be added to TOP GYM.

Additional read-only exercise verification: dbo.gym_exercises has 1,138 rows, with metadata_json.catalogStatus: 873 active and 265 legacy-compatibility. No DDL, DML, migration, or seed was executed.

