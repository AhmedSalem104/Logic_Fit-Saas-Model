# Members Dependency Map

**Status:** BLOCKED for implementation pending the Phase 8 gap register

## Dependency graph

```text
Phase 5B Authentication / Sessions / MFA / RBAC / Audit
                         |
Phase 6 Platform + Phase 7 Gym registry/provisioning
                         |
                 Phase 8 Members core
                    /        |       \
             Member Portal  Timeline   Future profile tabs
                              |          |
                  future Memberships  Attendance / Measurements /
                                      Training / Nutrition / Documents /
                                      Payments / CRM / Classes
```

## Required dependencies

| Dependency | Phase 8 relationship | Boundary |
|---|---|---|
| Authentication/session | Reuse current authenticated staff session | Phase 5B; no second auth |
| RBAC | Evaluate existing Members permissions in resolved Gym scope | Phase 5B; exact role grants are P8-G-002 |
| Audit | Reuse one audit system for Member mutations/security events | Phase 5B; no second audit |
| Gym context/provisioning | Use an existing active Gym database/context | Phases 6/7; Phase 8 does not provision |
| Phase 3 library seeds | No dependency for Member core profile | Must remain unchanged |
| Member Portal | Separate scoped Portal projection/auth contract | Future/adjacent; do not change Portal auth |
| Timeline | Core route is catalogued, but source/event boundary is open | P8-G-005 |

## Future-domain dependencies

Memberships, attendance, measurements, training, nutrition, payments, store, finance, CRM, classes, reports, notifications, documents, and QR are not imported into Member create/update/delete behavior. Their relationships may be represented by future links only after their own contracts are approved.

## Exclusions

- No membership package/catalog behavior.
- No payments or billing.
- No attendance implementation; `F-MEM-003` remains separate.
- No Member seed/demo data.
- No Platform Admin implicit Member access.
- No change to provisioning or Phase 5B.
