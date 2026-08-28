# Phase 6 Contract Gap Register

**Status:** GREEN - no unresolved Phase 6 contract gaps
**Resolution authority:** Approved P6-D-001 through P6-D-010

| Previous gap | Resolution | Final classification |
|---|---|---|
| Missing higher-authority files | Repository documentation is authoritative; absent files are not invented. | RESOLVED - P6-D-001 |
| Undefined Phase 6 release subset | Phase 6 is a bounded read-only Platform Foundation; provisioning and Members remain later phases. | RESOLVED - P6-D-002 |
| Server registry/table delta | Server placement metadata is a Platform boundary, while its table/API consumer is deferred to Phase 7; no Phase 6 server table/API. | RESOLVED - P6-D-003 |
| Plans/subscriptions | Commercial plan and subscription behavior is deferred; no plan table/API. | RESOLVED - P6-D-004 |
| Settings/feature-flag detail | Existing flag boundary is retained, but no speculative keys/settings/API are admitted. | RESOLVED - P6-D-005 |
| Monitoring/overview source | Snapshot is limited to existing API health and Control Plane registry metadata. | RESOLVED - P6-D-006 |
| Platform permission conflict | Existing 15-key catalog is unchanged; admitted Phase 6 routes use only `platform.view`. | RESOLVED - P6-D-007 |
| Audit schema delta | One canonical audit system; scope fields are an additive EF migration during implementation. | RESOLVED - P6-D-008 |
| Registry lifecycle | Phase 6 is read-only; future Active/Inactive mutation behavior is explicitly outside the admitted route set. | RESOLVED - P6-D-009 |
| Incomplete Platform API schemas | Exact read-only routes, DTOs, filters, errors, sources, and security rules are defined. | RESOLVED - P6-D-010 |

## Explicitly deferred by approved scope

- Platform mutations and any additional permissions;
- server placement and provisioning execution;
- commercial plans/subscriptions;
- settings/flag runtime keys and mutation;
- audit search permission/API;
- real-time/operational monitoring, backup, migration, deployment, and DR;
- Platform Flutter screens.

These are deliberate scope boundaries, not open Phase 6 decisions.
