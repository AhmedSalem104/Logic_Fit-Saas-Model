# Backend Architecture Correction

## Authority

The approved LogicFit backend architecture is:

`ASP.NET Core Web API → C# → .NET 10 → Entity Framework Core → Microsoft SQL Server`

Visual Studio is the intended IDE and `LogicFit.sln` is the official solution. The repository builds deterministically with the pinned .NET 10 SDK in `global.json`. The installed Visual Studio 2022 `17.14.36518.9` was checked and cannot target .NET 10; it reports `NETSDK1209`. The approved IDE prerequisite is Visual Studio 2026 / 18.x. The target is intentionally not downgraded to .NET 9.

## Non-canonical implementation classification

The former `apps/api` Fastify/TypeScript implementation, its Node package dependencies, Node AuthService, Node session/security adapters, Node migration runner, Node SQL seed runner, and Node auth seed package were draft/non-canonical implementation artifacts. Their approved intent was extracted into documentation and native .NET contracts. The implementation code and duplicate runtime path were removed.

Removed implementation paths:

- `apps/api/` (Fastify/TypeScript backend, draft AuthService, tests, and generated server output);
- `tools/migration/` and its Node migration runner;
- `tools/seed/run-sqlserver-seed.js`, `tools/seed/run-auth-seed.js`, and `tools/seed/local-test-schema.sql`;
- `tools/dev/foundation-smoke.js`, `tools/dev/health-check.js`, and `tools/dev/reset-local-database.js`;
- executable SQL files under `database/migrations/`;
- duplicate `database/seeds/auth/v1/` Node auth seed package.

The Phase 3 JSON package, Web workspace, Flutter project, and no-database seed validator/generator remain.

Preserved intent:

- 15 permission keys;
- 3 system roles and 14 role-permission assignments;
- permission-based authorization and Gym scope;
- SQL-backed session policy;
- password, TOTP, recovery-code, and audit requirements;
- schema intent for authentication/RBAC;
- deterministic reference seed behavior.

## Official project graph

```text
LogicFit.Api
  → LogicFit.Application
  → LogicFit.Infrastructure
  → LogicFit.Shared

LogicFit.Application → LogicFit.Domain, LogicFit.Shared
LogicFit.Infrastructure → LogicFit.Application, LogicFit.Domain, LogicFit.Shared
LogicFit.Domain → no infrastructure/UI dependency
```

Tests are `LogicFit.UnitTests`, `LogicFit.IntegrationTests`, and `LogicFit.ApiTests`.

## Client boundary

React and Flutter remain unchanged by this correction and continue to consume the same REST API. No authentication screens were added. The only API endpoints currently exposed by the official backend are `/api/v1/health`, `/api/v1/readiness`, and `/api/v1/version`.

## Decision record

There is no local `DECISION_LOCK.md` file in the LogicFit root. The external approved Master Bible and Decision Lock remain higher authority, as already stated by `DOCS/MASTER_INDEX.md`; this correction does not create a competing local lock.
