# Members Validation Contract

**Status:** BLOCKED — field limits are known, but normalization, duplicate, and status rules are incomplete
**Scope:** Contract only

## Locked profile validation

| Field | Locked rule | Remaining closure |
|---|---|---|
| `fullName` | Required; maximum 120 characters | Exact whitespace/Unicode normalization and allowed character policy |
| `phone` | Required; normalized; maximum 30 characters; TOP GYM reference observes a service minimum of 5 | Canonical normalization algorithm, accepted formats, and duplicate policy |
| `email` | Optional; maximum 254 characters; normalized by the server | Case/Unicode normalization and duplicate policy |
| `registrationDate` | Date value; no time-of-day in the database contract | Requiredness, accepted range, future-date rule, and API serialization |
| `notes` | Optional; maximum 1000 characters | Exact whitespace and privacy/logging treatment |
| `status` | Contract field | Canonical values, default, transitions, and validation are unresolved |

No additional personal, health, membership, payment, attendance, training, nutrition, CRM, or document fields may be added to the core create/update contract.

## Server-side rules

- Validation executes in the API and is repeated at the persistence boundary.
- The route Gym is the only valid ownership context for a new Member.
- A normal update cannot change `gymId` or move a Member between Gyms.
- The API uses the existing LogicFit validation/error envelope.
- Unknown filter, sort, or search fields are rejected according to the common API contract; arbitrary SQL-like filters are not allowed.
- Passwords, session values, MFA values, recovery codes, and other authentication secrets are not Member fields.

## Unresolved validation decisions

P8-G-003 must define whether normalized phone and/or email are unique within a Gym, whether duplicates are allowed with warnings, and how concurrent duplicate creation is reported. P8-G-004 must define the query allowlist and serialized request/response schema. P8-G-001 must define valid status values and transitions.

## Error mapping to preserve

The existing API contract provides:

- `400` for malformed requests, invalid fields, and invalid filter/state syntax;
- `401` for missing or invalid authentication;
- `403` for missing permission or unauthorized Gym scope;
- `404` for the approved safe not-found behavior;
- `409` for duplicate or optimistic-concurrency conflicts once the exact condition is closed;
- `422` for domain validation where distinct from structural validation;
- `429` only where an approved abuse/rate-limit policy applies.

No second validation or error format may be introduced.
