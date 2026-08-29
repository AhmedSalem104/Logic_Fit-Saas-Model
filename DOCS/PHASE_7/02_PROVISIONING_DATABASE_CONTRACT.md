# Phase 7 - Provisioning Database Contract

**Status:** GREEN - approved Phase 7 schema is applied and verified locally.
**Engine:** Microsoft SQL Server.
**Migration authority:** EF Core only.
**Change type:** Additive EF Core implementation; no operational data was deleted.

## Database boundary

The Control Plane stores platform and orchestration metadata. A newly
provisioned Gym receives its own SQL Server database. There is no shared
operational tenant database and no cross-database foreign key.

## Control Plane structures

The following are the Phase 7 structures. Existing structures are extended
only where the contract requires it; implementation must inspect the current
EF model and local databases before creating a migration.

### `platform.organizations` (existing registry)

| Column | SQL Server type | Nullability / rule |
|---|---|---|
| `organization_id` | `uniqueidentifier` | PK, server generated |
| `name` | `nvarchar(160)` | NOT NULL |
| `slug` | `nvarchar(120)` | NOT NULL; normalized and unique |
| `status` | `nvarchar(30)` | NOT NULL; existing registry status vocabulary |
| `created_at_utc`, `updated_at_utc` | `datetime2(3)` | NOT NULL UTC |
| `row_version` | `rowversion` | NOT NULL |

An organization is created by the provisioning operation. No plan FK or
commercial subscription field is required by Phase 7.

### `platform.gyms` (existing registry)

| Column | SQL Server type | Nullability / rule |
|---|---|---|
| `gym_id` | `uniqueidentifier` | PK, server generated |
| `organization_id` | `uniqueidentifier` | NOT NULL FK to organization; restrictive delete |
| `name` | `nvarchar(160)` | NOT NULL |
| `slug` | `nvarchar(120)` | NOT NULL; unique within organization |
| `status` | `nvarchar(40)` | NOT NULL; synchronized to the approved provisioning outcome |
| `timezone_name` | `nvarchar(80)` | NOT NULL; validated IANA timezone |
| `owner_user_id` | `uniqueidentifier` | NULL until Owner initialization; Control Plane reference |
| `created_at_utc`, `updated_at_utc` | `datetime2(3)` | NOT NULL UTC |
| `row_version` | `rowversion` | NOT NULL |

Deactivation never deletes the Gym database. Phase 7 does not add a
deactivation operation.

### `platform.servers` (Phase 7 placement metadata)

| Column | SQL Server type | Nullability / rule |
|---|---|---|
| `server_id` | `uniqueidentifier` | PK, server generated |
| `name` | `nvarchar(120)` | NOT NULL; unique with environment |
| `environment` | `nvarchar(30)` | NOT NULL; normalized deployment environment |
| `provider_key` | `nvarchar(80)` | NOT NULL provider-neutral identifier |
| `status` | `nvarchar(30)` | NOT NULL; only `active` servers are valid targets |
| `health_status` | `nvarchar(30)` | NOT NULL; `unavailable` cannot be selected |
| `endpoint_ref` | `nvarchar(240)` | NULL; non-secret adapter reference only |
| `created_at_utc`, `updated_at_utc` | `datetime2(3)` | NOT NULL UTC |
| `row_version` | `rowversion` | NOT NULL |

Constraints: PK, unique `(environment, name)`, status/health check values,
and no password, private key, raw connection string, or credential column.
The registry is metadata, not infrastructure automation.

### `platform.gym_databases` (existing registry extended by Phase 7)

| Column | SQL Server type | Nullability / rule |
|---|---|---|
| `gym_database_id` | `uniqueidentifier` | PK, server generated |
| `gym_id` | `uniqueidentifier` | NOT NULL FK to Gym; restrictive delete |
| `server_id` | `uniqueidentifier` | NOT NULL FK to `platform.servers` |
| `database_name` | `nvarchar(128)` | NOT NULL; unique with environment; system generated |
| `environment` | `nvarchar(30)` | NOT NULL; server/environment derived |
| `schema_version` | `nvarchar(80)` | NULL until migration verification |
| `seed_version` | `nvarchar(80)` | NULL until seed verification |
| `status` | `nvarchar(40)` | NOT NULL; mirrors the approved operation state where applicable |
| `connection_secret_ref` | `nvarchar(240)` | NULL; opaque adapter reference only |
| `last_health_at_utc` | `datetime2(3)` | NULL |
| `created_at_utc`, `updated_at_utc` | `datetime2(3)` | NOT NULL UTC |
| `row_version` | `rowversion` | NOT NULL |

The secret reference is never returned by the API. A database record is
retained after failure; no automatic drop or destructive cleanup is allowed.

### Database name convention

For a newly provisioned database the system generates:

```text
LogicFit_Gym_{gymId:N}_{environment}
```

`gymId:N` is the lowercase 32-character hexadecimal representation of the
server-generated Gym ID. `environment` is the normalized registered-server
environment using lowercase ASCII letters, digits, and hyphens. The result is
checked against SQL Server's 128-character identifier limit and the unique
`(environment, database_name)` constraint. The caller cannot provide or
override the physical database name. Existing local databases are not renamed
by this contract.

### `provisioning.runs` (new Phase 7 orchestration record)

| Column | SQL Server type | Nullability / rule |
|---|---|---|
| `provisioning_run_id` | `uniqueidentifier` | PK; public operation identifier |
| `organization_id` | `uniqueidentifier` | NOT NULL FK to organization |
| `gym_id` | `uniqueidentifier` | NOT NULL FK to Gym |
| `requested_by_user_id` | `uniqueidentifier` | NOT NULL logical FK to IAM user |
| `status` | `nvarchar(40)` | NOT NULL; exact lifecycle vocabulary only |
| `current_step` | `nvarchar(50)` | NULL; approved execution step key |
| `attempt_no` | `int` | NOT NULL, starts at 1, positive |
| `idempotency_key_hash` | `char(64)` | NOT NULL; unique per actor/environment |
| `request_fingerprint` | `char(64)` | NOT NULL; detects key reuse with different input |
| `server_id` | `uniqueidentifier` | NULL until placement selection, then FK |
| `gym_database_id` | `uniqueidentifier` | NULL until registry allocation, then FK |
| `requested_at_utc`, `started_at_utc`, `completed_at_utc` | `datetime2(3)` | requested required; others nullable |
| `failure_category` | `nvarchar(80)` | NULL; safe classification only |
| `error_code` | `nvarchar(80)` | NULL; safe public error code only |
| `safe_error_metadata_json` | `nvarchar(max)` | NULL; validated/redacted; no secrets |
| `created_at_utc`, `updated_at_utc` | `datetime2(3)` | NOT NULL UTC |
| `row_version` | `rowversion` | NOT NULL |

Required indexes/constraints: unique actor/environment/idempotency hash,
unique active operation per Gym, foreign keys to same Control Plane, status
check using the exact lifecycle list, and an index on `(status,
updated_at_utc)` for safe monitoring. The request fingerprint includes the
canonical request excluding the idempotency key and secret values are never
hashed into an externally exposed value.

The implementation also persists the owner identity and bounded retry replay
metadata (`owner_user_id`, `last_retry_*`) needed to resume the same operation
without replaying request secrets or creating a second Owner. These are
internal orchestration fields; they are not additional public response
fields, permissions, or lifecycle states.

### `provisioning.steps` (new Phase 7 execution metadata)

| Column | SQL Server type | Nullability / rule |
|---|---|---|
| `provisioning_step_id` | `uniqueidentifier` | PK |
| `provisioning_run_id` | `uniqueidentifier` | NOT NULL FK to run; cascade is not used for operational history |
| `step_key` | `nvarchar(50)` | NOT NULL; fixed approved order |
| `attempt_no` | `int` | NOT NULL, positive |
| `status` | `nvarchar(20)` | NOT NULL technical status: `Pending`, `Running`, `Success`, or `Failed` |
| `started_at_utc`, `completed_at_utc` | `datetime2(3)` | Nullable UTC |
| `retryable` | `bit` | NOT NULL; property, not a lifecycle state |
| `failure_category`, `error_code` | `nvarchar(80)` | Nullable safe values |
| `safe_metadata_json` | `nvarchar(max)` | Nullable; redacted |

Unique `(provisioning_run_id, step_key, attempt_no)` and an index on the run
and ordered step. Technical step statuses do not add states to the canonical
run lifecycle.

## Reused identity/Gym structures

The operation reuses existing Phase 5B structures:

- Control Plane `iam.users`, `iam.credentials`, and `iam.user_gym_roles`;
- one canonical `gym-security-admin` role assignment for the first Gym Owner;
- Gym `core.gym_context`, bound to the generated Gym ID before the app is unlocked; and
- Gym `auth.gym_users`, the local authorization projection with no password.

## Audit

Phase 7 uses the existing single `audit.events` table. It does not create a
second audit table. Required event names and metadata are in
`04_PROVISIONING_SECURITY_CONTRACT.md`.

## Excluded structures

Phase 7 creates no plans, subscriptions, billing, invoices, members,
attendance, business-module tables, backup/restore tables, or public
migration/seed tables. No Phase 3 seed JSON is changed.

## Applied EF Core migrations

The Control Plane migration history now includes:

- `20260829095350_Phase7ProvisioningFoundation`; and
- `20260829105045_Phase7LifecycleStates`.

The first migration adds the server/placement relation, owner reference,
provisioning run/step tables, and the `platform.provision` seed catalog
entry. The second safely replaces the registry status checks while retaining
the pre-existing local status values and adding the approved provisioning
states. No raw SQL migration runner or legacy migration history is used.
