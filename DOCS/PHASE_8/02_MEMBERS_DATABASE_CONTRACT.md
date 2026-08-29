# Members Database Contract

**Status:** BLOCKED pending lifecycle, uniqueness, and DTO/query decisions
**Database:** selected Gym database only
**Migration authority:** EF Core only

## Core table

The locked Phase 2 table is `members.members`. The following columns are contract evidence, not an implementation instruction until the gap register is closed.

| Column | Contract type | Requiredness / rules | Classification |
|---|---|---|---|
| `member_id` | `uniqueidentifier` | Stable Member identity; primary key | LOCKED |
| `gym_id` | `uniqueidentifier` | Control Plane reference; ownership is mandatory for every Member | IMPLIED; exact DDL nullability must be retained in the closed schema |
| `full_name` | `nvarchar(120)` | Required; server validation | LOCKED |
| `phone` | `nvarchar(30)` | Required; normalized; server validation | LOCKED |
| `email` | `nvarchar(254)` | Optional / nullable | LOCKED |
| `registration_date` | `date` | Present in the catalog; exact requiredness and API date semantics need closure | CONTRACT GAP |
| `notes` | `nvarchar(1000)` | Optional / nullable | LOCKED |
| `status` | Contract status field | Exact type, allowed values, transitions, and archive meaning need closure | CONTRACT GAP |
| audit metadata | Existing audit convention | created/updated actor and UTC timestamps | IMPLIED |
| `row_version` | SQL row-version convention | Mutable-root optimistic concurrency token | LOCKED convention |
| deletion/archive metadata | `deleted_at`/`deleted_by` or explicit archive state | History-preserving delete behavior is required, exact representation needs closure | CONTRACT GAP |

## Keys, references, and indexes

- `member_id` is the sole Member primary key.
- `gym_id` is a logical Control Plane reference; the database-per-Gym boundary means there is no cross-database foreign key.
- The Phase 2 catalog requires indexes supporting normalized phone, name, and status queries.
- The Phase 2 catalog explicitly leaves email uniqueness configurable; no uniqueness rule may be implemented until P8-G-003 is approved.
- The exact composite/index definitions, collation, and normalization storage strategy are part of the implementation-ready schema closure and must not be guessed.

## Related contract-listed tables

The Phase 2 catalog lists memberships, membership events, attendance records, body measurements, timeline events, QR tokens, and portal access/session tables. Those tables are not Member-core implementation permission. Memberships, attendance, measurements, and QR/Portal behavior remain separate contracts. `members.timeline_events` is relevant to the core timeline route, but its event-source scope is unresolved in P8-G-005.

## Timeline table evidence

The catalog describes `members.timeline_events` with:

- `timeline_event_id`;
- `member_id` foreign key;
- `event_type`;
- `event_at_utc`;
- `source_type`;
- `source_id`;
- `summary`;
- `metadata_json`;
- `created_at_utc`.

It also requires a Member/time index and filtering of financial or sensitive data. The approved core event set, projection ownership, payload allowlist, and pagination contract remain open.

## Database invariants

- No Member seed data is allowed.
- Phase 3 library seed identity and counts are unchanged.
- No Member table belongs in the Control Plane.
- No shared operational `tenant_id` database is introduced.
- No cross-Gym Member query is valid.
- No password, MFA secret, recovery code, session token, or database credential is stored in Member data or timeline metadata.
- EF Core is the only migration mechanism.

## Concurrency and deletion

The existing database contract requires row-version protection for mutable roots and history-preserving deletion/archive where history exists. Exact conflict responses, archive/status interaction, uniqueness constraints, and recovery/purge policy are unresolved and tracked in P8-G-001 through P8-G-003.
