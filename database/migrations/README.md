# LogicFit migration source

The official migration system is Entity Framework Core. Canonical migrations live beside the .NET persistence model in:

- `src/LogicFit.Infrastructure/Persistence/Migrations/ControlPlane/`
- `src/LogicFit.Infrastructure/Persistence/Migrations/Gym/`

`database/migrations/` is retained as a documentation location only; the former Node/Fastify SQL migration files and runner were removed during the Phase 5 backend correction. There is one executable migration system: EF Core, recorded in `dbo.__EFMigrationsHistory`.

The one-time local transition scripts under `database/scripts/` are not a competing migration runner. They only baseline the already-existing local draft schema and record the official EF history after safety checks.
