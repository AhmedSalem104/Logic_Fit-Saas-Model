# P6-D-001 — Phase 6 Authority Source

**Status:** APPROVED

## Problem

The workspace does not contain root copies of `DECISION_LOCK.md`,
`IMPLEMENTATION_ROADMAP.md`, or `CODEX_KICKOFF.md`.

## Existing evidence

The repository contains the approved Phase 2, Phase 3, Phase 4, and Phase 5
contracts and the Phase 6 contract package. The approval explicitly assigns
the repository documentation as the working authority and prohibits
reconstructing absent files.

## Options

1. Reconstruct or require missing root files.
2. Use the existing repository documentation and do not invent absent files.

## Recommendation

**Selected: Option 2.** The existing repository documentation is authoritative
for Phase 6. Missing files are recorded as absent, not recreated.

## Impact

This closes the authority-chain question without changing any contract,
schema, permission, or runtime behavior.

## Affected surfaces

- **DB:** Scope is taken from the existing Phase 2 database contract.
- **API:** Scope is taken from the existing Phase 2 API contract plus the
  closed Phase 6 API contract.
- **Permissions:** Existing canonical catalog remains authoritative.
- **Web:** Existing Phase 2 screen catalog remains authoritative.
- **Flutter:** Existing Web-only Platform classification remains authoritative.
- **Tests:** Documentation consistency checks must verify repository links and
  absence of invented authority files.
