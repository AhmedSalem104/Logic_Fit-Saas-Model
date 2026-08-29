# Members Web Contract

**Status:** GREEN — Web contract closed; implementation not authorized by this task
**Client:** existing React application and App Shell

## MEM-W-001 — Member list

| Item | Contract |
|---|---|
| Route | `/gyms/:gymId/members` |
| API | `GET /api/v1/gyms/{gymId}/members` |
| Permission | `members.read`; create action additionally requires `members.create` |
| Actions | Search, status filter, sort, page, open detail, open create, archive when `members.delete` is present |
| Fields | Approved `MemberSummary` only |
| States | Loading, empty, validation/filter error, unauthorized, server error, stale/retry, success |
| UX | Responsive table/cards, Arabic/RTL, Light/Dark, accessible controls |

Archived Members are not shown by default; the explicit status filter is required.

## MEM-W-002 — Member create/edit

| Item | Contract |
|---|---|
| Routes | `/gyms/:gymId/members/new` and `/gyms/:gymId/members/:id/edit` |
| APIs | `POST /api/v1/gyms/{gymId}/members`; `PUT /api/v1/gyms/{gymId}/members/{memberId}` |
| Permissions | Create: `members.create`; edit: `members.update` |
| Fields | `fullName`, `phone`, optional `email`, `registrationDate`, optional `notes`; status only through the closed ACTIVE/INACTIVE lifecycle |
| States | Loading, field validation, server validation, duplicate, concurrency, unauthorized, success, retry |
| Boundary | No membership, payment, attendance, training, nutrition, CRM, or other future-module side effect |

Member Code and Member ID are not normal editable fields. Archive is a separate `members.delete` action.

## MEM-W-003 — Member detail/profile

| Item | Contract |
|---|---|
| Route | `/gyms/:gymId/members/:id` |
| APIs | Member detail GET and Member timeline GET only for Phase 8 core |
| Permission | `members.read`; archive/update actions require their own permissions |
| Content | Core identity/profile, status, Member Code where Portal-contractually required, and Member-domain timeline |
| States | Loading, empty/not found, unauthorized, server error/retry, success |
| UX | Responsive, Arabic/RTL, Light/Dark, privacy-filtered fields |

Memberships, Attendance, Measurements, Training, Nutrition, Finance, Documents, and other tabs shown in older catalogs are future-module areas. No placeholder implementation or fabricated data is created.

## Shared client rules

React calls only the API, never SQL Server. Backend authorization, validation, status transitions, privacy, and Gym isolation are authoritative. Existing routing/theme/RTL foundations are reused; no separate Web application is introduced.
