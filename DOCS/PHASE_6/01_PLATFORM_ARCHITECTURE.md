# Phase 6 Platform Architecture

**Status:** GREEN — contract closed; implementation not started

## Approved topology

Platform data is owned by the Control Plane database. Gym operational data
is owned by the selected Gym database. The API resolves Control Plane
registry and Gym scope on the server; React and Flutter never connect to SQL
Server directly.

## Existing Control Plane foundation

| Entity/table | Boundary | Phase 6 contract use |
|---|---|---|
| `OrganizationEntity` / `platform.organizations` | Control Plane | Read-only organization registry |
| `GymEntity` / `platform.gyms` | Control Plane | Read-only Gym registry/detail |
| `GymDatabaseEntity` / `platform.gym_databases` | Control Plane | Read-only safe database metadata |
| `FeatureFlagEntity` / `platform.feature_flags` | Control Plane | Retained boundary; no Phase 6 keys/API |
| `AuditEventEntity` / `audit.events` | Control Plane | One shared audit architecture; no Phase 6 search API |
| Migration definition/run metadata | Control Plane | Existing foundation; execution remains outside this slice |

Existing entities are evidence of current foundation only. They do not
authorize uncontracted writes or additional tables.

## Security and data boundaries

- No shared operational database for multiple Gyms.
- No cross-database SQL foreign keys.
- No client-supplied connection string or database name.
- No Platform response contains credentials, secret references, or Gym
  operational records.
- Platform routes require the existing authenticated Platform scope and
  `platform.view`.
- Phase 5B authentication, sessions, RBAC, isolation, and audit remain the
  single security implementation.

## Server metadata boundary

P6-D-003 approves a provider-neutral server registry as Platform metadata
when a future placement consumer requires it. Phase 6 does not admit a new
server table, server API, or placement execution because the bounded local
read-only slice has no approved consumer. Phase 7 owns placement and
provisioning execution. No credentials are stored in registry metadata.

## Phase boundaries

- **Phase 6:** Control Plane registry, overview, and request-time health
  metadata read contract.
- **Phase 7:** database provisioning, placement, provisioning workers,
  migration orchestration for new Gyms, and provisioning lifecycle.
- **Phase 8:** member identity and member-linked Gym operations.
- **Later:** operational migration/backup/deployment/DR execution and
  commercial subscription behavior.
