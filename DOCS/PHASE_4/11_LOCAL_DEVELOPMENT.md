# Local Development Runbook

## Prerequisites

- .NET 10 SDK and Visual Studio-compatible solution tooling.
- Node.js/npm only for the React Web workspace and Phase 3 JSON validation/extraction tools.
- Flutter/Dart SDK.
- SQL Server service and `sqlcmd`.
- SQL Server service and `sqlcmd` for local database inspection.

## Setup

```powershell
cd C:\Users\B-SMART\Desktop\LogicFit
$env:DOTNET_ROOT = 'C:\Users\B-SMART\Tools\LogicFitDotNet10'
$env:DOTNET_ROOT_x64 = $env:DOTNET_ROOT
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
Copy-Item .env.example .env
dotnet tool restore
npm install
Set-Location apps/mobile
flutter pub get
Set-Location ../..
```

Do not put real credentials in `.env`; it is ignored by Git.

## Foundation database flow

```powershell
dotnet run --project src/LogicFit.Api/LogicFit.Api.csproj -- --migrate
dotnet run --project src/LogicFit.Api/LogicFit.Api.csproj -- --seed
dotnet run --project src/LogicFit.Api/LogicFit.Api.csproj -- --verify-seed
```

The official .NET commands target the configured local databases. They never connect to TOP GYM. Database provisioning/creation is a later approved platform operation; this foundation does not silently create arbitrary databases.

## Applications

```powershell
npm run dev:api
npm run dev:web
Set-Location apps/mobile
flutter run
```

For API readiness with the local smoke databases, set `CONTROL_PLANE_DATABASE` and `DEFAULT_GYM_DATABASE` in `.env` to the names above.

## Checks

```powershell
npm run typecheck
npm run build
dotnet test LogicFit.sln
npm run test:web
Set-Location apps/mobile
flutter analyze
flutter test
flutter build apk --debug --no-pub
Set-Location ../..
Invoke-WebRequest http://127.0.0.1:5199/api/v1/health
Invoke-WebRequest http://127.0.0.1:5199/api/v1/readiness
```

The canonical local API URL is `http://127.0.0.1:5199`; the root
`dev:api` script and the Visual Studio launch profiles use the same URL.
The root script also explicitly sets the ASP.NET Core host environment to
`Development`, so it does not depend on implicit `.env` loading.

The previous Node reset/health/foundation smoke helpers were removed. Any local reset/restore operation requires the approved SQL Server backup/restore runbook and explicit Lead authorization.
