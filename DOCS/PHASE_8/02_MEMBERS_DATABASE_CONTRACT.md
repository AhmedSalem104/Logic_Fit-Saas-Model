# Members Database Contract

**Status:** GREEN — schema contract closed
**Database:** selected Gym database only
**Migration authority:** EF Core only

## Canonical table

The core table is `members.members`. No table is added to the Control Plane for operational Member data.

| Column | SQL contract | Rules |
|---|---|---|
| `member_id` | `uniqueidentifier NOT NULL` | System-generated stable PK; immutable |
| `gym_id` | `uniqueidentifier NOT NULL` logical CP reference | Required Gym ownership; no cross-database FK |
| `full_name` | `nvarchar(120) NOT NULL` | Required; server validation |
| `phone` | `nvarchar(30) NOT NULL` | Required; normalized; no global uniqueness |
| `email` | `nvarchar(254) NULL` | Optional; normalized; no global uniqueness |
| `registration_date` | `date NOT NULL` | Date-only Member registration value |
| `notes` | `nvarchar(1000) NULL` | Optional; not copied into logs |
| `status` | `nvarchar(40) NOT NULL` | Exactly `ACTIVE`, `INACTIVE`, or `ARCHIVED` |
| `created_at_utc` / `created_by_user_id` | Existing audit columns | Immutable creation metadata |
| `updated_at_utc` / `updated_by_user_id` | Existing audit columns | Updated on accepted mutation |
| `row_version` | `rowversion NOT NULL` | Optimistic concurrency token |
| archive representation | Explicit `status = ARCHIVED` state | Preserves history; no physical delete and no separate hard-delete path |

The existing Portal contract requires a Member Code for Portal-enabled Members. It is Gym-unique, is not a database identifier, and is not changed by normal Member update. Its protected access material remains governed by the existing Portal access-code contract; no second code system is introduced.

## Constraints and indexes

- Primary key: `member_id`.
- Logical ownership: `gym_id` must match the selected Gym context.
- Status constraint: only `ACTIVE`, `INACTIVE`, `ARCHIVED`.
- Indexes support normalized phone, name, status, and `created_at_utc` ordering.
- A Member Code uniqueness constraint is scoped to the Gym when the Portal contract requires the code.
- Phone and email have no global uniqueness constraint. A future stronger authoritative contract would take precedence and require an explicit contract update.
- `row_version` is required on update/archive preconditions.
- No password, MFA secret, recovery code, session token, database credential, or private key is stored here.

## Timeline relation

The approved core timeline uses `members.timeline_events` with `timeline_event_id`, `member_id`, `event_type`, `event_at_utc`, `source_type`, `source_id`, `summary`, safe `metadata_json`, and `created_at_utc`, indexed by Member and descending event time. Gym scope is inherited from the selected Gym database and Member relation; the API projection includes the resolved `gymId`. It contains only the four Member-domain events defined in `07_MEMBERS_TIMELINE_CONTRACT.md`.

## Related tables and seed boundary

Memberships, attendance, body measurements, QR tokens, Portal access/session records, and future business tables remain their own contracts. They are not created merely to support the core Member profile. There is no operational Member seed data and Phase 3 library seed identity remains unchanged.

## Delete and concurrency

Archive changes status to `ARCHIVED`, preserves identity/history/audit relationships, and never issues SQL DELETE. Archived Members cannot be changed by normal update. Repeated archive is idempotent. A stale `row_version` returns `409 CONCURRENCY_CONFLICT`.
