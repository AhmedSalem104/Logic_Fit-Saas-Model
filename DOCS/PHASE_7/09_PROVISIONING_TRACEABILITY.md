# Phase 7 - Provisioning Traceability

**Status:** GREEN - the approved provisioning traceability is complete and verified.
**Implementation:** `SqlProvisioningService`, `ProvisioningController`, EF Core migrations, `ProvisioningWorker`, and `PlatformProvisioningPage`.

| Requirement | Database | EF | Application | API | Permission | Web | Flutter | Flow | Test requirement | Documentation |
|---|---|---|---|---|---|---|---|---|---|---|
| Create a new organization/Gym operation | `platform.organizations`, `platform.gyms`, `provisioning.runs` | Additive Control Plane model/migration only | Async operation acceptance and uniqueness | `POST /api/v1/platform/provisioning` | `platform.provision`; Platform scope; verified MFA | Existing registry entry to `PA-W-004` | None | `FLOW-PLAT-001` | schema, 202, duplicate, idempotency, audit | `00`, `01`, `02`, `03`, `04`, `10` |
| Select registered placement | `platform.servers`, `provisioning.runs.server_id` | FK/index/check migration | Registry validation and reservation | Included in start/status; no placement API | `platform.provision` | Safe server status only | None | `FLOW-PLAT-001` | active/unavailable target, scope, secret redaction | `02`, `03`, `04`, `10` |
| Generate isolated database | `platform.gym_databases` | `Phase7ProvisioningFoundation` | `SqlServerDatabaseCreator` and workflow | Included in status; no database-create API | Internal step under `platform.provision` | Safe database metadata only | None | `FLOW-PLAT-001` | deterministic name, collision, ownership, partial DB | `01`, `02`, `05`, `06`, `PROVISIONING_IMPLEMENTATION` |
| Apply Gym schema | New Gym database EF history and existing `core.gym_context` | EF Core migrations only | Internal migration step | Status only | No separate migration permission/API | Step display only | None | `FLOW-PLAT-001` | migration, failure, retry, no legacy runner | `05`, `06`, `10` |
| Run canonical library seed | Gym library tables and seed version | Existing .NET seed executor | Internal seed step | Status only | No separate seed permission/API | Step display only | None | `FLOW-PLAT-001` | counts, deterministic IDs, idempotency, FK order | `05`, `06`, `10` |
| Verify new Gym | `core.gym_context`, registry versions/status | EF verification against target DB | Verification and safe failure mapping | Status/result in `GET .../{runId}` | `platform.provision` operation scope | Safe result/failure state | None | `FLOW-PLAT-001` | schema/context/seed integrity and redaction | `03`, `05`, `06` |
| Initialize first Gym Owner | `iam.users`, `iam.credentials`, `iam.user_gym_roles`, Gym `auth.gym_users`, `platform.gyms.owner_user_id` | Reuse existing auth/RBAC mappings | Existing Phase 5B user/role path | Not a public child API | Platform operation; existing `gym-security-admin` role | No secret rendering | None | `FLOW-PLAT-001` | owner uniqueness, hash-only credential, role/scope, audit | `02`, `04`, `06`, `10`, `11` |
| Activate Gym | `platform.gyms`, `platform.gym_databases`, `provisioning.runs` | Existing status constraints | Atomic final activation | Status only | `platform.provision` internal execution | Active success state | None | `FLOW-PLAT-001` | no partial Active; terminal state | `05`, `10` |
| Retry failed operation | `provisioning.runs`, `provisioning.steps` | Attempt/history indexes | Same-run, same-target controlled retry | `POST /api/v1/platform/provisioning/{runId}/retry` | `platform.provision`; verified MFA | Retry only when `retryable=true` | None | `FLOW-PLAT-001` | retry state, idempotency, concurrency, restart | `03`, `04`, `05`, `10` |
| Observe operation | `provisioning.runs`, `provisioning.steps`, safe registry joins | Read model only | Redacted status projection | `GET /api/v1/platform/provisioning/{runId}` | `platform.provision` | `PA-W-004` polling | None | `FLOW-PLAT-001` | 401/403/404, DTO, no secret leakage | `03`, `04`, `07`, `10` |
| Audit provisioning | Existing `audit.events` | No second audit schema | Existing audit writer | Emitted by start/worker/retry | Existing authorization plus system actor | Safe request ID/status only | None | `FLOW-PLAT-001` | exact event vocabulary and redaction | `04`, `05`, `10` |
| Preserve Phase 6/8 boundaries | Control Plane vs isolated Gym DB; no member tables | No Phase 8 migration | No Phase 6 reimplementation | No duplicate Phase 6 routes | `platform.view` remains Phase 6; `platform.provision` is Phase 7 | Web-only provisioning | Explicitly none | Phase dependency graph | regression/no-business-data tests | `00`, `01`, `10`, `11` |

## Code and verification references

The Control Plane model and EF migrations are under
`src/LogicFit.Infrastructure/Persistence/ControlPlane` and
`src/LogicFit.Infrastructure/Persistence/Migrations/ControlPlane`. The
application contracts are under `src/LogicFit.Application/Provisioning`; the
workflow/worker/SQL adapters are under
`src/LogicFit.Infrastructure/Provisioning`; the HTTP boundary is
`src/LogicFit.Api/Controllers/ProvisioningController.cs`; and the Web client
is `apps/web/src/components/PlatformProvisioningPage.tsx`.

Automated coverage is in `tests/LogicFit.ApiTests/ProvisioningApiTests.cs`
and the Web tests. Flutter has no Phase 7 implementation by contract and is
covered only by regression analyze/test commands.

## Orphan check

- No Phase 7 API exists outside the three routes above.
- No Phase 7 permission exists outside `platform.provision`; the role grant is
  the existing Platform role only.
- No Phase 7 Flutter screen exists.
- No Phase 3 seed dataset is changed or duplicated.
- No Phase 8 member requirement is admitted.
- The first Gym Owner maps to the existing canonical `gym-security-admin`
  role; no new role or permission is introduced.
