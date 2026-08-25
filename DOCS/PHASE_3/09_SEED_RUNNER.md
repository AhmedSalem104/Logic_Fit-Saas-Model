# SQL Server Seed Runner

Phase 3 remains the canonical JSON package under `database/seeds/v1/`. The official SQL Server executor is the native .NET implementation `CanonicalLibrarySeeder` in `src/LogicFit.Infrastructure/Services/Seeding/`, invoked through `LogicFit.Api`.

The executor validates the Phase 3 manifest, resolves foreign keys by stable seed key, performs canonical-only upserts, records seed installation metadata, and wraps each Gym seed operation in a SQL Server transaction. Food conversions remain `contract-only`; no unsupported table is invented.

Commands:

```powershell
dotnet run --project src/LogicFit.Api/LogicFit.Api.csproj -- --migrate
dotnet run --project src/LogicFit.Api/LogicFit.Api.csproj -- --seed
dotnet run --project src/LogicFit.Api/LogicFit.Api.csproj -- --verify-seed
```

The former Node seed runner was a draft implementation and was removed. `tools/seed/validate-seeds.js` remains a no-database JSON contract validator, and `tools/seed/generate-seeds.js` remains the controlled Phase 3 extraction tool.

## Phase 4 integration addendum — 2026-08-25

The Phase 4 Gym migration imports this existing local reference/library target schema before invoking the official .NET executor. EF Core owns migration ordering, and the .NET seed coordinator owns application wiring without duplicating seed logic.
