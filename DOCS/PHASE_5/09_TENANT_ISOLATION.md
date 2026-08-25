# Gym Context and Tenant Isolation Foundation

The architecture remains Control Plane DB plus one database per Gym. A Gym database is resolved through the Control Plane registry; the database name is never accepted as an arbitrary client-controlled cross-tenant selector.

Foundation components:

- `IGymDatabaseResolver` resolves an active registered Gym database;
- `GymDatabaseResolver` reads `platform.gym_databases` from the Control Plane;
- `IGymContextAccessor` holds the explicit request Gym scope;
- `GymScopeService` resolves the registered scope and verifies the database route;
- `GymDbContextFactory` creates a context only for the resolved Gym route;
- `core.gym_context` provides the Gym-side identity projection.

Required behavior for every future protected operation:

- authenticate and authorize on the server;
- resolve Gym context before opening the Gym data path;
- use the resolved Gym database/context for all operational queries;
- reject inactive/unregistered/unknown Gym routes;
- never infer Gym scope from UI filtering;
- never allow Gym A to query or enumerate Gym B users, sessions, permissions, or data.

The correction validated Control Plane and default Gym connectivity and the explicit registry path. Cross-Gym business-resource tests belong to the resumed Authentication/RBAC vertical slice and later module gates.
