# Members Web Contract

**Status:** BLOCKED — core profile surface and API schemas require closure
**Client:** existing React application, existing App Shell and design system

## MEM-W-001 — Member list

| Item | Contract |
|---|---|
| Route | `/gyms/:gymId/members` |
| Purpose | Show an authorized Gym's Member collection |
| API | `GET /api/v1/gyms/{gymId}/members` |
| Permission | `members.read`; create action additionally requires `members.create` |
| Actions | Search/filter/sort/page, open detail, start create when permitted |
| States | Loading, empty, validation/filter error, unauthorized, server error, stale/retry, success |
| Layout | Responsive table/cards, Arabic/RTL, Light/Dark themes |

The exact query allowlist and list field allowlist are unresolved. Export/print is not a Phase 8 feature merely because a legacy screen or permission exists.

## MEM-W-002 — Create/edit Member

| Item | Contract |
|---|---|
| Routes | `/gyms/:gymId/members/new` and `/gyms/:gymId/members/:id/edit` |
| Purpose | Create or update the core Member profile |
| APIs | `POST /api/v1/gyms/{gymId}/members`; `PATCH /api/v1/gyms/{gymId}/members/{memberId}` |
| Permissions | Create: `members.create`; edit: `members.update` |
| Fields | `fullName`, `phone`, optional `email`, `registrationDate`, optional `notes`; status/identity fields only if the closed contract permits them |
| States | Loading, field validation, server validation, duplicate/conflict, unauthorized, success, stale/retry |
| Requirements | No membership/package/payment/attendance/training/nutrition data is created from this form |

The immutable/mutable field set, version submission, duplicate behavior, and status controls require closure.

## MEM-W-003 — Member detail

| Item | Contract |
|---|---|
| Route | `/gyms/:gymId/members/:id` |
| Purpose | Show authorized Member core detail and approved timeline |
| APIs | Member detail GET; timeline GET; future-domain APIs only in their own approved slices |
| Permission | Core detail/timeline: `members.read`; action buttons use their own permissions |
| States | Loading, empty/not found, unauthorized, server error/retry, success |
| Layout | Responsive, Arabic/RTL, Light/Dark themes, privacy-filtered fields |

Phase 2 lists membership, attendance, measurements, training, nutrition, payments, documents, and timeline tabs. The Phase 8 locked core scope excludes those business modules, creating P8-G-006. No placeholder or unauthorized tab behavior may be chosen before that boundary is approved.

## Shared client rules

- React calls only the LogicFit API.
- Backend authorization and validation are authoritative.
- Query state must not be treated as a security decision.
- No SQL, connection string, secret, or direct database access is allowed in the Web client.
- All screens must support the existing Arabic/RTL, Light/Dark, accessible, and responsive foundation.
