# TOP GYM → LogicFit Database Decision

**Decision ID:** SC-003  
**Date:** 2026-08-25  
**Status:** RESOLVED for source consolidation  
**TOP GYM modification:** none

## Conflict

The checked-in TOP GYM `database/schema.sql` does not describe every structure used by the running application. Runtime DDL in `src/services/coaching-service.js` adds coaching columns/tables, including RIR/RPE, nutrition calculator fields, `athlete_checkins`, and `coaching_activity_events`.

## Evidence

- `DOCS/TOP_GYM_DATABASE_MAPPING.md` records the static schema and runtime `ensure*` structures.
- `DOCS/TOP_GYM_LIVE_DB_READONLY_SNAPSHOT.md` records the read-only connection metadata and row counts.
- `TOP GYM/src/services/coaching-service.js:180-368` contains the runtime DDL evidence.
- Read-only SQL metadata on 2026-08-25 confirmed the live `dbo.gym_exercises` columns and status distribution. No DDL, DML, migration, or seed was executed.

## Authority decision

1. Runtime SQL Server state is authoritative for describing the current TOP GYM database instance.
2. `database/schema.sql` remains legacy/documentation evidence and is not rewritten to match runtime.
3. TOP GYM is not modified to reconcile the two descriptions.
4. LogicFit must not blindly copy either source as its schema.
5. LogicFit's canonical SQL Server mapping will be derived from verified runtime behavior plus approved LogicFit requirements, then implemented through LogicFit-owned migrations.

## Consequences for LogicFit

The source audit contributes observed concepts, fields, relationships, and historical data. It does not dictate the LogicFit table layout. The canonical design must additionally enforce:

- SQL Server as the locked engine.
- Control Plane database plus database per Gym.
- Tenant/Gym isolation at connection/context and query boundaries.
- Versioned migrations with history/checksums.
- Canonical seed data separated from organization-owned custom data.
- Explicit lifecycle/version/snapshot structures for published training and nutrition plans.

TOP GYM numeric primary keys are not LogicFit primary keys. Source IDs are stored only as provenance where useful.

## Classification

The conflict is **resolved as runtime truth for legacy state plus documentation/architecture drift**. The exact LogicFit schema remains a Phase 2 contract item, not an unresolved choice between TOP GYM files.

