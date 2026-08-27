# Authentication/RBAC Web Implementation

**Status:** IMPLEMENTED — typecheck/build/Vitest and direct Chrome verification passing

## Client boundary

The React application uses `apps/web/src/lib/api.ts` as its sole HTTP client. It sends the opaque session bearer to ASP.NET Core and never accesses SQL Server. Session metadata is kept only for UI rehydration; `/api/v1/auth/me` remains authoritative.

## Screens and routes

| Screen | Route | API use |
|---|---|---|
| Login/MFA | `/login` | login, combined MFA verify |
| Password reset | `/password-reset` | canonical hyphenated request/complete routes |
| Authenticated home | `/app` | me, sign-out |
| Account security | `/app/security` | sessions, revoke, password change, MFA, recovery codes |
| Access administration | `/platform/access` | catalog, users, status, role assignment/revocation; permission-aware Gym and Platform scope selection; API-enforced target scope |

## UX/security behavior

The screens use the existing design system and support RTL/Arabic, responsive layout, light/dark themes, loading, empty, validation, server-error, success, and disabled states. Protected routes redirect to `/login` when the server session is unavailable. The access screen selects an authorized Gym target or Platform scope and derives the role list from the API catalog; the client never decides whether an operation is authorized and only reflects the API result.

## Verification

`apps/web/src/App.test.tsx` covers shell/health, safe login failure, successful login/API-backed shell, combined recovery-code MFA path, and protected-route redirect. `npm run typecheck`, `npm test -- --run`, and `npm run build` pass.

On 2026-08-26, direct Google Chrome `151.0.7922.170` opened `http://localhost:5173/login` and `http://localhost:5173/` against the running ASP.NET Core API at `http://127.0.0.1:5199`. The real browser rendered the Arabic/RTL login page, performed a successful local fixture login, rendered `/app`, and exercised light/dark theme switching and responsive shell behavior. Browser requests to `/api/v1/health`, `/api/v1/readiness`, `/api/v1/version`, `/api/v1/auth/login`, and `/api/v1/auth/me` succeeded, including the approved localhost-to-127.0.0.1 CORS path. No LogicFit application console exception or `Cannot redefine property: process` occurred. That error was observed only while the external browser-control adapter bootstrapped and is documented as an adapter limitation; no Web workaround or polyfill was added.

The same live Chrome run then exercised invalid login, TOTP enrollment/verification, recovery-code verification and one-time consumption, password change with reauthentication, the canonical hyphenated password-reset request, session/security actions, access catalog, user creation, status transition, role assignment, and role revocation. The discovered Gym-scope selection defect was fixed in `AccessPage.tsx`; an authorized Platform actor's explicit Gym selection is now preserved, and inactive role reactivation sends the approved `If-Match` version. No LogicFit application exception or API network error remained after the fix. The only browser asset warning observed in the final direct run was the Vite request for the optional `/favicon.ico` (HTTP 404), which was documented without adding an unapproved asset.
