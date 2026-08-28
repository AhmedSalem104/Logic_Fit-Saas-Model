# P6-D-008 — Audit Scope Schema Compatibility

**Status:** APPROVED

## Problem

The current single `audit.events` implementation does not yet contain the
Phase 2 `scope_type` and `scope_id` fields.

## Existing evidence

The current audit table is append-only and already records request, actor,
target, action, result, reason, metadata, and occurrence time. The approved
architecture requires one audit system and scope identification where the
contract requires it.

## Options

1. Create a second Phase 6 audit schema.
2. Keep one audit system and reconcile the current table to the canonical
   scope-aware shape through one safe EF Core migration when implementation
   begins.

## Recommendation

**Selected: Option 2.** The Phase 6 contract uses one `audit.events` system.
The implementation phase will add the nullable scope fields compatibly,
backfill no invented values, and set scope for new scoped events. No audit
search API is admitted because the canonical catalog lacks `platform.audit.view`.

## Impact

There is no schema or migration change in contract closure. The future EF
migration must preserve existing rows, avoid a competing audit table, and
continue secret redaction.

## Affected surfaces

- **DB:** One `audit.events` table with the canonical additive scope shape.
- **API:** No Phase 6 audit-search route; denied/suspicious access uses the
  existing security-audit path.
- **Permissions:** No audit-read permission is added.
- **Web:** Audit panel is outside the admitted `PA-W-009` snapshot.
- **Flutter:** No audit UI.
- **Tests:** Migration compatibility, append-only behavior, scope tagging,
  and secret-redaction regression tests.
