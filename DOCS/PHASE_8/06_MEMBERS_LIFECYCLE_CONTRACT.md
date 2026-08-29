# Members Lifecycle and Concurrency Contract

**Status:** GREEN — lifecycle and concurrency contract closed

## Canonical statuses

| Status | Meaning | Normal visibility |
|---|---|---|
| `ACTIVE` | Normal operational Member | Included by default |
| `INACTIVE` | Retained in the Gym database but not an active operational Member | Included by default; can be explicitly filtered |
| `ARCHIVED` | Historical/audit-retained Member | Excluded by default; requires explicit filter |

## Transitions

```text
Create -> ACTIVE
ACTIVE <-> INACTIVE       (PUT, members.update)
ACTIVE -> ARCHIVED        (DELETE, members.delete)
INACTIVE -> ARCHIVED      (DELETE, members.delete)
ARCHIVED -> no normal Phase 8 transition
```

Create defaults to `ACTIVE` because the create DTO has no status override and `ACTIVE` is the normal operational state. `ARCHIVED` cannot be changed through normal PUT. No restore/reactivation API is created in Phase 8. A future restore operation requires a new approved contract.

## Archive semantics

`DELETE /api/v1/gyms/{gymId}/members/{memberId}` means archive only. It sets the canonical archive/status metadata, preserves Member identity, history, audit, and future relationships, and never issues SQL DELETE. It is high-risk and requires `members.delete`.

Repeated archive is idempotent: an already archived, authorized Member remains archived and no duplicate record or destructive action occurs. An archived Member remains readable when explicitly requested and remains excluded from ordinary active operations.

## Concurrency

- `memberId` is a server-generated immutable UUID.
- Member Code uniqueness, when applicable, is enforced within the Gym at the database boundary.
- Phone and email have no global uniqueness constraint.
- PUT and DELETE require the existing opaque `If-Match` row version for a current mutable resource.
- A stale version returns `409 CONCURRENCY_CONFLICT` and never overwrites newer data.
- A duplicate Member Code race returns exactly one success and one `409 DUPLICATE_RESOURCE`.
- No distributed lock or second concurrency system is introduced.

## Audit

Create emits `MEMBER_CREATED`; accepted profile changes emit `MEMBER_UPDATED`; ACTIVE/INACTIVE transitions emit `MEMBER_STATUS_CHANGED`; archive emits `MEMBER_ARCHIVED`. Audit uses the existing server audit system and contains only safe identifiers and approved changed-field metadata.
