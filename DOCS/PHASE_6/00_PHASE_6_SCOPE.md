# Phase 6 Scope

**Status:** GREEN — contract closure complete; implementation not started
**Date:** 2026-08-27

## Purpose

Phase 6 is the bounded Platform Foundation contract. It covers safe,
read-only Platform Admin views over Control Plane registry and health
metadata. It does not implement a Gym business module or provisioning.

The approved topology remains:

```text
Control Plane SQL Server database
  - platform identity and registry metadata
  - platform audit
  - approved platform configuration boundary

One SQL Server database per Gym
  - Gym operational data
```

No C# business code, EF migration, SQL change, seed change, React feature,
Flutter feature, or TOP GYM change was made by this contract-closure task.

## Contract authority

The repository documentation is authoritative for this phase under P6-D-001.
Missing root copies of `DECISION_LOCK.md`, `IMPLEMENTATION_ROADMAP.md`, and
`CODEX_KICKOFF.md` are not reconstructed and do not block the phase.

The detailed contract package is:

- `01_PLATFORM_ARCHITECTURE.md`
- `02_PLATFORM_DATABASE_CONTRACT.md`
- `03_PLATFORM_API_CONTRACT.md`
- `04_PLATFORM_PERMISSION_CONTRACT.md`
- `05_PLATFORM_MONITORING_CONTRACT.md`
- `06_PLATFORM_SETTINGS_CONTRACT.md`
- `07_PLATFORM_WEB_CONTRACT.md`
- `08_PLATFORM_FLUTTER_CONTRACT.md`
- `09_PLATFORM_TRACEABILITY.md`
- `10_PHASE_6_DECISIONS.md`
- `11_PHASE_6_CONTRACT_GAP_REGISTER.md`

## Admitted Phase 6 scope

- Control Plane organization registry reads.
- Control Plane Gym registry and safe detail reads.
- Control Plane Gym database registry and safe detail reads.
- Platform Admin overview counts and status metadata.
- Request-time API health and registry monitoring snapshot.
- Existing `platform.view` authorization and existing audit/security boundary.

The admitted API and DTOs are defined completely in
`03_PLATFORM_API_CONTRACT.md`. The admitted Web screens are defined in
`07_PLATFORM_WEB_CONTRACT.md`. No Platform Flutter screen is admitted.

## Explicit boundaries

| Phase | Scope |
|---|---|
| Phase 6 | Platform Foundation read-only registry, overview, and health metadata. |
| Phase 7 | Gym database creation, placement/provisioning execution, provisioning orchestration, and new-Gym migration execution. |
| Phase 8 | Members, memberships, attendance, and member-linked operations. |
| Later | Platform operations execution, backup/restore, deployment/DR, commercial subscriptions, and other Gym business modules. |

Phase 6 must not move Gym operational tables into the Control Plane, create a
shared operational tenant database, execute provisioning, or expose detailed
member/payment/attendance/training/nutrition/store/CRM data.

## Gate result

All Phase 6 contract gaps have an explicit approved resolution or deliberate
deferment. Phase 6 implementation remains unauthorized by this document.
