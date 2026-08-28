# P6-D-002 — Phase 6 Release Scope

**Status:** APPROVED

## Problem

Phase 2 names more Platform resources than can be safely admitted without
pulling provisioning or business modules into this release.

## Existing evidence

The phase sequence assigns Platform Foundation to Phase 6, Gym provisioning
to Phase 7, and Members to Phase 8. Platform infrastructure is Web-first.

## Options

1. Admit a bounded read-only registry/overview slice.
2. Admit Platform mutations, provisioning, and operations execution.
3. Defer all Platform views.

## Recommendation

**Selected: Option 1.** Phase 6 admits only read-only organization, Gym,
database registry, overview, and request-time monitoring contracts. Phase 7
and later phases retain execution and mutation responsibilities.

## Impact

The admitted tables, API routes, permission mapping, Web screens, and tests
are limited to the closed Phase 6 package. No Phase 7 or Phase 8 code is
authorized by this decision.

## Affected surfaces

- **DB:** Existing Control Plane registry tables only; no new Phase 6 table.
- **API:** The eight read-only routes listed in `03_PLATFORM_API_CONTRACT.md`.
- **Permissions:** Existing `platform.view` only.
- **Web:** `PA-W-001`, `PA-W-002`, `PA-W-003`, `PA-W-005`, and `PA-W-009`.
- **Flutter:** No Platform Admin UI.
- **Tests:** Read authorization, redaction, pagination/filtering, source, and
  boundary tests.
