# Members Timeline Contract

**Status:** BLOCKED — event source and core event scope are not closed

## Locked storage evidence

The Phase 2 database catalog describes `members.timeline_events` with:

- `timeline_event_id`;
- `member_id`;
- `event_type`;
- `event_at_utc`;
- `source_type`;
- `source_id`;
- `summary`;
- `metadata_json`;
- `created_at_utc`.

It also requires Member/time indexing, authorization, and filtering of financial or sensitive data. The API catalog defines a Gym-scoped read route:

`GET /api/v1/gyms/{gymId}/members/{memberId}/timeline`

with `members.read`.

## Unresolved event boundary

The Phase 2 Member contract describes a future projection that may include membership, attendance, measurement, CRM, training/nutrition, and payment events. The Phase 8 locked initial scope excludes those modules. No implementation may silently import their records or event types.

Human approval is required for:

- the event types available in the Members core slice;
- whether Member create/update/archive events are included;
- the source of each event and projection ownership;
- whether the initial timeline is Member-core-only until future modules exist;
- the safe metadata allowlist and whether actor identity is shown;
- event ordering and tie-breaking;
- time/status filters, page size, and response envelope fields;
- behavior for a missing or inaccessible source event.

## Security and privacy invariants

- The timeline is always scoped to the requested Gym and Member.
- Financial, health, authentication, and other sensitive records are excluded unless an explicit authorized contract later permits them.
- No password, MFA secret, recovery code, session token, database credential, or private key may be stored in or returned through timeline metadata.
- The API returns only an approved safe projection, never arbitrary `metadata_json`.
