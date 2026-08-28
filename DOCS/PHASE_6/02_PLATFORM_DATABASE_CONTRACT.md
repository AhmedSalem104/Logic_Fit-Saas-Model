# Phase 6 Platform Database Contract

**Status:** GREEN — contract closed; implementation not started
**Database engine:** Microsoft SQL Server

This is the canonical Phase 6 database scope. It does not authorize an EF
migration in this task.

## Locked topology and ownership

- Control Plane owns organizations, Gym registry metadata, database registry
  metadata, platform configuration metadata, and platform audit.
- Each Gym owns its operational data in its own database.
- No Gym operational table is moved to the Control Plane.
- No shared `tenant_id` operational database is introduced.
- Phase 7 owns provisioning, database creation, placement execution, and
  provisioning lifecycle execution.

## Phase 6 retained/required tables

### `platform.organizations`

| Column | SQL Server type | Nullability/default | Key/index |
|---|---|---|---|
| `organization_id` | `uniqueidentifier` | NOT NULL; `NEWSEQUENTIALID()` | PK `PK_platform_organizations` |
| `name` | `nvarchar(160)` | NOT NULL | — |
| `slug` | `nvarchar(120)` | NOT NULL | unique `UQ_platform_organizations_slug` |
| `status` | `nvarchar(30)` | NOT NULL; default `active` | Existing status value; no new state introduced |
| `created_at_utc` | `datetime2(3)` | NOT NULL | — |
| `updated_at_utc` | `datetime2(3)` | NOT NULL | — |
| `row_version` | `rowversion` | NOT NULL | Optimistic concurrency |

Purpose: safe organization registry metadata. Phase 6 does not include a
plan/subscription foreign key or commercial fields.

### `platform.gyms`

| Column | SQL Server type | Nullability/default | Key/index |
|---|---|---|---|
| `gym_id` | `uniqueidentifier` | NOT NULL; `NEWSEQUENTIALID()` | PK `PK_platform_gyms` |
| `organization_id` | `uniqueidentifier` | NOT NULL | FK to `platform.organizations`; restrictive delete |
| `name` | `nvarchar(160)` | NOT NULL | — |
| `slug` | `nvarchar(120)` | NOT NULL | unique `(organization_id, slug)` |
| `status` | `nvarchar(30)` | NOT NULL; existing default `provisioning` | Preserve existing source states; Phase 6 lifecycle minimum is Active/Inactive |
| `timezone_name` | `nvarchar(80)` | NOT NULL; default `Africa/Cairo` | — |
| `created_at_utc` | `datetime2(3)` | NOT NULL | — |
| `updated_at_utc` | `datetime2(3)` | NOT NULL | — |
| `row_version` | `rowversion` | NOT NULL | Optimistic concurrency |

Phase 6 exposes registry metadata only. Owner assignment, app-version
metadata, and provisioning state execution remain later/Phase 7 concerns.

### `platform.gym_databases`

| Column | SQL Server type | Nullability/default | Key/index |
|---|---|---|---|
| `gym_database_id` | `uniqueidentifier` | NOT NULL; `NEWSEQUENTIALID()` | PK `PK_platform_gym_databases` |
| `gym_id` | `uniqueidentifier` | NOT NULL | FK to `platform.gyms`; restrictive delete |
| `database_name` | `nvarchar(128)` | NOT NULL | unique `(environment, database_name)` |
| `environment` | `nvarchar(30)` | NOT NULL; default `local` | — |
| `schema_version` | `nvarchar(80)` | NULL | — |
| `seed_version` | `nvarchar(80)` | NULL | — |
| `status` | `nvarchar(30)` | NOT NULL; default `pending` | Existing status value |
| `connection_secret_ref` | `nvarchar(240)` | NULL | Never returned or logged |
| `last_health_at_utc` | `datetime2(3)` | NULL | — |
| `created_at_utc` | `datetime2(3)` | NOT NULL | — |
| `updated_at_utc` | `datetime2(3)` | NOT NULL | — |
| `row_version` | `rowversion` | NOT NULL | Optimistic concurrency |

No `server_id` is added in Phase 6. Server placement metadata is explicitly
deferred to the Phase 7 placement/provisioning contract; the current local
registry remains valid without a server relation.

### `platform.feature_flags`

The existing table is retained as the single approved feature-flag boundary:

`feature_flag_id uniqueidentifier PK`, `flag_key nvarchar(160) NOT NULL`,
`scope_type nvarchar(30) NOT NULL`, `scope_id uniqueidentifier NULL`,
`enabled bit NOT NULL DEFAULT 0`, `config_json nvarchar(max) NULL`,
`created_at_utc datetime2 NOT NULL`, and `updated_at_utc datetime2 NOT NULL`.

The unique identity is `(flag_key, scope_type, scope_id)`. No Phase 6 keys,
records, mutation API, or new permission are introduced. Scope semantics are
limited to the approved Platform/Gym boundary; flags never grant permissions.

### `audit.events`

There is one audit system. The canonical Phase 6 target shape is the existing
Phase 5B fields plus:

`scope_type nvarchar(30) NULL` and `scope_id uniqueidentifier NULL`.

Existing rows may remain scope-null for backward compatibility. New
platform/Gym-scoped events must set the relevant scope fields where required
by the event contract. The reconciliation is additive and must be delivered
by one safe EF Core migration when implementation begins; no second audit
table/schema is allowed.

Existing fields remain: `audit_event_id`, `request_id`, `actor_user_id`,
`target_type`, `target_id`, `action`, `result`, `reason`, `metadata_json`,
and `occurred_at_utc`. Secrets and raw credentials are prohibited.

## Explicitly deferred/not-required tables

| Table/category | Phase 6 decision |
|---|---|
| `platform.servers` | Deferred to placement/provisioning work; no Phase 6 table/API is required for the local registry-only scope. |
| `platform.plans` | Not required; commercial plans/subscriptions are deferred. |
| `platform.settings` | Not required; no arbitrary settings framework or keys are approved. |
| `operations.*` metrics, queues, backups, restores, deployments | Later Platform Operations; no real-time monitoring infrastructure in Phase 6. |
| `provisioning.*` | Phase 7 only. |
| Gym `members.*` and all operational tables | Phase 8/later business slices. |

## Consumers and security

Phase 6 read APIs consume only the retained Control Plane tables. No route
opens a Gym database for member or business data. `connection_secret_ref` is
never part of a DTO. All future mutations require exact permission,
optimistic concurrency, audit, and the Active/Inactive lifecycle rules.
