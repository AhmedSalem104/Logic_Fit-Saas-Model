# TOP GYM Permission Mapping

**Audit date:** 2026-08-25  
**Status:** YELLOW â€” route-level legacy permissions captured; LogicFit RBAC remains separate.

## Roles

The inspected implementation defines `Owner` and `Assistant`. Owner is treated as full access by the permission middleware. Assistant access is resolved against granted permissions. No Trainer/Coach role was found.

Evidence: `src/permissions/roles.js`, `src/permissions/role-permissions.js`, `src/middleware/permission.middleware.js`.

## Permission families observed

```text
members.read/create/update/delete
memberships.read/create/freeze/renew
payments.create
attendance.read/check_in/check_out
trainees.read/create
coaching.read/create/update/delete
library.read/create/update/delete
finance.read/write
expenses.read/create/update/delete
reports.read/export
pricing.read/update
day_passes.read/create/update/delete/void
feedback.read
permissions.manage
management.users
management.backup.create/download/inspect/restore
intelligence.read/generate
store.dashboard.view
store.products.read/create/update/delete
store.inventory.read/adjust
store.purchases.read/create
store.sales.read/create/return
store.expenses.read/create/update/delete
store.profit.view
```

The exact catalog is defined by `src/permissions/role-permissions.js` and `src/permissions/route-permissions.js`; the above is the audited domain grouping, not a new permission contract.

## Route-family mapping

| Domain/action | Observed permission behavior |
|---|---|
| Members GET | `members.read`; membership details may also require membership read. |
| Member create | member/membership/payment permissions according to submitted fields. |
| Member update/delete | update/delete permission; payment fields add payment permission. |
| Freeze/resume/renew/add membership/payment | membership/payment combinations resolved by route policy. |
| Attendance read/check-in/out | attendance read/check-in/check-out families. |
| Coaching GET | `coaching.read`. |
| Coaching POST | `coaching.create`. |
| Coaching PUT/status | `coaching.update`. |
| Coaching DELETE | `coaching.delete`. |
| Library GET/POST/PUT/DELETE | `library.read/create/update/delete`. |
| Intelligence overview/query/churn | intelligence read. |
| Intelligence suggestions/refine | intelligence generate. |
| Backup and user administration | Owner-only/management permissions; restore is high risk. |
| Member portal lookup/feedback | Public code-based lookup/feedback routes, subject to their own validation/rate rules. |

Evidence: `src/permissions/route-permissions.js`, route modules, `src/middleware/auth.middleware.js`.

## Security observations

- API authentication is applied through the `/api` middleware composition.
- Backend permission middleware is the security boundary; frontend hides/disables controls only as UX.
- Financial response filtering exists in `financial-data.middleware.js`.
- Owner-only high-risk operations include permission management, backup/restore, and portal-code reveal/rotation.
- Permission changes record reason/audit and can revoke Assistant sessions.

## Gaps and unresolved behavior

1. **BLOCKED: SPECIFICATION GAP** â€” no tenant/gym scope predicate exists in TOP GYM custom queries.
2. **BLOCKED: SPECIFICATION GAP** â€” no separate approval/publish permissions for training/nutrition.
3. **BLOCKED: SPECIFICATION GAP** â€” `/qr/:id` privacy/auth boundary requires an approved contract.
4. **BLOCKED: SPECIFICATION GAP** â€” no mobile permission surface exists in legacy evidence.
5. The technical specification contains stale/auth-conflicting statements; current code and current route policy were used for this map.

LogicFit must not copy the role/permission set until it is reconciled with the Master Bibleâ€™s platform/gym roles, tenant isolation, resource/action/scope model, and high-risk audit requirements.

## Source Consolidation Resolution - 2026-08-25

TOP GYM lack of tenant predicates and separate approval/publish permissions is classified as a LogicFit architecture/security requirement, not a missing legacy field to guess. The Approval transition and exact permission matrix are approved in `LOGICFIT_APPROVAL_PERMISSION_MATRIX.md` (SC-016). The legacy `/qr/:id` privacy boundary is replaced for LogicFit by the approved opaque-token contract in `LOGICFIT_PUBLIC_QR_PRIVACY_CONTRACT.md` (SC-017); no TOP GYM route is modified.

