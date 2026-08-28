# Phase 6 Platform Foundation Test Results

**Run date:** 2026-08-29

## Automated results

| Check | Result | Evidence |
|---|---|---|
| Visual Studio solution build | PASS | Visual Studio Community 2026 `18.9.2`; `LogicFit.sln`: 8 succeeded, 0 failed |
| .NET solution tests | PASS | 35 passed: Unit 5, Integration 2, API 28 |
| Phase 6 API tests | PASS | 11 passed within `PlatformApiTests` |
| Web typecheck | PASS | `npm run typecheck` |
| Web tests | PASS | 8 tests in 2 files |
| Web production build | PASS | `npm run build` |
| Flutter analysis | PASS | `flutter analyze`, no issues |
| Flutter tests | PASS | 2 tests passed |
| Phase 3 seed consistency | PASS | `node tools/seed/check-phase3-consistency.js` |
| Phase 4 consistency | PASS | `node tools/dev/check-phase4-consistency.js` |
| EF Control Plane migrations | PASS | two official migrations applied; history matches model |
| EF Gym migrations | PASS | one official migration applied; unchanged by Phase 6 |
| Seed verification | PASS | v1, permissions 15, roles 3, assignments 14, canonical records 2074 |
| Seed idempotency | PASS | second `--seed` completed with the same verified counts |
| API health/readiness/version | PASS | HTTP 200; readiness reports control-plane=True and gym=True |
| CORS preflight | PASS | localhost:5173 allowed for API GET with credentials |

## SQL Server verification

`LogicFit_ControlPlane_Local` contains 2 EF history rows, one organization,
one Gym, one registered database, 15 permissions, 3 roles, and 14 role
assignments. `audit.events` has the two approved nullable scope columns.

`LogicFit_Gym_001_Local` contains 1 EF history row, 1,133 exercises, 297
muscles, 367 foods, and 194 anatomy mappings. Duplicate seed-key checks for
exercises, muscles, foods, and anatomy mappings are all zero. No legacy
migration table was found in either database.

## API runtime smoke test

The API ran at `http://127.0.0.1:5199`. Health, readiness, and version all
returned 200 with request IDs and security headers. A temporary local-only
Platform Security Admin identity exercised all eight approved platform
routes; every route returned 200 with the approved envelope and no secret
field. The session was logged out and the local test identity is removed
after final verification.

## Browser verification limitation

The installed browser is Google Chrome `151.0.7922.174`, but the Codex
Chrome adapter could not initialize: its browser-client import fails at the
adapter's own `globalThis.process` shim (`browser-client.mjs:33`) with
`Cannot redefine property: process`. Diagnostics also show that the
selected Chrome profile has no ChatGPT Chrome Extension/native-host
registration. This is outside the LogicFit repository. No polyfill,
dependency, or application error suppression was added.

HTTP serving and API CORS were independently verified at
`http://localhost:5173`; interactive browser console, visual theme, and
responsive checks remain pending until the external adapter is repaired or
manual Chrome verification is performed.
