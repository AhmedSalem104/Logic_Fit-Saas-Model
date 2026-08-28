# P6-D-005 — Settings and Feature Flags

**Status:** APPROVED

## Problem

The existing feature-flag boundary is present, but Phase 6 has no approved
key registry, value schemas, mutation permission, or generic settings model.

## Existing evidence

The approved boundary keeps Platform flags in the Control Plane, separates
Platform and Gym scope, and prohibits arbitrary dynamic settings frameworks
and speculative keys.

## Options

1. Add a generic settings framework and flag mutation API.
2. Retain the existing feature-flag boundary without admitting Phase 6 keys,
   records, API, or UI.

## Recommendation

**Selected: Option 2.** `platform.feature_flags` remains the single boundary,
but Phase 6 creates no flag state, setting table, settings API, flag API,
cache, or mutation screen. Deployment configuration remains application
configuration, not Platform Admin data.

## Impact

No speculative configuration surface is added. Any future flag contract must
define key, type/schema, version, scope, precedence, validation, and an
approved permission before implementation.

## Affected surfaces

- **DB:** Existing `platform.feature_flags` only; no new table or records.
- **API:** No Phase 6 settings or feature-flag route.
- **Permissions:** No new settings/flag permission.
- **Web:** `PA-W-008` is outside Phase 6.
- **Flutter:** No settings/flag UI.
- **Tests:** Contract checks ensure Platform/Gym/deployment scopes are not
  mixed and no speculative keys are seeded.
