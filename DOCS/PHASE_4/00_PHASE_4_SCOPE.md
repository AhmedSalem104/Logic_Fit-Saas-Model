# Phase 4 — Local Technical Foundation Scope

**Status:** GREEN — foundation complete; Phase 5 is not started.

## In scope

- Local repository/application structure.
- ASP.NET Core Web API / C# / .NET 10 bootstrap and safe foundation endpoints (the former Fastify draft was removed).
- SQL Server Control Plane and Gym database foundation.
- EF Core migration foundation with SQL Server history.
- Phase 3 seed integration after Gym migration.
- SQL-backed session/password/TOTP abstractions without the login vertical slice.
- React/Vite/Tailwind/RTL/dark-mode shared web shell.
- Flutter/Riverpod/GoRouter/Dio/RTL/dark-mode mobile shell.
- Local tests, smoke checks, and developer setup documentation.

## Explicitly out of scope

Members, Measurements, CRM, Finance, Store, Inventory, Exercise UI, Training, Nutrition, Classes, Member Portal, Reports, provisioning UI, global migration, backup/restore execution, and production deployment.

No TOP GYM source file was modified.

## Phase 5 correction addendum

The initial Phase 4 implementation used Fastify/TypeScript as a draft. It is not canonical and was removed during the Phase 5 backend architecture correction. The official implementation is documented in `DOCS/PHASE_5/01_BACKEND_CORRECTION.md`.
The historical Phase 4 status line saying that Phase 5 was not started predates this authorized correction; the current Phase 5 state is YELLOW/PAUSED as recorded in `DOCS/PHASE_5/PHASE_5_STATUS_REPORT.md`.
