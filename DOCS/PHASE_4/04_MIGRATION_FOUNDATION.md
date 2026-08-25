# Migration Foundation

Runner: Entity Framework Core through the official .NET host. History table: `dbo.__EFMigrationsHistory`.

## Supported local modes

```text
`dotnet run --project src/LogicFit.Api/LogicFit.Api.csproj -- --migrate`
`dotnet ef migrations list --project src/LogicFit.Infrastructure/LogicFit.Infrastructure.csproj`
```

The repository includes a root-local `dotnet-ef` tool manifest. Run
`dotnet tool restore` once after checkout; do not use the old unversioned
global EF tool or create a second migration runner.

The two EF contexts are explicit: `ControlPlaneDbContext` and `GymDbContext`. Database names are configuration-validated and the Gym route is resolved through the Control Plane registry. Windows integrated SQL authentication is the local default; SQL credentials are optional and never printed.

## Guarantees

- Ordered EF migration IDs.
- EF model snapshot and migration integrity.
- SQL Server transaction-aware application.
- Applied/pending migration history in `dbo.__EFMigrationsHistory`.
- EF migration locking for concurrent local application.
- Reapplying an applied migration is a no-op.

## Verified migration sequence

Control Plane:

1. `20260825144155_InitialControlPlaneFoundation`

Gym:

1. `20260825144011_InitialGymFoundation`

Global migration/canary/batch orchestration is not implemented in this phase.
