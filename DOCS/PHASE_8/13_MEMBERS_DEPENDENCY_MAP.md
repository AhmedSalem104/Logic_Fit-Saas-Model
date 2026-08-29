# Members Dependency Map

**Status:** GREEN — dependencies and boundaries closed

```text
Phase 5B Auth / Sessions / MFA / RBAC / Audit
                         |
Phase 6 Platform + Phase 7 Gym registry/provisioning
                         |
                 Phase 8 Members core
                    /        |       \
             Portal-safe   Timeline   future business modules
             projection                 |
                              Memberships / Attendance / Measurements /
                              Training / Nutrition / Payments / Store /
                              Finance / CRM / Classes / Documents
```

## Required dependencies

| Dependency | Phase 8 use | Boundary |
|---|---|---|
| Authentication/session | Reuse authenticated Phase 5B staff session | No second auth |
| RBAC | Evaluate the five approved Members permissions in Gym scope | Exact grants in 04 |
| Audit | Reuse the one server audit system | No second audit |
| Gym context | Resolve selected active Gym database | Phases 6/7 foundation |
| EF Core | Persist and query the Gym schema | Only migration system |
| Portal | Preserve Member Code flow and safe projection boundary | No Portal auth change |
| Timeline | Persist/read four Member-domain event types | No future source leakage |

## Future dependencies

Memberships, attendance, measurements, training, nutrition, payments, store, finance, CRM, classes, reports, notifications, documents, and QR remain separate contracts. Their records do not become side effects of Member create/update/archive.

## Explicit exclusions

- no membership packages, subscriptions, billing, or payments;
- no Attendance implementation (`F-MEM-003` remains separate);
- no measurements, training, nutrition, store, finance, CRM, classes, documents, or notifications;
- no operational Member seed/demo data;
- no Platform Admin implicit Member access;
- no provisioning or Phase 5B/6/7 redesign.
