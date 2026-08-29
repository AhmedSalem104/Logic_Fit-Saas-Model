# Members Architecture Contract

**Status:** BLOCKED pending the Phase 8 gap register
**Scope:** Contract only; no implementation

## Architectural placement

The Members module is a Gym operational module. Its authoritative records belong in the selected Gym database in the existing `CONTROL PLANE + DATABASE PER GYM` architecture.

```text
React Web / Flutter
          |
          v
   ASP.NET Core API
          |
          +--> Control Plane: Gym context and platform authorization metadata
          |
          +--> Selected Gym DB: members.members and Member operational projections
```

Web and Flutter never connect directly to SQL Server. The API resolves the authenticated user, requested Gym, and effective permissions before a Member query or mutation. A client-supplied `gymId` is a route context, not proof of access.

## Layer responsibilities

| Layer | Members responsibility |
|---|---|
| Domain | Member identity, approved profile values, lifecycle invariants, and concurrency concepts once the gaps are closed |
| Application | Member use cases, Gym authorization, validation, duplicate/idempotency policy, and timeline projection |
| Infrastructure | Gym `DbContext` mapping, indexes, row-version handling, audit persistence, and approved queries |
| API | Canonical `/api/v1/gyms/{gymId}/members` routes, envelopes, errors, and request IDs |
| Web | Contracted MEM screens; UX permission checks only |
| Flutter | Contracted F-MEM screens; same API and backend rules |

## Existing foundation relationship

Phase 5B supplies authentication/RBAC, sessions, MFA, user status, Gym context, and the single audit system. Phase 6/7 supply Platform/Gym registry and provisioning. Phase 8 must consume those foundations and must not create a second identity, authorization, tenant, or audit system.

The current repository inspection found no Member entity, Member table mapping, Member controller, or Member service. This is consistent with the requested contract-only gate.

## Domain boundary

The Member core profile is limited to the fields in the locked Phase 2 Member contract. Membership, attendance, measurements, plans, payments, training, nutrition, CRM, documents, and QR data are related domains or separate contracts. They must not be silently embedded in Member create/update operations.

## Isolation invariants

1. Every operation has one requested Gym context.
2. The server verifies that the actor is authorized for that Gym and operation.
3. Queries include the resolved Gym scope before repository execution.
4. A Member identifier from another Gym is not disclosed; the final HTTP behavior must follow the approved 403/404 policy once closed.
5. Platform Admin does not receive implicit access to Gym operational data.
