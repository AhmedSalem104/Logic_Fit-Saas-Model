# P6-D-003 — Server Registry and Database Relation

**Status:** APPROVED

## Problem

Phase 2 describes provider-neutral server/placement metadata, while the
current Control Plane model has no `platform.servers` table or
`gym_databases.server_id` relation.

## Existing evidence

The approved decision permits a server registry as Platform metadata and
prohibits credentials in it. Provisioning and placement execution belong to
Phase 7. The admitted Phase 6 read-only API has no approved operation that
requires a new server resource.

## Options

1. Add a server table and relation to the Phase 6 schema.
2. Retain the server-registry concept as a future Platform metadata boundary
   and defer its table/API until a placement consumer is contracted.

## Recommendation

**Selected: Option 2.** Phase 6 exposes the existing safe database registry
metadata only. Phase 7 owns the placement/provisioning contract and may
introduce the server relation through its own approved EF change. No server
credentials, connection strings, or private keys are registry data.

## Impact

The current Phase 6 database and API contracts remain compatible with the
existing local foundation. No server table, API, permission, or mobile screen
is introduced.

## Affected surfaces

- **DB:** No Phase 6 `platform.servers` table and no Phase 6 `server_id`.
- **API:** No Phase 6 server route; database DTO excludes secret references.
- **Permissions:** No new server or placement permission.
- **Web:** Server placement UI is outside the admitted Phase 6 screens.
- **Flutter:** No Platform infrastructure UI.
- **Tests:** Phase 6 verifies safe database metadata; Phase 7 will own
  placement/provisioning tests.
