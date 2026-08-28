# Phase 6 Platform Contract Traceability

**Status:** GREEN - all admitted requirements have complete contract edges

| Requirement | Database | API | Permission | Web | Flutter | Flow | Test requirement | Documentation |
|---|---|---|---|---|---|---|---|---|
| Platform overview | Control Plane organizations, Gyms, and database registry | `GET /api/v1/platform/overview` | `platform.view` | `PA-W-001` `/platform-admin` | None | Read Platform snapshot | source/scope/redaction and browser-state tests | `02`, `03`, `05`, `07` |
| Organization registry read | `platform.organizations` | `GET /api/v1/platform/organizations`; `GET /api/v1/platform/organizations/{organizationId}` | `platform.view` | `PA-W-002` `/platform-admin/organizations` | None | Search/filter/page/open | pagination, filter, scope, and redaction tests | `02`, `03`, `04`, `07` |
| Gym registry read | `platform.gyms` | `GET /api/v1/gyms`; `GET /api/v1/gyms/{gymId}` | `platform.view` | `PA-W-002`; `PA-W-003` | None | Open Gym metadata | scope, not-found, and no-operational-data tests | `02`, `03`, `04`, `07` |
| Database registry read | `platform.gym_databases` | `GET /api/v1/platform/databases`; `GET /api/v1/platform/databases/{databaseId}` | `platform.view` | `PA-W-003`; `PA-W-005` | None | Inspect safe placement/status metadata | pagination, scope, and secret-redaction tests | `02`, `03`, `04`, `07` |
| Platform monitoring snapshot | Existing health/version infrastructure plus registry tables | `GET /api/v1/platform/monitoring` | `platform.view` | `PA-W-001`; `PA-W-009` | None | Refresh snapshot | dependency-failure, source, scope, and partial-data tests | `03`, `05`, `07` |
| Feature-flag boundary | Existing `platform.feature_flags` only | No Phase 6 API | No new permission | `PA-W-008` deferred | None | Deferred configuration | future key/schema contract check | `02`, `06` |
| Platform audit architecture | One `audit.events` target shape | Audit search deferred; no Phase 6 route | `platform.audit.view` absent; no new permission | Audit panel deferred | None | Existing security-audit path | EF compatibility, append-only, and redaction regression | `02`, `04`, `10` |
| Server placement metadata | No Phase 6 server table | No Phase 6 server API | No new permission | Server UI deferred | None | Phase 7 provisioning handoff | Phase 7 placement/provisioning contract | `02`, `10` |
| Provisioning | `provisioning.*` | Provisioning routes deferred | Existing catalog is not extended | `PA-W-004` deferred | None | Phase 7 flow | Phase 7 tests | `00`, `01`, `10` |
| Members | Gym member tables | Member routes deferred | Member permissions deferred | Member screens deferred | Member screens in Phase 8 | Phase 8 flows | Phase 8 tests | `00`, `01`, `10` |

## Traceability rule

The first five rows are the only admitted Phase 6 runtime requirements. The
remaining rows document deliberate boundaries so deferred capabilities cannot
be mistaken for Phase 6 APIs, permissions, tables, or screens. Every admitted
API has an exact database source, permission, Web mapping, and test edge. No
Platform Flutter requirement exists.
