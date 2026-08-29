# Phase 7 - Provisioning Architecture

**Status:** GREEN - Phase 7 provisioning architecture is implemented and verified locally.

## Topology

```text
Platform Admin + verified MFA
          |
          v
ASP.NET Core API (/api/v1/platform/provisioning)
          |
          +-- Control Plane: organization, Gym, server, DB registry,
          |                  provisioning run/steps, IAM, audit
          |
          +-- asynchronous provisioning worker/orchestrator
                    |
                    +-- registered server placement adapter
                    +-- SQL Server database creation adapter
                    +-- EF Core Gym migration executor
                    +-- .NET canonical seed executor
                    +-- Gym verification and Owner initialization
                    |
                    +-- one isolated LogicFit Gym database
```

The Web client consumes the API only. Flutter has no Phase 7 provisioning
client. Neither client connects to SQL Server.

## Responsibility boundaries

| Component | Phase 7 responsibility |
|---|---|
| API | Authenticate, authorize, validate the request, persist the operation, return `202`, and expose safe status/retry commands. |
| Application | Coordinate the approved lifecycle and enforce idempotency, state transitions, retry rules, and audit intent. |
| Control Plane persistence | Store registry metadata, operation state, placement metadata, safe fingerprints, and audit records. |
| Provisioning worker | Execute long-running work outside the HTTP request lifecycle and recover an accepted operation after restart. |
| Server placement adapter | Select/validate a registered server without exposing credentials. |
| SQL Server adapter | Create the generated database on the selected registered server; it never accepts a client connection string. |
| EF Core executor | Apply the existing Gym migrations to the newly created database. EF Core is the only migration authority. |
| .NET seed executor | Run the unchanged Phase 3 seed package in its approved order and record seed version. |
| Gym database | Own `core.gym_context`, `auth.gym_users`, and the Gym-scoped library foundation; it never owns Control Plane registry rows. |

## Ordered workflow

The worker follows the exact approved order:

1. request validation;
2. organization creation;
3. Gym registry creation;
4. server placement selection;
5. database creation;
6. EF Core migrations;
7. canonical seed execution;
8. verification;
9. Owner initialization; and
10. activation.

Owner initialization is a substep of the `Verifying` lifecycle stage because
the approved lifecycle has no separate Owner state. No extra lifecycle state
is introduced.

## No competing systems

This contract admits exactly one ASP.NET Core backend, one EF Core migration
mechanism, one .NET runtime seed executor, one SQL access layer, one
authentication/RBAC system, one SQL-backed session system, and one audit
system. The former Node/Fastify implementation is not part of the runtime
path.

## Implemented components

- `SqlProvisioningService` persists the accepted operation, enforces
  authorization/MFA/idempotency, and coordinates the approved steps.
- `ProvisioningQueue` and `ProvisioningWorker` execute accepted work outside
  the HTTP request and recover persisted non-terminal runs on startup.
- `SqlServerDatabaseCreator` creates only system-generated local SQL Server
  database names through the master connection; credentials remain in
  configuration.
- `GymDbContextFactory` applies the existing Gym EF Core migrations and the
  existing .NET canonical seed executor.
- `ProvisioningController` exposes only the three locked routes.
- `PlatformProvisioningPage` is the Web-only Platform Admin client; it calls
  the API and never accesses SQL Server.
