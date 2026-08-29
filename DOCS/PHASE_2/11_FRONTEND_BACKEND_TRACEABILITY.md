# Phase 2 Frontend / Backend Traceability

The traceability chain is mandatory:

```text
Screen field/action
  → REST endpoint
  → application use case
  → domain service / state machine / calculation engine
  → repository
  → SQL Server table(s)
```

The API names below are defined in `04_API_ENDPOINT_CATALOG.md`; table names are defined in `02_DATABASE_TABLE_CATALOG.md`. `CP` means Control Plane and `GYM` means the selected Gym DB.

| Screen/action | API | Use case | Service/domain authority | Repository/table(s) |
|---|---|---|---|---|
| `SYS-W-001` login | `POST /auth/login` | Authenticate user | Auth/session service | CP `iam.users`, `iam.credentials`, `iam.sessions`, `iam.mfa_factors` |
| `PA-W-002` create organization/Gym | `POST /platform/organizations`, `POST /gyms` | Register platform tenant | Organization/Gym registry service | CP `platform.organizations`, `platform.gyms`, `platform.plans` |
| `PA-W-004` start/retry provisioning | `POST /platform/provisioning`, retry | Provision Gym | Provisioning state machine + adapters | CP provisioning, DB registry, IAM, audit |
| `PA-W-005` migration preview/execute | migration endpoints | Roll out migration | Migration orchestration service | CP migrations, DB registry, backups, audit |
| `PA-W-006` backup/restore | backup/restore endpoints | Backup or restore target | Backup/restore adapter + high-risk policy | CP backup/restore/DB registry/audit |
| `PA-W-009` monitoring/audit | monitoring/audit GET | Inspect operations | Health/audit query services | CP operations health/audit |
| `MEM-W-001` search/filter | `GET /gyms/{gymId}/members` | Search members | Member query service | GYM `members.members` |
| `MEM-W-002` create/edit | member POST/PATCH | Create/update member | Member service | `members.members`, audit |
| `MEM-W-003` profile tabs | member detail/timeline + nested reads | Assemble authorized profile | Member profile query service | members, memberships, attendance, measurements, plans, documents, timeline |
| `MEM-W-004` freeze/renew/payment | membership/freeze/renew/payment endpoints | Apply membership lifecycle | Membership service + finance payment service | memberships/events/payments |
| `MEM-W-005` measurement save | measurements POST/PATCH | Record measurement | Measurement service | `members.body_measurements` |
| `CRM-W-001` lead/activity/follow-up | CRM endpoints | Manage CRM timeline | CRM service | `crm.leads`, `crm.lead_activities`, `crm.lead_follow_ups` |
| `CRM-W-001` convert | convert endpoint | Convert lead | Conversion transaction service | `crm.lead_conversions`, `crm.leads`, `members.members` |
| `FIN-W-001` payment/expense/closing | finance endpoints | Record financial event | Finance policy service | finance tables; linked membership/sale refs |
| `STORE-W-001` sale/return | store sales/returns | Complete POS operation | POS + inventory ledger + payment service | commerce sales/lines/returns, inventory movements, finance payments |
| `INV-W-001` count/adjust | inventory endpoints | Reconcile stock | Inventory ledger service | inventory counts/lines/movements |
| `LIB-W-001` exercise search | exercise/metadata GET | Resolve canonical exercise | Library query service | library exercises/lookups/mappings |
| `LIB-W-002` food search | food/metadata GET | Resolve canonical food | Food library query service | library foods/categories/units |
| `TRN-W-002` Save Draft | training POST/PATCH | Build canonical Training Draft | Training aggregate service + schema/domain validators | training plans/days/exercises |
| `TRN-W-002` Generate | training generate | Generate Draft | Context builder + deterministic/AI adapter + canonical resolver | training tables + audit; canonical library read |
| `TRN-W-003` submit/review | submit-review/review | Review Training plan | Training lifecycle service | training reviews/plans |
| `TRN-W-003` approve | approve | Approve Training plan | Approval permission/state service | reviews/audit |
| `TRN-W-003` publish | publish | Publish immutable Training version | Version/snapshot service | training versions/plans/audit |
| `TRN-W-004` log session | sessions/set logs | Log workout against version | Workout session service | workout sessions/set logs |
| `NUT-W-002` calculate | nutrition calculate | Calculate targets | `logicfit-nutrition-calculation-engine-v1.0.0` | nutrition calculations/targets/plans |
| `NUT-W-002` Save/Gerate Draft | nutrition POST/PATCH/generate | Build canonical Nutrition Draft | Nutrition aggregate + generator + canonical food resolver | nutrition plans/meals/meal foods |
| `NUT-W-003` submit/review/approve | lifecycle endpoints | Review/approve Nutrition plan | Nutrition lifecycle + permission service | nutrition reviews/plans/calculations |
| `NUT-W-003` publish | publish | Publish immutable Nutrition version | Snapshot/version service | nutrition versions/calculations/meals |
| `NUT-W-004` log meal | nutrition logs | Log consumed values | Meal log service | meal logs/food logs |
| `CLS-W-001` book/cancel | class booking endpoints | Manage class booking | Class availability/booking service | class sessions/bookings/waitlist |
| `POR-W-001/002` portal view | member-portal endpoints | Authorize member self-view | Portal projection/allowlist service | member/membership/attendance/versions/docs |
| `POR-W-003` QR lookup | `GET /qr/{token}` | Public safe QR lookup | QR verifier + privacy allowlist | GYM `members.qr_tokens`, `core.gym_context`, `core.branding` |
| `DOC-W-001` upload/download | documents endpoints | Store document metadata/version | StorageAdapter + document policy service | documents records/versions; local filesystem adapter |
| `REP-W-001` report run | report endpoints | Run/export report | Report query/adapter service | source tables + optional `reports.report_runs` |
| `PRT-W-001` print/PDF | print/pdf endpoints | Render approved source | Print/PDF adapter boundary | source entity/version + branding; optional report run |

## Screen-level completion mapping

The following screens reuse the same use cases and REST contracts; they are listed explicitly so mobile and summary screens cannot drift into a second API model.

| Screen | API / use case | Service/repository/table source |
|---|---|---|
| `SYS-W-002` App Shell | `/auth/me` + authorized overview; BootstrapAppShell | Auth/scope + dashboard query; CP or selected Gym overview tables |
| `PA-W-001` Platform Overview | `/platform/overview`; ReadPlatformOverview | Platform overview query; CP platform/operations |
| `PA-W-003` Gym Detail | `/gyms/{gymId}` + DB/monitoring; ReadGymDetail | Gym registry/DB/health; CP platform tables |
| `PA-W-007` Access Administration | access read/update contract; ManageAccess | IAM repository; CP `iam.users/roles/permissions/assignments` |
| `PA-W-008` Settings/Flags/Branding | settings/flags/Gym metadata endpoints; ManagePlatformConfiguration | CP settings/flags + Gym branding |
| `TRN-W-001` Training Plans | training plan list; ListTrainingPlans | `training.training_plans` |
| `NUT-W-001` Nutrition Plans | nutrition plan list; ListNutritionPlans | `nutrition.nutrition_plans/targets` |
| `POR-W-002` Portal Plans/Progress | member portal training/nutrition/progress; ReadMemberPublishedContent | published version/progress/log/document sources |
| `F-AUTH-001` Flutter Login | same `/auth/login` and MFA endpoints; AuthenticateUser | CP identity/session tables |
| `F-MEM-001` Flutter Member List | same members list; SearchMembers | GYM `members.members` |
| `F-MEM-002` Flutter Member Profile | same member detail/tab queries; ReadMemberProfile | GYM member linked tables |
| `F-MEM-003` Flutter Attendance | same attendance API; RecordAttendance | GYM `members.attendance_records` |
| `F-MEM-004` Flutter Measurements | same measurements API; RecordMeasurement | GYM `members.body_measurements` |
| `F-TRN-001` Flutter Training | same plan/session APIs; ReadTraining/StartSession | training plans/versions/sessions |
| `F-TRN-002` Flutter Training Review | same review/approve APIs; ReviewTraining | training plans/reviews |
| `F-TRN-003` Flutter Session | same session/set APIs; LogWorkout | training sessions/set logs |
| `F-NUT-001` Flutter Nutrition | same published/log APIs; ReadNutrition/LogMeal | nutrition versions/logs |
| `F-NUT-002` Flutter Nutrition Review | same review/approve APIs; ReviewNutrition | nutrition plans/reviews/calculations |
| `F-CRM-001` Flutter Follow-ups | same CRM APIs; ManageLeadFollowUp | CRM follow-ups/activities |
| `F-CLS-001` Flutter Booking | same class/booking APIs; BookClass | classes sessions/bookings/waitlist |
| `F-POR-001` Flutter Portal Home | same portal home; ReadMemberPortal | safe member/membership/attendance projection |
| `F-POR-002` Flutter Portal Plans | same published plans/docs; ReadMemberPublishedContent | training/nutrition versions/documents |
| `F-QR-001` Flutter QR | same public `/qr/{token}`; PublicQrLookup | QR verifier + public branding |
| `F-LIB-001` Flutter Exercises | same library read; SearchExercises | library exercises/lookups/media |
| `F-LIB-002` Flutter Foods | same library read; SearchFoods | library foods/categories/units |

## Traceability enforcement

- A screen field not present in this matrix or a domain contract is a contract violation, not a new backend field guessed by a client.
- An endpoint without a use case, exact permission, Gym/CP context, and table mapping fails the Phase 2 consistency check.
- A table not present in the catalog with a documented owner/consumer fails the database check.
- Client cache keys include Gym scope and resource/version; cross-Gym cache reuse is prohibited.

## Final traceability addendum — 2026-08-25

| Screen/action | API contract | Use case / service | Repository / persisted entities |
|---|---|---|---|
| `SYS-W-001` reset/MFA | password-reset and MFA endpoints | ResetPassword / ManageTotpMfa | IAM users, credentials, sessions, reset tokens, MFA factors, recovery codes, audit |
| `POR-W-000`, `F-POR-000` portal access | `POST /member-portal/access`, logout | ExchangeMemberCode / ManagePortalSession | Gym members, portal access codes, portal sessions, audit |
| `NOT-W-001`, `F-NOT-001` notifications | notifications list/read | ReadInAppNotifications | notifications records, related entity projection |
| `FIN-W-001` finance settings/refund | finance settings/refund endpoints | ManageFinanceSettings / RefundTransaction | finance settings, payments, refunds, audit |
| `CLS-W-001` attendance/no-show | class attendance endpoint | RecordSessionAttendance | class sessions, bookings, session attendance, audit |
| `PA-W-009` monitoring thresholds | monitoring/threshold endpoints | ManageOperationalThresholds | Control Plane monitoring thresholds, health, audit |
| `PA-W-006` backup/restore policy | backup/restore endpoints | VerifyBackup / RestoreDatabase | backup policies, backups, restores, database registry, audit |

## Phase 5B Authentication/RBAC addendum traceability — 2026-08-26

| Screen/action | API | Use case | Service/domain authority | Repository/table(s) |
|---|---|---|---|---|
| `SYS-W-001` change password | `POST /auth/password/change` | ChangeOwnPassword | Password policy + session invalidation service | CP `iam.users`, `iam.credentials`, `iam.sessions`, `audit.events` |
| `SYS-W-001` MFA recovery | existing `POST /auth/mfa/verify` with `method=recovery_code` | VerifyMfaRecoveryCode | MFA verification + one-time recovery-code service | CP `iam.mfa_factors`, `iam.mfa_recovery_codes`, `iam.sessions`, `audit.events` |
| `SYS-W-002` session security | `GET /auth/sessions`; `POST /auth/sessions/{sessionId}/revoke` | ListOwnSessions / RevokeOwnSession | Session scope/ownership service | CP `iam.sessions`, `iam.users`, `audit.events` |
| `PA-W-007` access catalog | `GET /platform/access/catalog`; `GET /platform/access/users` | ReadAccessCatalog / ListAccessUsers | Permission/scope query service | CP `iam.roles`, `iam.permissions`, `iam.role_permissions`, `iam.users`, `iam.user_gym_roles` |
| `PA-W-007` user create/status | `POST /platform/access/users`; `PATCH /platform/access/users/{userId}/status` | CreateAccessUser / ChangeUserStatus | User status/credential service | CP `iam.users`, `iam.credentials`, `iam.user_gym_roles`, `iam.sessions`, `audit.events` |
| `PA-W-007` role assign/revoke | `PUT /platform/access/users/{userId}/role-assignments/{roleId}`; revoke action | EnsureRoleAssignment / RevokeRoleAssignment | RBAC scope service | CP `iam.user_gym_roles`, `iam.roles`, `platform.gyms`, `audit.events` |

## Phase 7 provisioning traceability closure - 2026-08-29

| Requirement | API | Permission | Web | Flutter | Contract/test source |
|---|---|---|---|---|---|
| Request asynchronous provisioning | `POST /api/v1/platform/provisioning` | `platform.provision`; verified Platform scope and MFA step-up | `PA-W-002` action to `PA-W-004` | NO FLUTTER UI REQUIRED | `FLOW-PLAT-001`; Phase 7 API/lifecycle/security contracts |
| Observe provisioning operation | `GET /api/v1/platform/provisioning/{runId}` | `platform.provision`; authorized Platform operation scope | `PA-W-004` polling stepper | NO FLUTTER UI REQUIRED | Phase 7 API/lifecycle/Web contracts |
| Retry a retryable operation | `POST /api/v1/platform/provisioning/{runId}/retry` | `platform.provision`; verified MFA step-up | `PA-W-004` retry action | NO FLUTTER UI REQUIRED | Phase 7 API/recovery/security contracts |

The complete database, EF, application, audit, and test mapping for these
rows is `../PHASE_7/09_PROVISIONING_TRACEABILITY.md`. No Phase 7 API or
screen exists outside this matrix.
