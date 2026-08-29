# Members TOP GYM Mapping

**Status:** GREEN — reference mapping complete; TOP GYM remains read-only

TOP GYM is a legacy Node/Express, single-database application. It supplies source evidence only and does not override LogicFit architecture or contracts.

| TOP GYM evidence | LogicFit Members contract | Classification |
|---|---|---|
| `fullName`, phone, optional email, registration date, notes | `members.members` approved core fields and limits | Canonical field evidence |
| List search/status/sort/page | Gym-scoped list with page 25/default, 100/max, safe search/status/sort | Reusable UX, LogicFit API redesign |
| `/api/members` and unscoped legacy controllers | `/api/v1/gyms/{gymId}/members` | Legacy defect; never copy |
| Create coupled to membership/payment | Core create only | Explicitly out of scope |
| Legacy update/delete | PUT update and DELETE archive with row version | LogicFit redesign |
| One shared `members` table | selected Gym database `members.members` | Architecture redesign |
| Duplicate phone/email checks | phone/email not globally unique; Member Code Gym-unique | Resolved LogicFit rule |
| Profile includes membership/payment/training/attendance | Core profile and Member timeline only | Future-module leakage; excluded |
| Attendance route | `F-MEM-003` separate Attendance scope | Out of scope |
| Membership-code Portal | Existing Member Code -> scoped Portal session | Canonical relationship; Portal remains separate |
| Legacy Owner-only code reveal/rotate | No raw Portal secret in core DTOs | Legacy behavior not copied |
| Legacy permissions/roles | Existing LogicFit five permissions and approved role grants | LogicFit RBAC authority |
| Legacy QR behavior | Separate opaque QR privacy contract | Separate contract |

## Reuse rule

Only field intent, safe list/profile UX concepts, and legacy validation evidence may inform implementation. Tenant isolation, API routes, database placement, authorization, timeline scope, and future-module boundaries are LogicFit rules.
