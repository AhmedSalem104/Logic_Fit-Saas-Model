# Phase 2 Database Contract

**Database engine:** Microsoft SQL Server  
**Status:** Contract approved for design; implementation deferred.  
**Sources:** `DECISION_LOCK.md`, `TOP_GYM_LOGICFIT_DATABASE_DECISION.md`, Master Bible platform/seed/domain documents, and the completed TOP GYM runtime audit.

## Topology

```text
LogicFit_ControlPlane
  platform / iam / provisioning / migrations / operations / audit

LogicFit_Gym_<allocated_code>  (one database per Gym)
  core / auth / members / library / training / nutrition / crm /
  finance / commerce / inventory / classes / documents /
  notifications / reports / audit
```

`LogicFit_ControlPlane` is authoritative for Organizations, Gyms, server/database registry, provisioning, platform users and permissions, feature flags, platform settings, migration orchestration, backup metadata, operational metadata, and platform audit. A Gym DB is authoritative for that Gym's operational records only.

## Routing and isolation contract

1. Every authenticated request resolves an actor, an allowed scope, and a target `gym_id` before a Gym DB connection is acquired.
2. The connection resolver accepts only a Control Plane database-registration record whose status is enabled/healthy enough for the requested operation.
3. No API accepts a caller-supplied connection string or database name.
4. No Gym DB query may route to another Gym DB. A Gym DB has a `core.gym_context` binding and every Gym-owned table carries `gym_id` where useful for defensive predicates and audit traceability.
5. Cross-database references use `*_control_plane_id`/`*_external_id` values and service-level verification; SQL foreign keys do not cross the Control Plane/Gym database boundary.
6. Global migration and provisioning are the only approved Control Plane workflows allowed to enumerate multiple Gym DBs, and they operate through adapters with independent target status.

## SQL conventions

| Concern | Contract |
|---|---|
| Primary key | `uniqueidentifier NOT NULL`, server-generated; clustered choice is an implementation note. |
| Seed identity | `nvarchar(160) NOT NULL`, unique within `(dataset, seed_version, seed_key)` or the table's canonical identity scope. |
| Text | `nvarchar`; explicit length in the catalog. No unbounded text for ordinary fields. |
| Money/quantities | `decimal(19,4)` for financial amounts; `decimal(12,3)` for nutrition quantities/totals; other domain decimals are specified in the table catalog. |
| Boolean | `bit NOT NULL` with explicit default where a default is safe. |
| Status | `nvarchar(40)` plus a check constraint or lookup contract; no free-form UI labels. |
| Version | `int` or `nvarchar(80)` according to the catalog; published snapshots include a content hash. |
| Instant | `datetime2(3) NOT NULL` UTC unless explicitly nullable. |
| Concurrency | `rowversion NOT NULL` on mutable aggregate roots. |
| JSON | `nvarchar(max)` only for validated snapshots/provenance/adapter payloads; a schema/version field is required. |
| Soft deletion | `deleted_at_utc`/`deleted_by_user_id` for records whose history matters; immutable canonical records use `active`/archive semantics. |

## Shared audit columns

Unless a table is immutable seed metadata or an append-only audit table, the catalog's `standard_audit` means:

```text
created_at_utc datetime2(3) NOT NULL
created_by_user_id uniqueidentifier NULL   -- NULL only for system/seed actor
updated_at_utc datetime2(3) NOT NULL
updated_by_user_id uniqueidentifier NULL
row_version rowversion NOT NULL
deleted_at_utc datetime2(3) NULL
deleted_by_user_id uniqueidentifier NULL
```

Append-only audit tables additionally carry `request_id`, `actor_user_id`, `target_type`, `target_id`, `action`, `result`, `reason`, and safe metadata. Secrets and raw QR tokens are prohibited.

## Ownership and FK rules

- A table marked **Control Plane** may reference other Control Plane tables only.
- A table marked **Gym** may reference Gym tables in the same database only.
- A Gym table may retain a `control_plane_*_id` for identity/registry linkage, but the API must verify it against the current Gym context.
- Canonical lookup FKs are required for canonical IDs. AI/automatic generation may persist only IDs that resolve to active canonical rows or explicitly allowed Gym-owned extensions.
- Published version tables store immutable JSON/hash snapshots and do not depend on mutable current library rows for historical display.

## Migration contract

- Migration definitions are versioned and ordered; target state is recorded per Gym DB.
- A global rollout uses Preview → Compatibility → Backup → Canary → Batch → Verify.
- A single failed Gym target yields `PARTIAL_FAILURE`, never global success.
- Provisioning runs base migrations before seed and records each step as Pending/Running/Success/Failed/Retryable.
- Migrations must be additive/compatible where possible; destructive changes require an approved backup/restore plan and explicit high-risk permission.
- This phase creates no `.sql` migration and does not connect to or change TOP GYM.

## Security contract

- Control Plane and Gym DB credentials are adapter-managed and never returned by API responses or ordinary logs.
- SQL access is through parameterized repositories; no client-supplied table/database identifiers.
- Permission and scope checks occur before repository access.
- Audit records are append-only from application paths.
- Public QR lookup uses only the hashed opaque token contract in `16_QR_CONTRACT.md`.

## Final gap-resolution database rules — 2026-08-25

The following persisted boundaries are canonical and must be represented in the SQL Server table catalog before implementation:

- SQL-backed sessions are Gym/Control-Plane scoped as defined by the authentication boundary; password-reset tokens are single-use, expiring, hash-only records, and MFA recovery codes are protected records.
- Member-code Portal access has protected access-code metadata and revocable, expiring, Gym/member-scoped portal sessions. It is not a second username/password identity system and is separate from QR tokens.
- Finance and commerce money uses explicit currency (default `EGP`) and SQL Server `DECIMAL(19,4)`; tax is explicit and defaults to `0%`; completed sales and refunds are stateful and auditable.
- Store costing is Weighted Average Cost. Sales, purchases, returns, stock movements, and adjustments are transaction boundaries; completed sales are never hard-deleted.
- Classes persist recurrence kind (`one_time`/`weekly`), explicit boundaries, capacity, waitlist enablement, cancellation cutoff (default two hours), bookings, waitlist order, and separate attendance/no-show state.
- CRM persists the canonical default pipeline stages and follow-up due/type/note/owner/completed state; overdue is a server-derived query state.
- Documents persist uploader, subject/member, category, MIME type, size, storage key, status, and retention metadata. Storage is accessed only through `StorageAdapter`.
- In-app notifications persist recipient, type, title, body, read state, related entity, and optional action/deep-link metadata.
- Reports are on-demand/server-side; persisted report runs contain source/report key, parameters, filters/date semantics, output reference, status, and audit context. Monitoring thresholds and backup/DR policy are Control Plane operational metadata.

These rules refine the table catalog and do not authorize migrations or production schema creation in Phase 2.

## Phase 4 foundation addendum — 2026-08-25

Phase 4 has now created only the approved local technical foundation: Control Plane identity/registry/auth/migration/audit tables, Gym context/migration/audit tables, and the Phase 3 reference/library seed target projection. The executable local migration runner is documented under `../PHASE_4/04_MIGRATION_FOUNDATION.md`. Operational business tables and production migrations remain deferred.
