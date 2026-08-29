# Members API Contract

**Status:** BLOCKED — route family is known, implementation-ready schemas are not yet closed
**Base:** `/api/v1`

## Canonical operation set

These routes are the only Phase 8 Members core routes currently authorized by the Phase 2 catalog:

| Method | Canonical route | Permission | Operation |
|---|---|---|---|
| `GET` | `/api/v1/gyms/{gymId}/members` | `members.read` | List/search/filter Members |
| `POST` | `/api/v1/gyms/{gymId}/members` | `members.create` | Create a Member |
| `GET` | `/api/v1/gyms/{gymId}/members/{memberId}` | `members.read` | Read Member detail |
| `PATCH` | `/api/v1/gyms/{gymId}/members/{memberId}` | `members.update` | Update a Member |
| `DELETE` | `/api/v1/gyms/{gymId}/members/{memberId}` | `members.delete` | Delete/archive according to the closed lifecycle contract |
| `GET` | `/api/v1/gyms/{gymId}/members/{memberId}/timeline` | `members.read` | Read the authorized Member timeline |

No export route is present in the locked API catalog. `members.export` is retained as an approved permission identifier, but export is deferred unless a separate operation contract is approved.

## Common API rules already locked

- The API is authoritative and clients use it exclusively.
- `gymId` is resolved and authorized server-side before data access.
- Responses use the existing `{ data, meta }` success envelope.
- Errors use the existing error envelope with `code`, `message`, `fieldErrors`, and request metadata.
- Standard status families are 400 validation/filter/state, 401 unauthenticated, 403 unauthorized or scope denied, 404 safe not found, 409 concurrency/duplicate, 422 domain validation, and 429 rate limiting where applicable.
- Collection paging supports `page >= 1` and `pageSize` up to 100; the endpoint-specific default and complete query allowlist are not closed.
- Mutation authorization and validation are re-evaluated by the backend in the transaction.
- Mutable updates use the existing opaque row-version/`If-Match` convention once the exact representation is confirmed.

## Known request/response field set

The locked profile fields are:

```text
fullName: required string, max 120
phone: required normalized string, max 30
email: optional string, max 254
registrationDate: date
notes: optional string, max 1000
status: canonical Member status — unresolved
memberId / gymId / audit / version metadata: response-controlled
```

This is not a complete JSON schema. The exact casing, nullability, date format, status vocabulary, list/detail field allowlists, version field, and error field names must be approved before implementation.

## Endpoint-specific closure items

### List

The route, permission, Gym scope, and paged collection concept are locked. The default page size, searchable fields, normalization rules, exact status filter, sortable fields/directions, list privacy allowlist, and response metadata remain unresolved.

### Create

The route, `members.create`, exact core form fields, server validation, Gym ownership, audit, and no automatic membership/payment/attendance creation are locked. Duplicate handling, idempotency-key requirement/fingerprint behavior, generated/stable Member Code behavior, response status, and complete response schema remain unresolved.

### Detail

The route and `members.read` are locked. The core detail allowlist, timeline link/shape, future-domain tab behavior, privacy filtering, and not-found/scope disclosure policy remain unresolved.

### Update

The route, `members.update`, Gym ownership protection, row-version concept, validation, and audit are locked. Mutable versus immutable fields, exact `If-Match`/version contract, conflict response, and status mutation rules remain unresolved.

### Delete/archive

The route and `members.delete` are locked. The meaning of delete, resulting status/visibility, audit payload, recovery, and physical-purge policy remain unresolved. Phase 2 history-preservation rules rule out an unqualified destructive delete.

### Timeline

The route, `members.read`, Gym scope, time-ordered projection concept, and sensitive-data filtering are locked. Event types, source ownership, metadata allowlist, ordering tie-breaker, filters, page size, and future-domain inclusion remain unresolved.

## Security and privacy

No request or response may carry passwords, reset secrets, TOTP secrets, recovery codes, session secrets, database credentials, or private keys. Platform Admin does not gain implicit Member API access. Client-side permission checks are UX only; backend authorization is authoritative.
