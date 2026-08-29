# Phase 8 Members Gap and Conflict Register

**Gate status:** GREEN — all six approved blockers resolved

| ID | Gap/conflict | Resolution | Classification | Status |
|---|---|---|---|---|
| P8-G-001 | Member status and DELETE semantics | `ACTIVE`, `INACTIVE`, `ARCHIVED`; DELETE archives only; no hard delete or restore route | Resolved from explicit human decision | CLOSED |
| P8-G-002 | Concrete grants for five Members permissions | `gym-security-admin`: all five; `gym-authenticated-user`: read; `platform-security-admin`: none | Resolved security decision | CLOSED |
| P8-G-003 | Duplicate/uniqueness/idempotency/concurrency rules | Gym-scoped Member Code uniqueness when applicable; no global phone/email uniqueness; idempotent create/archive; row-version conflicts | Resolved product/security decision | CLOSED |
| P8-G-004 | DTOs, query model, privacy fields, and update verb | Complete schemas/query rules in API contract; PUT is canonical and older PATCH references reconciled | Resolved contract decision/documentation drift | CLOSED |
| P8-G-005 | Timeline event scope and projection behavior | Four Member-domain events, safe metadata, descending time/event ordering, page 25/max 100 | Resolved product/privacy decision | CLOSED |
| P8-G-006 | Future-domain profile tabs versus Members core | Core profile/timeline only; future tabs are out of scope without placeholders | Resolved product/UI boundary | CLOSED |

## Deferred, not blocking

- `members.export`: contracted permission, implementation deferred; no endpoint exists.
- Portal Member Code generation/rotation details: governed by the existing Portal contract and not changed by Members core.
- `F-MEM-003`: Attendance remains a separate scope.
- Membership packages, payments, subscriptions, and all other business modules remain later contracts.
- Root governance files absent: no content reconstructed.

## Closure result

No open Phase 8 contract gap remains for the approved Members core. Implementation is still prohibited by the current user instruction; this package only closes the specification.
