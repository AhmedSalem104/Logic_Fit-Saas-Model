# Members Timeline Contract

**Status:** GREEN — Member-domain timeline contract closed

## Route and scope

`GET /api/v1/gyms/{gymId}/members/{memberId}/timeline`

Requires `members.read`, authenticated Phase 5B session, and authorized Gym scope. The route already exists in the Phase 2 API catalog; no second timeline API is created.

## Core events

The Members core timeline contains exactly these event categories:

- `MEMBER_CREATED`
- `MEMBER_UPDATED`
- `MEMBER_ARCHIVED`
- `MEMBER_STATUS_CHANGED`

It does not include payments, subscriptions, attendance, training, nutrition, store, CRM, classes, or other future-module events. Future modules may contribute events only through their own approved contract updates.

## Timeline DTO

```json
{
  "eventId": "uuid",
  "memberId": "uuid",
  "gymId": "uuid",
  "eventType": "MEMBER_CREATED|MEMBER_UPDATED|MEMBER_ARCHIVED|MEMBER_STATUS_CHANGED",
  "occurredAt": "ISO-8601-UTC",
  "actorId": "uuid-or-null",
  "metadata": "approved-safe-object"
}
```

The event table follows the existing `members.timeline_events` evidence. `sourceType`/`sourceId` identify the Member-domain source event where applicable; they are not a route to arbitrary future data.

## Query and ordering

Timeline uses the existing collection envelope and pagination: `page` default 1, `pageSize` default 25, maximum 100. Results are ordered by `occurredAt` descending, then `eventId` descending as a deterministic tie-breaker. No arbitrary filters or sort fields are accepted.

## Safe metadata

Only approved safe metadata is returned, such as a changed-field name or status transition. Raw request bodies, notes, passwords, MFA secrets, recovery codes, session tokens, authentication secrets, database credentials, and arbitrary JSON are excluded. The timeline is always Member- and Gym-scoped.
