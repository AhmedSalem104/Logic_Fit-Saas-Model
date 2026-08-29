# Members Architecture Contract

**Status:** GREEN — contract closed; implementation not authorized by this task

## Placement

Members is Gym operational data in the existing `CONTROL PLANE + DATABASE PER GYM` architecture.

```text
React Web / Flutter
          |
          v
   ASP.NET Core API
          |
          +--> Control Plane: Gym context and authorization metadata
          +--> Selected Gym DB: members.members and Member timeline
```

Web and Flutter use the API only. The API resolves the authenticated actor, requested Gym, and effective permission before accessing Member data. A route `gymId` is context, not proof of authorization.

## Layer responsibilities

| Layer | Members responsibility |
|---|---|
| Domain | Member identity, `ACTIVE`/`INACTIVE`/`ARCHIVED` lifecycle, immutable identity, and concurrency invariants |
| Application | Gym authorization, validation, idempotent create/archive, update concurrency, and Member timeline projection |
| Infrastructure | Gym `DbContext` mapping, constraints/indexes, row-version handling, safe queries, and audit persistence |
| API | The six canonical Members operations, envelopes, errors, request IDs, and scope enforcement |
| Web | MEM-W-001/002/003 using the existing App Shell; client checks are UX only |
| Flutter | F-MEM-001/002 using Dio/Riverpod/GoRouter; same backend rules |

## Isolation invariants

1. Every Member operation is resolved against exactly one requested Gym.
2. The backend checks the actor's permission for that Gym before the query or mutation.
3. No Member query is executed without the resolved Gym predicate.
4. Cross-Gym Member access returns the canonical scope-denial behavior.
5. Platform Admin permission does not imply Gym business access.
6. No future business module is imported into Member create/update/archive behavior.

## Reused foundations

Phase 5B supplies authentication, sessions, MFA, RBAC, Gym context, and audit. Phase 6/7 supply registry, provisioning, and the selected Gym database. Phase 8 adds no identity, tenant, authorization, migration, seed, or audit subsystem.
