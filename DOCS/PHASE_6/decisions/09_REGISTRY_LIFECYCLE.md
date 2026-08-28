# P6-D-009 — Organization and Gym Registry Lifecycle

**Status:** APPROVED

## Problem

Registry status values exist, but Phase 6 does not have an approved mutation
permission or complete mutation contract.

## Existing evidence

The approval sets Active and Inactive as the minimum lifecycle states,
preserves existing source states, requires optimistic concurrency/audit for
future registry operations, and prohibits deactivation from deleting a Gym
database or operational data.

## Options

1. Admit organization/Gym status mutations in Phase 6.
2. Keep Phase 6 read-only and defer the mutation state machine to the phase
   that receives an approved permission and complete request contract.

## Recommendation

**Selected: Option 2.** Phase 6 reads and preserves stored registry status;
it introduces no POST/PATCH lifecycle route. Future mutation work must define
allowed transitions, patchable fields, `If-Match`/row-version concurrency,
audit, authorization, and non-destructive deactivation before implementation.

## Impact

The current read DTOs expose source status without inventing labels or
transitions. Provisioning and deprovisioning remain Phase 7 concerns.

## Affected surfaces

- **DB:** Existing status and row-version columns are read as-is; no status
  migration is authorized here.
- **API:** No Phase 6 organization/Gym mutation route.
- **Permissions:** No lifecycle permission or grant is added.
- **Web:** Registry screens have no mutation controls.
- **Flutter:** No Platform registry mutation UI.
- **Tests:** Read redaction/scope tests now; future state-machine and
  concurrency tests when mutation is contracted.
