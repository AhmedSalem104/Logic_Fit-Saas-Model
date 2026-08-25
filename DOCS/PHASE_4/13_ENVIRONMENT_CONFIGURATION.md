# Environment Configuration

The committed [`.env.example`](../../.env.example) contains placeholders only.

The repository pins SDK `10.0.400` in `global.json`. On this machine the SDK is installed at `C:\Users\B-SMART\Tools\LogicFitDotNet10`; the current PowerShell session must expose `DOTNET_ROOT`/`PATH` before invoking the .NET-backed npm scripts. The approved IDE prerequisite is Visual Studio 2026 / 18.x with the required .NET workloads. Visual Studio 17.x is not an accepted environment for `net10.0`.

Validated groups:

- ASP.NET Core environment/URL and .NET runtime configuration.
- CORS and rate-limit settings.
- SQL Server server/auth mode/trust setting and named Control Plane/Gym databases.
- Session timeout/MFA/reset policy boundaries.
- API CORS and rate-limiter foundation.

Rules:

- Windows integrated authentication is the local default.
- SQL username/password are required together when SQL authentication is selected.
- Configuration validation rejects incomplete SQL authentication settings and invalid timeout values.
- No secrets are present in the example file.
- `.env`, credentials, and generated build/dependency directories are ignored.
- Flutter API URL is supplied with `--dart-define=LOGICFIT_API_BASE_URL=...` when needed.

The canonical local API URL is `http://127.0.0.1:5199`: it is defined in `.env.example`, the root `dev:api` command, the API launch profiles, and the Web fallback. Web and API local defaults are therefore aligned.

Visual Studio readiness on the inspected machine remains RED until Visual Studio 2026 / 18.x is installed. The currently installed Community instance is 17.14.16 and reports `NETSDK1209` for the pinned .NET 10 SDK. The project remains on `net10.0`; no downgrade is permitted.
