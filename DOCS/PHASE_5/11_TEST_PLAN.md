# Backend Correction Test Plan and Results

## Automated tests

| Area | Result |
|---|---|
| `dotnet build LogicFit.sln` | PASS; .NET 10 solution builds |
| `dotnet test LogicFit.sln` | PASS; Unit 5, API 2, Integration 2 |
| EF Core fresh Control Plane migration | PASS |
| EF Core fresh Gym migration | PASS |
| EF migration history on local databases | PASS; both official IDs recorded |
| .NET seed first run | PASS; `ValidationPassed=true` |
| .NET seed second run | PASS; `ValidationPassed=true`, no duplicate seed keys |
| .NET seed verification | PASS; `ValidationPassed=true` |
| API startup | PASS |
| `/api/v1/health` | PASS, HTTP 200, request ID present |
| `/api/v1/readiness` | PASS, HTTP 200, Control Plane and Gym true |
| `/api/v1/version` | PASS, `v1` |
| Web typecheck/build/test | PASS |
| Flutter analyze/test | PASS |
| TOP GYM safety | PASS; no changes made by this correction |
| Visual Studio 2022 solution build | ENVIRONMENT GAP; installed 17.14 rejects .NET 10; Visual Studio 2026 / 18.x is required (`NETSDK1209`) |

## Validation counts

Current local seed verification: permissions 15, roles 3, role-permissions 14, exercises 1,133, muscles 297, foods 367, anatomy mappings 194, seed-installation records 11, and zero duplicate exercise/food/muscle seed keys.

## Toolchain gap

The official CLI build/run path is green with the pinned .NET 10 SDK. Visual Studio team use requires Visual Studio 2026 / 18.x. The final local gate still observed Visual Studio 17.14.16, so the IDE build remains blocked with `NETSDK1209`. This is an environment/toolchain requirement, not a reason to change the approved ASP.NET Core/.NET 10 architecture.

## Final local environment gate review

On 2026-08-25 the official .NET CLI build, tests, EF migration checks, local API smoke test, seed idempotency checks, Web build/typecheck/test, and Flutter run/analyze/test were rerun. The Visual Studio build remained blocked because the installed `devenv.com` reports 17.14.16 and resolves the system .NET 9 SDK instead of the pinned .NET 10 SDK.

The local startup configuration is now aligned on `http://127.0.0.1:5199`: the root API script, Visual Studio launch profiles, `.env.example`, and Web client fallback use the same URL.

The in-app browser verification surface was unavailable due a browser-runtime bootstrap error (`Cannot redefine property: process`). Vite startup and HTTP delivery were verified, but visual browser inspection and browser console/network inspection remain unverified.

## NU1903 remediation

The warnings were isolated to the transitive `System.Security.Cryptography.Xml` 9.0.0 dependency of `Microsoft.EntityFrameworkCore.Design` through `Microsoft.Build.Tasks.Core`/`Microsoft.CodeAnalysis.Workspaces.MSBuild`. A private direct pin to `System.Security.Cryptography.Xml` 9.0.18 was applied as the minimum same-major security remediation. Vulnerability query, .NET build, and all .NET tests pass with zero NU1903 warnings afterward.

## Not yet tested because the slice is paused

Login, logout, password reset, MFA enrollment/verification/recovery, authorization endpoint matrix, active-session UX, cross-Gym protected-resource scenarios, Auth Web E2E, Auth Flutter E2E, and Auth UAT. These are remaining Phase 5 work, not silently marked complete.
