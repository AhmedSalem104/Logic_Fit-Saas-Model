# Phase 6 Approved Decisions

**Status:** GREEN — human decisions recorded; implementation not started
**Authority:** Phase 6 Human Decision Approval, 2026-08-28

| ID | Selected decision | Status |
|---|---|---|
| P6-D-001 | Existing repository documentation is authoritative; absent kickoff/roadmap files are not reconstructed. | APPROVED |
| P6-D-002 | Phase 6 is Platform Foundation only; Phase 7 owns provisioning and Phase 8 owns Members. | APPROVED |
| P6-D-003 | Server registry is platform metadata, but server placement/table/API is deferred from the local Phase 6 read-only slice. | APPROVED |
| P6-D-004 | No commercial plans, subscriptions, billing, or payment gateways in Phase 6. | APPROVED |
| P6-D-005 | Retain the existing feature-flag boundary; no speculative keys, settings framework, or Phase 6 flag API. | APPROVED |
| P6-D-006 | Overview/monitoring is a Platform Admin snapshot from existing API health and Control Plane registry metadata only. | APPROVED |
| P6-D-007 | Use only existing `platform.view`; add no permission keys, aliases, or role grants. | APPROVED |
| P6-D-008 | One audit system; target canonical audit shape adds scope fields through one safe EF migration when implementation begins. | APPROVED |
| P6-D-009 | Phase 6 registry is read-only; Active/Inactive is the minimum future lifecycle and existing source states are preserved. | APPROVED |
| P6-D-010 | Phase 6 admits only the fully specified read-only API list in `03_PLATFORM_API_CONTRACT.md`. | APPROVED |

The detailed evidence, options, selected option, and surface impacts for
each decision are recorded in `decisions/01_` through `decisions/10_`.

## Later Phase 7 transition - 2026-08-29

P7-D-001 subsequently approves `platform.provision` for Phase 7
provisioning execution. It does not revise P6-D-007: all Phase 6 routes
remain read-only and continue to use only `platform.view`. The Phase 7 key is
not added to the Phase 6 runtime by this contract-only update.
