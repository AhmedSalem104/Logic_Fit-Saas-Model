# Members TOP GYM Mapping

**Status:** Reference mapping only; TOP GYM remains read-only

TOP GYM evidence was compared with the LogicFit contracts. TOP GYM is a legacy Node/Express, single-database application and is not an architectural or contract authority.

| TOP GYM evidence | LogicFit contract | Reusable behavior | Required redesign / disposition |
|---|---|---|---|
| `fullName`, phone, optional email, registration date, notes | `members.members` core fields | Field intent and validation limits | Implement only after nullability/normalization DTO closure |
| Member list search/status/sort/page | `GET /api/v1/gyms/{gymId}/members` | General list UX concept | Add mandatory API Gym scope and approved query allowlist; exact schema unresolved |
| Legacy `/api/members` | `/api/v1/gyms/{gymId}/members` | None at route level | Do not copy unscoped route |
| Legacy create can create membership/payment | Members create core | None | Explicitly excluded; no package/payment side effects |
| Legacy update/delete | PATCH/DELETE canonical routes | Update/delete intent | Replace legacy semantics with closed row-version and archive lifecycle |
| Legacy one-database `members` table | Gym DB `members.members` | Core profile concept | Redesign for database-per-Gym and server scope; do not copy schema blindly |
| Legacy duplicate phone/email checks | Phase 2 says duplicate policy is not invented; email uniqueness configurable | Risk to consider | Product/security decision required; no automatic rule copied |
| Legacy details with membership/payments/training/attendance | MEM-W-003 profile contract | Profile navigation concept | Future-domain tabs conflict with locked core scope; P8-G-006 |
| Legacy attendance route | `F-MEM-003` separate Attendance | Separate navigation boundary | No Attendance implementation in Phase 8 Members core |
| Legacy membership code Portal | Phase2 `Member Code -> Gym context -> scoped Portal session` | Separation concept | Preserve Portal auth; exact code policy is future dependency |
| Legacy Owner-only code reveal/rotate | Portal security evidence | Security concern | Not a Members core API; do not expose codes |
| Legacy permissions | Existing LogicFit five `members.*` identifiers | Names are consistent | Do not copy legacy role grants; current LogicFit role mapping needs closure |
| Legacy QR behavior | LogicFit opaque QR privacy contract | None needed for core | QR remains separate; no Member ID/sensitive data in QR |

## Result

TOP GYM supplies field and legacy-flow evidence only. Tenant isolation, API routes, database placement, RBAC grants, timeline sources, and future-module boundaries come from LogicFit authority or remain explicit blockers.
