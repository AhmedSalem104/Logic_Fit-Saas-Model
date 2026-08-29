# Members API Contract

**Status:** GREEN — API contract closed
**Base:** `/api/v1`

## Canonical routes

| Method | Route | Permission | Result |
|---|---|---|---|
| `GET` | `/api/v1/gyms/{gymId}/members` | `members.read` | Paged Member summaries |
| `POST` | `/api/v1/gyms/{gymId}/members` | `members.create` | Created Member |
| `GET` | `/api/v1/gyms/{gymId}/members/{memberId}` | `members.read` | Member detail |
| `PUT` | `/api/v1/gyms/{gymId}/members/{memberId}` | `members.update` | Updated Member |
| `DELETE` | `/api/v1/gyms/{gymId}/members/{memberId}` | `members.delete` | Archived Member |
| `GET` | `/api/v1/gyms/{gymId}/members/{memberId}/timeline` | `members.read` | Paged Member-domain timeline |

The Phase 8 approval explicitly selects `PUT`; affected older Phase 2 `PATCH` references are documentation drift and are reconciled to this route. No compatibility route is permitted.

## Common contract

- Authentication is the Phase 5B session model.
- The server authorizes the requested Gym and permission before data access.
- Success uses the existing `{ data, meta }` envelope. Collection `meta` includes `page`, `pageSize`, `total`, `hasNext`, `requestId`, and `version`.
- Errors use the existing error envelope.
- `401` means unauthenticated; `403` means missing permission or unauthorized Gym; `404` is the canonical safe resource result; `409` is duplicate/idempotency/concurrency conflict; `422` is domain validation; `400` is malformed input/filter; `429` applies only to the existing abuse policy.
- Unknown query/sort fields are rejected; arbitrary SQL-like filters are not accepted.
- No response or log contains passwords, password hashes, MFA/recovery/session secrets, credentials, connection strings, or private keys.

## DTO contract

### `MemberSummary` (list)

```json
{
  "memberId": "uuid",
  "memberCode": "string",
  "fullName": "string",
  "phone": "string",
  "email": "string-or-null",
  "registrationDate": "YYYY-MM-DD",
  "status": "ACTIVE|INACTIVE|ARCHIVED",
  "createdAtUtc": "ISO-8601-UTC",
  "updatedAtUtc": "ISO-8601-UTC",
  "version": "opaque-row-version"
}
```

The list contains only these operational identification fields. The existing Portal contract establishes `memberCode` for Portal-enabled Members; raw Portal access secrets are separate and are never returned.

### `MemberDetail`

`MemberDetail` contains `memberId`, `gymId`, the same approved profile fields, status, `createdAtUtc`, `updatedAtUtc`, and opaque `version`. Actor IDs remain internal audit metadata and are not returned unless a later explicit safe projection contract permits them. It contains no linked membership, payment, attendance, health, training, nutrition, CRM, document, QR, or authentication payload.

### `CreateMemberRequest`

```json
{
  "fullName": "string",
  "phone": "string",
  "email": "string-or-null",
  "registrationDate": "YYYY-MM-DD",
  "notes": "string-or-null"
}
```

No client-supplied `memberId`, `gymId`, status override, database identifier, membership, payment, attendance, or future-module field is accepted. If the already approved Portal contract requires a server-managed Member Code, it is handled by that existing Portal mechanism and is not a second create flow.

### `UpdateMemberRequest`

```json
{
  "fullName": "string",
  "phone": "string",
  "email": "string-or-null",
  "registrationDate": "YYYY-MM-DD",
  "notes": "string-or-null",
  "status": "ACTIVE|INACTIVE"
}
```

The request is a complete replacement of mutable profile fields. `memberId`, `gymId`, creation metadata, Member Code, and an `ARCHIVED` status are immutable through this operation. Status transitions are validated against the lifecycle contract. `If-Match` with the opaque current version is required.

### Archive response

`DELETE` returns the normal envelope with `{ memberId, status: "ARCHIVED", archivedAtUtc, version }`. It requires `If-Match` unless the caller is repeating an already successful archive with the same authorized resource state. Repeated archive is idempotent and never physically deletes.

### Timeline DTO

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

Timeline metadata is allowlisted and never exposes arbitrary JSON.

## List query contract

`GET` accepts:

- `page`: integer, default `1`;
- `pageSize`: integer, default `25`, maximum `100`;
- `search`: one safe term evaluated only against `memberCode`, `firstName`, `lastName`, `displayName`, `phone`, and `email` search projections;
- `status`: one or more of `ACTIVE`, `INACTIVE`, `ARCHIVED`;
- `sort`: `createdAt` or `updatedAt` with `asc|desc`; default `createdAt:desc`.

The default status result is `ACTIVE` and `INACTIVE`; `ARCHIVED` requires an explicit status filter. Sorting always adds `memberId` as a stable descending tie-breaker. Queries are parameterized and Gym-scoped.

The name selectors are API search projections over the canonical `full_name` value; they do not authorize new persisted first-name/last-name columns. Phone/email matching uses the canonical normalized values.

## Idempotency and errors

Create requires the existing `Idempotency-Key` policy. An equivalent repeated key returns the original result; a conflicting payload returns deterministic `409 IDEMPOTENCY_KEY_CONFLICT`. Concurrent Member Code creation yields one success and one deterministic `409 DUPLICATE_RESOURCE`. Stale update/archive returns `409 CONCURRENCY_CONFLICT`. Invalid or unauthorized Gym context never causes an unscoped query.
