# LogicFit

LogicFit is a local-first gym SaaS platform built around the approved
Control Plane database plus database-per-Gym architecture.

## Current implementation boundary

The repository currently contains the shared technical foundation plus the
Phase 5B Authentication/RBAC vertical slice:

- ASP.NET Core Web API, C#, .NET 10, EF Core, and SQL Server;
- React, TypeScript, Vite, Tailwind, and the Web foundation;
- Flutter/Dart and the Mobile foundation;
- the Phase 3 canonical reference seed package;
- SQL-backed sessions, password management, TOTP/recovery-code MFA,
  permission-based RBAC, access administration, Gym isolation, audit, and
  their Web/Flutter client flows;
- foundation and Phase 5B tests and local development tooling.

The later business modules still require their own explicit phase
authorization. Members, Measurements, Attendance, Training, Nutrition,
Store, Finance, CRM, Classes, Reports, Notifications, and Provisioning are
not implemented by the current boundary.

## Required toolchain

- .NET SDK `10.0.400` from `global.json`;
- Visual Studio 2026 / 18.x with the ASP.NET and .NET desktop workloads;
- SQL Server for the local Control Plane and Gym databases;
- Node.js/npm for the Web workspace and seed validators;
- Flutter/Dart for the Mobile workspace.

Visual Studio 17.x is not a supported IDE for the pinned .NET 10 target.
Do not downgrade projects to .NET 9.

## Repository layout

```text
LogicFit.sln
src/                 .NET application projects
tests/               .NET test projects
apps/web/            React Web client
apps/mobile/         Flutter client
packages/            shared client contracts/configuration
database/seeds/      canonical Phase 3 seed package
database/scripts/    guarded local transition scripts
tools/               seed and development validation tools
DOCS/                approved contracts and implementation records
```

## Local setup

```powershell
cd C:\Users\B-SMART\Desktop\LogicFit
$env:DOTNET_ROOT = 'C:\Users\B-SMART\Tools\LogicFitDotNet10'
$env:DOTNET_ROOT_x64 = $env:DOTNET_ROOT
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
dotnet tool restore
npm install
Set-Location apps/mobile
flutter pub get
Set-Location ../..
```

The local foundation uses `LogicFit_ControlPlane_Local` and
`LogicFit_Gym_001_Local` by default. The root API command explicitly runs
the ASP.NET Core host in `Development` on port `5199`. Use the documented
EF Core and seed commands in `DOCS/PHASE_4/11_LOCAL_DEVELOPMENT.md`.

## Verification

```powershell
dotnet build LogicFit.sln
dotnet test LogicFit.sln
npm run typecheck:web
npm run build:web
npm run test:web
Set-Location apps/mobile
flutter analyze
flutter test
```

TOP GYM is a read-only external source at
`C:\Users\B-SMART\gym-membership-app`; it is not part of this repository.
