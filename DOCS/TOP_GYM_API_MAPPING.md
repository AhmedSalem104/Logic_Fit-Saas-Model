# TOP GYM API Mapping

**Audit date:** 2026-08-25  
**Evidence:** `src/routes/*.routes.js`, controllers, services, `src/permissions/route-permissions.js`, `docs/API.md`.

All listed API paths are legacy TOP GYM paths. They are not approved LogicFit contracts.

## Health and authentication

```text
GET  /api/health
GET  /api/auth/session
POST /api/auth/login
POST /api/auth/logout
GET  /api/auth/users
POST /api/auth/users
PUT  /api/auth/users/:id
PATCH /api/auth/users/:id/status
DELETE /api/auth/users/:id
GET  /api/auth/permissions/catalog
GET  /api/auth/users/:id/permissions
PUT  /api/auth/users/:id/permissions
POST /api/auth/users/:id/permissions/reset
```

## Members, memberships, payments, attendance

```text
GET    /api/members
GET    /api/members/:id
GET    /api/members/:id/details
POST   /api/members
PUT    /api/members/:id
DELETE /api/members/:id
POST   /api/members/:id/alert-communications
POST   /api/members/:id/freeze
POST   /api/members/:id/resume
POST   /api/members/:id/renew
POST   /api/members/:id/memberships
POST   /api/memberships/:id/payments

GET  /api/attendance
GET  /api/attendance/report
GET  /api/attendance/member/:id
POST /api/attendance/check-in
POST /api/attendance/check-out
```

## Finance, pricing, day passes, reports

```text
GET    /api/monthly-finance
POST   /api/expenses
PUT    /api/expenses/:id
DELETE /api/expenses/:id

GET  /api/pricing
PUT  /api/pricing
PUT  /api/pricing/:planCode
POST /api/pricing-plans
PUT  /api/pricing-plans/:planCode
POST /api/membership-types
PUT  /api/membership-types/:typeCode

GET    /api/day-passes
GET    /api/day-passes/pricing
PUT    /api/day-passes/pricing
GET    /api/day-passes/summary
POST   /api/day-passes
PUT    /api/day-passes/:id
DELETE /api/day-passes/:id
POST   /api/day-passes/:id/whatsapp-opened
POST   /api/day-passes/:id/void

GET /api/reports
```

## Library and coaching

```text
GET    /api/library/options
GET    /api/library/:type
GET    /api/library/:type/:id
POST   /api/library/:type
PUT    /api/library/:type/:id
DELETE /api/library/:type/:id

GET  /api/external-trainees
POST /api/external-trainees
GET  /api/coaching/clients
GET  /api/coaching/catalog
GET  /api/clients/:id/coaching-summary
GET  /api/clients/:id/training-overview
PUT  /api/clients/:id
GET  /api/clients/:id/measurements
POST /api/clients/:id/measurements
PUT  /api/clients/:id/measurements/:measurementId
DELETE /api/clients/:id/measurements/:measurementId
GET  /api/clients/:id/checkins
POST /api/clients/:id/checkins
PUT  /api/clients/:id/checkins/:checkinId
DELETE /api/clients/:id/checkins/:checkinId

GET    /api/workoutprograms[/:id]
POST   /api/workoutprograms
PUT    /api/workoutprograms/:id
PATCH  /api/workoutprograms/:id/status
DELETE /api/workoutprograms/:id

GET    /api/workout-programs[/:id]
POST   /api/workout-programs
PUT    /api/workout-programs/:id
PATCH  /api/workout-programs/:id/status
DELETE /api/workout-programs/:id

GET    /api/dietplans[/:id]
POST   /api/dietplans
PUT    /api/dietplans/:id
PATCH  /api/dietplans/:id/status
DELETE /api/dietplans/:id

GET    /api/diet-plans[/:id]
POST   /api/diet-plans
PUT    /api/diet-plans/:id
PATCH  /api/diet-plans/:id/status
DELETE /api/diet-plans/:id

POST /api/workoutsessions/start
GET  /api/workoutsessions
GET  /api/workoutsessions/:id
POST /api/workoutsessions/:id/sets
POST /api/workoutsessions/:id/end
POST /api/meal-logs
GET  /api/meal-logs
```

## Intelligence

```text
GET  /api/intelligence/overview
POST /api/intelligence/query
GET  /api/intelligence/churn
POST /api/intelligence/workout-suggestions
POST /api/intelligence/diet-suggestions
POST /api/intelligence/refine
```

The implementation is deterministic/local heuristic logic. It does not call a paid or external AI provider in the inspected package.

## Store/POS/inventory

```text
GET  /api/store/bootstrap
GET  /api/store/dashboard
GET  /api/store/reports
GET  /api/store/categories
POST /api/store/categories
PUT  /api/store/categories/:id
GET  /api/store/products
GET  /api/store/products/:id
POST /api/store/products
PUT  /api/store/products/:id
DELETE /api/store/products/:id
POST /api/store/products/:productId/variants
PUT  /api/store/products/:productId/variants/:variantId
DELETE /api/store/products/:productId/variants/:variantId
GET  /api/store/suppliers
POST /api/store/suppliers
PUT  /api/store/suppliers/:id
GET  /api/store/inventory
GET  /api/store/inventory/movements
POST /api/store/inventory/adjustments
GET  /api/store/customers/search
GET  /api/store/purchases
GET  /api/store/purchases/:id
POST /api/store/purchases
GET  /api/store/sales
GET  /api/store/sales/:id
POST /api/store/sales
POST /api/store/sales/:id/returns
GET  /api/store/expenses
POST /api/store/expenses
PUT  /api/store/expenses/:id
DELETE /api/store/expenses/:id
GET  /api/members/:id/store-purchases
```

## Portal, feedback, backup

```text
POST /api/member-portal/lookup
GET  /api/member-portal/library/options
GET  /api/member-portal/library/:type
GET  /api/member-portal/library/:type/:id
GET  /api/members/:id/membership-code
POST /api/members/:id/membership-code/reveal
POST /api/members/:id/membership-code/resend
POST /api/members/:id/membership-code/rotate
POST /api/member-portal/feedback
GET  /api/member-feedback

GET    /api/backup/daily
GET    /api/backup/download
GET    /api/backup/history
GET    /api/backup/archives/:id
DELETE /api/backup/archives/:id
POST   /api/backup/inspect
POST   /api/backup/restore
```

## Response and trace notes

Controllers map legacy envelopes such as `{members}`, `{measurements}`, `{checkins}`, `{programs}`, `{plans}`, `{sessions}`, and `{mealLogs}`. Coaching and member responses are assembled by services and SQL queries. The frontend calls the API directly through `fetch`/the shared API helper; no React Query, Dio, or OpenAPI-generated client exists in TOP GYM.

Authorization mapping is in `TOP_GYM_PERMISSION_MAPPING.md`. Resource ownership and tenant isolation are legacy single-gym behavior and must not be assumed for LogicFit.
