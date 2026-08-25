# TOP GYM Audit Evidence Index

**Audit date:** 2026-08-25  
**Legacy source:** `C:\Users\B-SMART\gym-membership-app`  
**Boundary:** read-only; TOP GYM was not modified.

## Evidence hierarchy used

1. Actual TOP GYM implementation: `src/`, `public/`, `database/`, `data/`, `server.js`.
2. TOP GYM implementation documentation that describes the current code: `docs/`.
3. LogicFit Master Bible, Decision Lock, and approved DOCS for the target-product constraints.
4. Agent handoff evidence, reviewed by the Lead.

TOP GYM documentation is evidence of intent and mapping, not authority to change the LogicFit contract.

## Core evidence map

| Area | Primary evidence |
|---|---|
| Composition and server | `server.js`, `src/app.js`, `src/routes/index.js`, `docs/ARCHITECTURE.md` |
| Database | `database/schema.sql`, `database/migrations/005-member-feedback.sql`, `006-permissions.sql`, `007-store.sql`, `src/database/`, `docs/DATABASE.md` |
| Authentication/RBAC | `src/middleware/`, `src/permissions/`, `src/services/auth-service.js`, `src/services/permission-service.js`, `tests/unit/permissions.test.js`, `docs/AUTH.md`, `docs/PERMISSIONS.md` |
| Members and membership lifecycle | `src/routes/members.routes.js`, `src/controllers/members.controller.js`, `src/services/member-service.js`, `public/js/app.js` |
| Attendance | `src/routes/attendance.routes.js`, `src/controllers/attendance.controller.js`, `src/services/attendance-service.js`, `public/js/pages/attendance/attendance.js` |
| Training and nutrition | `src/routes/coaching.routes.js`, `src/controllers/coaching.controller.js`, `src/services/coaching-service.js`, `src/services/intelligence-service.js`, `public/js/pages/coaching/coaching.js` |
| Library and canonical data | `data/library/*.json`, `src/services/library-service.js`, `src/routes/library.routes.js`, `public/data/*.json`, `scripts/validate-*.js` |
| Screens and navigation | `public/index.html`, `public/member-portal.html`, `public/js/feature-loader.js`, `public/js/page-tabs.js`, `docs/SCREENS-SUMMARY.md` |
| Print/PDF | `public/js/integrations/print-enhancements.js`, `public/css/print.css`, `public/js/member-portal.js`, `public/js/pages/store/store.js`, `docs/MEMBER-PORTAL.md`, `docs/EXERCISE-ASSETS.md` |
| Media | `public/assets/`, `public/data/exercise-assets.json`, `public/data/muscle-assets.json`, `public/data/anatomy-muscle-mapping.json`, `docs/MUSCLE_ANATOMY_ASSETS.md` |
| QA | `tests/unit/`, `tests/browser/`, `qa/`, `scripts/validate-*.js`, `package.json` |

## Evidence limitations

- The requested `60_TOP_GYM_SOURCE_AUDIT_PROMPT.md` was not present; the available `35_TOPGYM_SOURCE_AUDIT.md` was used.
- No production database write or destructive operation was performed, so runtime-only schema differences were identified statically rather than normalized by execution.
- No native mobile client is present in the inspected repository. Responsive Web behavior must not be treated as Flutter evidence.
- Unknown behavior is recorded as `BLOCKED: SPECIFICATION GAP`; implementation/schema divergence is recorded as `BLOCKED: SPECIFICATION CONFLICT`.

## Source Consolidation Authority

The audit-time markers above are historical evidence. Current classifications and decisions are maintained in SOURCE_CONSOLIDATION_DECISIONS.md, TOP_GYM_LOGICFIT_DATABASE_DECISION.md, TOP_GYM_LOGICFIT_CANONICAL_MAPPING.md, and the linked LogicFit lifecycle/seed/print/enum records. No TOP GYM mutation was used to resolve a conflict.

