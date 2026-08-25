# TOP GYM Flow Inventory

**Audit date:** 2026-08-25  
**Evidence boundary:** actual code and SQL were inspected; no behavior was invented.

## Authentication and shell

1. Browser opens `/` and the login UI.
2. `POST /api/auth/login` establishes the HttpOnly session.
3. `GET /api/auth/session` bootstraps the user and permissions.
4. `page-tabs.js` selects hash-based pages; `feature-loader.js` lazy-loads feature modules.
5. Backend auth and route permissions remain authoritative; frontend filtering is a UX layer.

Evidence: `public/index.html`, `public/js/auth-ui.js`, `public/js/page-tabs.js`, `public/js/feature-loader.js`, `src/middleware/auth.middleware.js`.

## Member lifecycle

`Members list â†’ search/filter â†’ create/edit member â†’ choose membership type/plan â†’ calculate price â†’ optional initial payment â†’ save â†’ details â†’ renew/freeze/resume/payment/delete/print`.

Evidence: `public/js/app.js`, `src/routes/members.routes.js`, `src/services/member-service.js`.

Observed rules include duplicate phone/email checks, membership status calculation, freeze extension, payment ledger entries, receipt numbers, and version/concurrency handling where implemented. Exact financial authority remains backend service/SQL.

## Attendance and QR

`Attendance page â†’ phone/QR input â†’ member eligibility preview â†’ check-in â†’ active visit â†’ auto/manual checkout â†’ daily/report views`.

The service recognizes the observed TOP GYM QR token formats, rejects duplicate same-day check-ins, requires an active membership for check-in, and permits checkout after membership expiry for an existing visit. Day-pass flows are separate.

Evidence: `src/services/attendance-service.js`, `src/routes/attendance.routes.js`, `public/js/pages/attendance/attendance.js`.

## Library

`Library tab â†’ select muscles/foods/exercises â†’ search/filter/pagination â†’ details â†’ create/edit/delete`. Coaching and member portal load catalog data through separate API projections and local media manifests.

Evidence: `src/routes/library.routes.js`, `src/services/library-service.js`, `public/js/pages/library/library.js`, `public/js/member-portal-library.js`.

## Training

`Trainees â†’ open profile â†’ new workout â†’ Step 1 context â†’ Step 2 days/routines/exercises â†’ Step 3 review â†’ POST/PUT â†’ status update/print/PDF â†’ optional workout session/set logs`.

AI flow is `intelligence page or builder action â†’ deterministic suggestion/refine â†’ draft returned to manual builder â†’ human save`. The runtime response is marked draft/requires review, but the backend accepts normal status values; a full approval/publish gate is not present.

Evidence: `public/js/pages/coaching/coaching.js`, `public/js/pages/intelligence/intelligence.js`, `src/services/intelligence-service.js`, `src/services/coaching-service.js`.

## Nutrition

`Trainees/profile â†’ new diet â†’ Step 1 member/targets/calculator â†’ Step 2 3â€“6 meals and foods â†’ live macro totals â†’ Step 3 review â†’ POST/PUT â†’ status/print/PDF â†’ optional meal logs`.

The UI calculates BMR/TDEE and macros locally. The backend stores submitted calculator values and snapshots calories/protein/carbs/fats at item creation. Fiber is present in the food library but is not carried into diet item snapshots, logs, totals, or print output.

Evidence: `public/js/pages/coaching/coaching.js`, `src/services/coaching-service.js`, `src/services/intelligence-service.js`.

## Finance and store

- Finance: `expenses page â†’ list/month filter â†’ create/edit/delete expense â†’ monthly summary/report`.
- Store: `bootstrap â†’ product/category/supplier setup â†’ inventory/purchase â†’ POS cart â†’ payment â†’ sale/receipt â†’ return/stock movement/report`.
- Day passes: `pricing â†’ create/list â†’ WhatsApp opened marker â†’ void/delete/edit according to permission`.

Evidence: `src/routes/finance.routes.js`, `src/routes/store.routes.js`, `src/routes/day-pass.routes.js`, corresponding lazy page modules, `database/migrations/007-store.sql`.

## Member portal

`/member-portal â†’ membership code lookup â†’ report home â†’ membership/payment/attendance/freeze sections â†’ print/PDF-named action â†’ exercise/food library â†’ feedback submission`.

The portal PDF-named button currently invokes the same `window.print()` path as the print button. It does not prove a dedicated PDF file implementation.

Evidence: `public/member-portal.html`, `public/js/member-portal.js`, `public/js/member-portal-library.js`, `src/routes/member-portal.routes.js`.

## Management and recovery

- User management: owner â†’ auth users â†’ create/update/status/delete Assistant.
- Permissions: owner â†’ select Assistant â†’ change permission set â†’ reason â†’ save/audit/session revocation.
- Backup: owner â†’ download/history/archive â†’ inspect â†’ restore; restore is audited in the service.

Evidence: `public/js/pages/management/`, `src/routes/auth.routes.js`, `src/routes/backup.routes.js`, `src/services/permission-service.js`, `src/services/backup-service.js`.

## Missing or unproven flows

- Native Flutter/iOS/Android flow: **BLOCKED: SPECIFICATION GAP** â€” no evidence.
- CRM lead/pipeline/activity/conversion flow: **BLOCKED: SPECIFICATION GAP** â€” no matching route/service evidence.
- Training/nutrition approval and immutable publish flow: **BLOCKED: SPECIFICATION GAP**.
- Offline print/PDF with no external runtime resources: **BLOCKED: SPECIFICATION GAP** until tested.

## Source Consolidation Resolution - 2026-08-25

The missing Flutter and CRM flows are classified as new LogicFit scope. Training/nutrition approval and immutable publish are LogicFit lifecycle decisions, not legacy behavior to infer. The portal PDF-named action is documented as browser print; LogicFit local Print/PDF requirements are recorded separately. Phase 2 remains stopped.

