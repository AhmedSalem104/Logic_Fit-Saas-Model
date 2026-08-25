# Phase 2 Module Dependency Graph

## Canonical dependency order

```text
Authentication / Identity
        ↓
Control Plane + RBAC + Audit
        ↓
Gym Provisioning + Gym DB Context + Settings/Branding/Storage
        ↓
Members + Memberships + Attendance
        ↓
Measurements ───────────────┐
                            ├→ Nutrition
Exercise/food canonical libs ┘
        ├→ Training
        ├→ Member Portal
        └→ Reports / Print / PDF

Members → CRM
Memberships/Payments → Finance
Products/Suppliers → Store/POS → Inventory → Finance/Reports
Members + Trainers → Classes/Booking → Member Portal

All domains → Audit
All persisted snapshots → Reports / Print / PDF
Platform Operations → Migrations / Backups / Monitoring / DR
```

## Dependency contract

| Module | Required predecessors | Consumes | Produces |
|---|---|---|---|
| Authentication | none | Control Plane identity | authenticated actor/session/scope |
| Control Plane/RBAC | Authentication | identity, permission catalog | Gym/server/DB/operation authority |
| Gym Provisioning | Control Plane/RBAC, adapters | server/DB/seed/migration contracts | ready Gym DB context and Owner scope |
| Members | Gym context/RBAC | member source mapping | member identity for downstream domains |
| Measurements | Members | exact audited measurement set | valid member context for Training/Nutrition |
| Canonical Libraries | Gym provisioning/seed contract | Phase 3 canonical datasets | resolvable Exercise/Food IDs |
| Training | Members, Measurements (when configured), Exercise Library, RBAC | member context + canonical exercises | plan versions/sessions |
| Nutrition | Members, Measurements, Food Library, RBAC, Calculation Engine | context + canonical foods | plan versions/meal logs |
| CRM | Members, RBAC | leads/activities | optional member conversion |
| Finance | Members/Memberships and Store where linked | payment/expense events | financial reports/closing |
| Store/Inventory | Products/Suppliers, RBAC | sales/purchases/stock ledger | POS/COGS/profit inputs |
| Classes | Members, trainers/identity, RBAC | schedules/bookings | attendance/portal class state |
| Member Portal | Members, Published Training/Nutrition, Classes, Documents, Notifications | safe projections | member self-service views/logs |
| Reports/Print/PDF | source domain contracts, branding, adapters | authorized current/snapshot data | report/print/PDF outputs |
| Platform Operations | Control Plane, adapters | versions, targets, backup metadata | migration/backup/health/audit state |

## Boundary rules

- Training/Nutrition cannot use arbitrary text IDs from AI; they resolve canonical library IDs.
- Member Portal cannot query another Gym DB and cannot use Draft/Review plan tables as published member content.
- Finance and Store links are logical/same-Gym references; no cross-Gym financial joins.
- Platform migrations/backups may enumerate Gym DB metadata but never read operational member data as part of ordinary control-plane views.
- Reports/Print/PDF read authoritative DTOs/snapshots and never become a second business calculation authority.
- Audit is a cross-cutting dependency for all state-changing/high-risk actions, not an excuse for a shared operational database.

## Phase boundaries

Phase 3 depends directly on `10_SEED_CONTRACT.md`, library tables, and provisioning. Phase 4 foundation depends on API envelopes, DB topology, auth/RBAC, adapters, and traceability. Business vertical slices cannot be considered complete until their downstream Web/Flutter/QA/DOCS edges are integrated.

## Final dependency addendum — 2026-08-25

```text
Auth/MFA (SQL sessions)
  ├── Member-code Portal Auth ──> Member Portal / QR separation
  └── Platform/Gym authorization ──> all Gym-scoped domains

Gym settings / payment methods / currency / tax
  ├── Finance / refunds / daily close
  └── Store / sales / returns / inventory weighted-average cost

Class definitions + recurrence boundaries
  └── Sessions + capacity ──> bookings ──> FIFO waitlist ──> attendance/no-show

CRM stages + follow-ups ──> Member conversion ──> Members/Membership
StorageAdapter ──> Documents + generated Print/PDF files
In-app NotificationAdapter ──> overdue follow-ups and approved domain events
Control Plane operations ──> monitoring thresholds + backups/DR + migrations/provisioning
```

The dependency graph does not authorize cross-Gym operational joins. Every edge resolves one Gym database except explicitly Control Plane operations metadata.
