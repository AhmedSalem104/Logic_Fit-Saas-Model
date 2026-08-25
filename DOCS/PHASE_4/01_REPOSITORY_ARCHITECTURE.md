# Repository Architecture

The official root remains `C:\Users\B-SMART\Desktop\LogicFit`. TOP GYM remains outside the project at `C:\Users\B-SMART\gym-membership-app`.

Application roots:

- `src/LogicFit.Api`: HTTP boundary, bootstrap, middleware, configuration, and API composition.
- `src/LogicFit.Application`: application abstractions, context services, and dependency registration.
- `src/LogicFit.Domain`: framework-independent domain constants and value objects.
- `src/LogicFit.Infrastructure`: SQL Server/EF Core persistence, security primitives, migrations, and seed services.
- `src/LogicFit.Shared`: API contracts and shared configuration types.
- `apps/web`: React + TypeScript + Vite + Tailwind foundation.
- `apps/mobile`: real Flutter/Dart iOS/Android project.
- `packages/*`: shared API envelopes, foundation DTOs, and client-safe tokens used by Web.
- `database/seeds`: Phase 3 canonical seed package; its identity and contents are unchanged.
- `src/LogicFit.Infrastructure/Persistence/Migrations`: the only executable EF Core migration definitions.
- `database/migrations`: documentation-only legacy location; no competing runner.
- `database/scripts`: guarded local transition scripts only.
- `tools/seed`: Phase 3 generator/validator and consistency helpers.
- `tools/dev`: documentation consistency helper.
- `DOCS`: approved contracts, decisions, audit evidence, and implementation records.

Within the .NET projects, folders follow responsibility rather than feature speculation:

- API middleware is under `src/LogicFit.Api/Middleware`.
- Application interfaces/records are under `src/LogicFit.Application/Abstractions`; context implementations are under `Common/Context`.
- Infrastructure persistence is separated into `Persistence/ControlPlane`, `Persistence/Gym`, and `Persistence/Migrations`; seed services are under `Services/Seeding`.
- Shared HTTP types are under `LogicFit.Shared/Contracts`; runtime options are under `LogicFit.Shared/Configuration`.

There is one backend, one migration system, one seed executor, and no duplicate application root. The official solution is `LogicFit.sln`.
