# Phase 6 Platform Admin Web Implementation

## Route and screen map

| Screen ID | Route | API calls | State behavior |
|---|---|---|---|
| PA-W-001 | `/platform-admin` | overview, monitoring | loading, retryable error, status/empty breakdown |
| PA-W-002 | `/platform-admin/organizations` | organizations, gyms | filter, paging, empty, retryable error |
| PA-W-003 | `/platform-admin/gyms/:gymId` | Gym detail | loading, not-found/error, safe registry detail |
| PA-W-005 | `/platform-admin/databases` | databases | filter, paging, empty, retryable error |
| PA-W-009 | `/platform-admin/operations` | monitoring | request-time snapshot, loading/error/empty |

The screens are implemented in `PlatformAdminPage.tsx`, use the existing
App Shell and UI primitives, and are protected by the existing authenticated
route. The server remains authoritative for permission enforcement.

## UI guarantees

- Navigation shows Platform Admin only when the current session reports
  `platform.view`; direct routes still receive backend authorization.
- All controls have loading, disabled, empty, error, and retry behavior
  appropriate to their request.
- No mutation controls are present.
- No member, payment, attendance, training, nutrition, store, CRM, or other
  Gym operational data is rendered.
- The existing document-level Arabic/RTL and light/dark theme foundation is
  reused; styles use the existing semantic CSS variables rather than fixed
  light-only colors.
- The Web client calls only the REST API and never imports SQL or database
  infrastructure.

## Verification

TypeScript typecheck, production build, and the Web test suite pass. The
automated tests cover overview/registry route rendering and API usage. The
interactive Chrome adapter could not be connected in this environment; its
failure occurs in the external browser client before a page is opened and
does not originate in this Web bundle. This is recorded as an environment
verification limitation, not suppressed in application code.
