# Members Validation Contract

**Status:** GREEN — validation contract closed

## Member profile fields

| Field | Rule |
|---|---|
| `fullName` | Required string; maximum 120 characters |
| `phone` | Required string; server-normalized; maximum 30 characters; the existing TOP GYM-derived minimum of 5 characters applies |
| `email` | Optional nullable string; server-normalized; maximum 254 characters |
| `registrationDate` | Required `YYYY-MM-DD` date; stored as SQL `date` |
| `notes` | Optional nullable string; maximum 1000 characters |
| `status` | `ACTIVE`, `INACTIVE`, or `ARCHIVED`; create defaults to `ACTIVE`; normal update accepts only `ACTIVE` or `INACTIVE` |
| `memberId` | Server-generated UUID; never accepted from a client |
| `gymId` | Route scope only; never accepted as a body ownership override |
| `memberCode` | Existing Portal contract value when required; Gym-unique, protected, and not a client-selected database identifier |

## Normalization

Phone and email are normalized by the backend before comparison and persistence according to the existing LogicFit/TOP GYM field contract. No global uniqueness is applied. Name search uses the canonical `full_name` search projection; `firstName`, `lastName`, and `displayName` are approved API search selectors and do not add persisted columns.

## Create/update/archive validation

- Create accepts only `fullName`, `phone`, `email`, `registrationDate`, and `notes`.
- Create cannot select another Gym, supply a Member ID, force an inactive/archived status, or create membership/payment/attendance/future-module records.
- PUT accepts only mutable profile fields and `status` values permitted by the lifecycle contract.
- `memberId`, Gym ownership, creation metadata, Member Code, and `ARCHIVED` are immutable through PUT.
- DELETE has no business payload; it archives the scoped Member and preserves history.
- All mutations validate the current Gym, permission, and row version server-side.

## Duplicate and idempotency validation

- Member Code, when required by the Portal contract, is unique within the Gym and is enforced by a database constraint.
- Phone and email are not globally unique; multiple Members may share either value unless a stronger future authoritative contract explicitly changes this rule.
- Create uses the existing idempotency-key policy. Equivalent replay returns the original result; a conflicting payload for the same key returns `409 DUPLICATE_RESOURCE`.
- Concurrent Member Code creation yields one success and one deterministic `409 DUPLICATE_RESOURCE`.

## Query validation

List accepts only `page`, `pageSize`, `search`, `status`, and the approved `sort` fields. `page` defaults to 1; `pageSize` defaults to 25 and is capped at 100. Status values are case-sensitive canonical values. Unknown parameters, fields, or sort directions return `400 INVALID_FILTER`.

## Error contract

The existing LogicFit error envelope is used: `400 VALIDATION_ERROR`/`INVALID_FILTER`/`INVALID_STATE_TRANSITION`, `401 AUTHENTICATION_REQUIRED` or `SESSION_INVALID`, `403 PERMISSION_DENIED` or `GYM_SCOPE_DENIED`, `404 RESOURCE_NOT_FOUND` for an absent resource within authorized scope, `409 DUPLICATE_RESOURCE` or `CONCURRENCY_CONFLICT`, and `422 DOMAIN_RULE_VIOLATION` where a domain rule is distinct from field validation.
