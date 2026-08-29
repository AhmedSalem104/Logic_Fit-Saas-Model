# Phase 2 API Endpoint Catalog

Each row is a contract. `CP` means Control Plane; `GYM` means the selected `/gyms/{gymId}` database; `PUBLIC` means the constrained unauthenticated QR surface. All routes use the envelope and error rules in `03_API_CONTRACT.md`.

## Authentication

| Method / route | Use case | Auth / permission / context | Request → response | Validation / errors / audit / DB |
|---|---|---|---|---|
| `POST /auth/login` | Start session | Public with rate limit | `{email,password}` → user/session DTO | Credential/MFA policy; `AUTHENTICATION_FAILED`, rate limit; audit success/failure; CP `iam.users/credentials/sessions`. |
| `POST /auth/refresh` | Rotate access session | Refresh token | `{refreshToken}` → access/session DTO | Token hash/revocation; `SESSION_INVALID`; security audit; CP sessions. |
| `POST /auth/logout` | Revoke session | Authenticated | `{sessionId}` → `{revoked:true}` | Idempotent; audit; CP sessions. |
| `GET /auth/me` | Resolve actor/scopes | Authenticated | none → user + permission scopes | No secrets; CP users/assignments. |
| `POST /auth/mfa/verify` | Complete configured TOTP challenge | Authenticated pre-session | `{challenge,code}` → session DTO | TOTP verification/rate limit; no raw secret; audit; CP MFA/sessions. |

## Platform / Control Plane

| Method / route | Use case | Auth / permission / context | Request → response | Validation / errors / audit / DB |
|---|---|---|---|---|
| `GET /platform/overview` | Platform health dashboard | `platform.view`, CP | filters → counts/health summary | Safe aggregate; CP platform/operations. |
| `GET/POST /platform/organizations` | List/create organization | `platform.view` / `platform.organizations.manage`, CP | organization DTO → resource | unique slug/name; audit create; `platform.organizations`. |
| `GET/PATCH /platform/organizations/{id}` | View/update organization | matching platform permission, CP | patch + version → resource | scope/version/status; audit; organization/plan. |
| `GET/POST /gyms` | List/create Gym registry | `platform.view` / `platform.organizations.manage`, CP | Gym DTO → Gym resource/provisioning reference | slug/org uniqueness; create starts provisioning only when explicitly requested; `platform.gyms`. |
| `GET/PATCH /gyms/{gymId}` | Manage Gym metadata/status | `platform.view` / `platform.organizations.manage`, CP | patch + version → Gym | no operational data; deactivation preserves DB; audit; Gym/registry. |
| `GET/POST /platform/servers` | View/register server metadata | `platform.servers.view/manage`, CP | server DTO → server | provider refs only; audit; `platform.servers`. |
| `GET /platform/databases` | DB registry/health list | `platform.databases.view`, CP | filters → DB registry DTOs | never credentials; `platform.gym_databases/health`. |
| `GET /platform/databases/{id}` | DB detail | `platform.databases.view`, CP | none → health/version/backup summary | safe metadata; CP DB/health/backups/migrations. |
| `POST /platform/provisioning` | Start Gym provisioning | `platform.provision`, CP | `{organization,gym,serverTarget,owner}` + required idempotency → asynchronous run (`202`) | The historical `plan` member is not accepted; server creates registry records, validates target, and audits. |
| `GET /platform/provisioning/{runId}` | Monitor provisioning | `platform.provision`, CP | none → safe run/steps result | Exact Phase 7 lifecycle, redacted failure, and no diagnostic-permission alias. |
| `POST /platform/provisioning/{runId}/retry` | Retry failed step | `platform.provision`, CP | `{reason}` + required idempotency → asynchronous retry acceptance (`202`) | Only a persisted retryable failure; same run/target; audit. |
| `GET /platform/migrations` | Migration preview/history | `platform.migrations.view`, CP | filters → run/definition list | no execution; migration tables. |
| `POST /platform/migrations/preview` | Preview compatibility | `platform.migrations.view`, CP | `{migrationKey,targetGymIds}` → compatibility report | no mutation; migration definitions/DB registry. |
| `POST /platform/migrations` | Execute rollout | `platform.migrations.execute`, explicit confirmation/reason | `{migrationKey,targetGymIds,canaryPolicy}` → migration run | backup/preflight/canary required; audit; migration/backup tables. |
| `POST /platform/migrations/{runId}/retry` | Retry failed targets | `platform.migrations.retry`, reason | `{targetIds}` → target results | idempotency/version checks; audit; migration targets. |
| `GET/POST /platform/backups` | List/request backup | `platform.backups.view/manage`, CP | backup request → backup metadata | target scope, adapter; audit; backup records. |
| `POST /platform/restores` | Restore verified backup | `platform.restore`, confirmation + reason | `{gymDatabaseId,backupId,confirmation,reason}` → restore run | high-risk prechecks; audit; restore/backups. |
| `GET /platform/monitoring` | Platform/server/DB health | `platform.view` or `platform.diagnostics`, CP | filters → health checks | safe metrics; operations health. |
| `GET /platform/audit` | Audit search | `platform.audit.view`, CP | filters/pagination → audit events | no secret metadata; CP audit. |
| `GET/PATCH /platform/feature-flags/{key}` | Read/update flags | `platform.feature_flags.manage` for write, CP | scope/value/version → flag | scope/schema/audit; feature flags. |
| `GET/PATCH /platform/settings/{key}` | Read/update platform settings | `platform.settings.manage` for write, CP | typed value/version → setting | secret exclusion/schema; settings/audit. |

## Members, membership, attendance, measurements

| Method / route | Use case | Auth / permission / context | Request → response | Validation / audit / DB |
|---|---|---|---|---|
| `GET /gyms/{gymId}/members` | List/search/filter members | `members.read`, GYM | query → paged member DTOs | authorized fields only; members. |
| `POST /gyms/{gymId}/members` | Create member | `members.create`, GYM | member create DTO → member | fullName/phone/TOPGYM field rules; audit; members. |
| `GET/PATCH/DELETE /gyms/{gymId}/members/{memberId}` | Read/update/archive member | `members.read`, `members.update`, `members.delete`, GYM | DTO + version → member | tenant/member existence; soft delete; audit; members. |
| `GET /gyms/{gymId}/members/{memberId}/timeline` | Member timeline | `members.read`, GYM | filters → timeline DTOs | financial/sensitive filtering; timeline/source tables. |
| `GET/POST /gyms/{gymId}/members/{memberId}/memberships` | List/add membership | `memberships.read`, `memberships.create`, GYM | membership DTO → membership | dates/freeze/payment linkage; membership tables. |
| `POST /gyms/{gymId}/memberships/{membershipId}/freeze` | Freeze membership | `memberships.freeze`, GYM | `{days,reason}` → membership | approved freeze rule from contract; event/audit; membership tables. |
| `POST /gyms/{gymId}/memberships/{membershipId}/renew` | Renew membership | `memberships.renew`, GYM | renewal DTO → membership/payment refs | date/payment validation; audit; membership/finance. |
| `GET/POST /gyms/{gymId}/members/{memberId}/attendance` | Read/check-in/out | `attendance.read`, `attendance.check_in`, `attendance.check_out`, GYM | attendance DTO → record | duplicate/open-session checks; attendance/audit. |
| `GET/POST/PATCH /gyms/{gymId}/members/{memberId}/measurements` | Read/create/update measurement | `measurements.read`, `measurements.create`, `measurements.update`, GYM | exact audited measurement fields → record | nonnegative/range/unit checks; body measurements. |
| `POST /gyms/{gymId}/members/{memberId}/qr-tokens` | Issue/rotate QR token | `members.qr.manage`, GYM | `{expiresAt?}` → one-time raw token response | raw token only at issuance; hash stored; audit; qr_tokens. |
| `POST /gyms/{gymId}/members/{memberId}/qr-tokens/{id}/revoke` | Revoke QR | `members.qr.manage`, GYM | `{reason}` → status | idempotent/audit; qr_tokens. |
| `GET /qr/{token}` | Minimal public QR lookup | PUBLIC, rate-limited | none → `{qrStatus,gym.publicName}` | generic invalid; no-store/allowlist; hashed qr token + Gym context. |

## Canonical libraries

| Method / route | Use case | Auth / permission / context | Request → response | Validation / audit / DB |
|---|---|---|---|---|
| `GET /gyms/{gymId}/exercises` | Search canonical/custom exercises | `library.read`, GYM | filters → exercise DTOs | active/scope/difficulty filters; library tables. |
| `POST/PATCH/DELETE /gyms/{gymId}/exercises` | Manage Gym-owned exercise extension | `library.create`, `library.update`, `library.delete`, GYM | custom DTO/version → resource | cannot mutate canonical seed; audit; exercises/lookups. |
| `GET /gyms/{gymId}/muscles` | Lookup muscles/groups/anatomy | `library.read`, GYM | filters → lookup DTOs | active canonical/custom scope; library. |
| `GET /gyms/{gymId}/foods` | Search canonical/custom foods | `library.read`, GYM | filters → food DTOs | active/unit/category; library foods. |
| `POST/PATCH/DELETE /gyms/{gymId}/foods` | Manage Gym-owned food extension | `library.create`, `library.update`, `library.delete`, GYM | custom DTO/version → resource | nutrition values/source/unit required; no canonical overwrite. |
| `GET /gyms/{gymId}/library/metadata` | Load categories/equipment/levels/units | `library.read`, GYM | none → lookup bundles | deterministic seed/version metadata. |

## Training

| Method / route | Use case | Auth / permission / context | Request → response | Validation / state/audit / DB |
|---|---|---|---|---|
| `GET /gyms/{gymId}/training/plans` | Plan list | `training.read`, GYM | member/status/goal/level/date filters → page | scope; training plans. |
| `POST /gyms/{gymId}/training/plans` | Manual Draft | `training.create`, GYM | canonical plan DTO → Draft | member/context/mode; audit; plan/days/exercises. |
| `GET/PATCH /gyms/{gymId}/training/plans/{id}` | Read/edit Draft | `training.read`, `training.edit`, GYM | plan DTO + version → plan | Draft-only edit; canonical IDs; tables. |
| `POST /gyms/{gymId}/training/plans/{id}/generate` | Automatic/AI/Hybrid Draft generation | `training.create`, GYM | mode/context/idempotency → Draft result | validator, canonical IDs, no publish; generation tables/plan. |
| `POST /gyms/{gymId}/training/plans/{id}/submit-review` | Draft → Review | `training.submit_review`, GYM | `{notes,version}` → state | required validation; review/audit. |
| `POST /gyms/{gymId}/training/plans/{id}/review` | Review outcome/return | `training.review`, GYM | `{outcome,notes,version}` → state | reviewer cannot publish by review alone; review table. |
| `POST /gyms/{gymId}/training/plans/{id}/approve` | Review → Approved | `training.approve`, GYM | `{reason,version}` → state | creator != approver; review complete; audit. |
| `POST /gyms/{gymId}/training/plans/{id}/publish` | Approved → Published snapshot | `training.publish`, GYM | `{version,confirmation}` → published version | immutable snapshot/idempotency; plan/version/audit. |
| `GET /gyms/{gymId}/training/plans/{id}/versions/{version}` | Read historical version | `training.read`, GYM | none → immutable snapshot | no mutation; version table. |
| `POST /gyms/{gymId}/training/sessions` | Start/log workout | `training.sessions.manage`, GYM | session/set DTO → session | references published version; sessions/set logs. |

## Nutrition

| Method / route | Use case | Auth / permission / context | Request → response | Validation / state/audit / DB |
|---|---|---|---|---|
| `GET /gyms/{gymId}/nutrition/plans` | Plan list | `nutrition.read`, GYM | member/status/goal/date filters → page | scope; plans. |
| `POST /gyms/{gymId}/nutrition/plans` | Create Manual Draft | `nutrition.create`, GYM | canonical plan/context → Draft | measurement/input validation; plans/targets/calculation. |
| `GET/PATCH /gyms/{gymId}/nutrition/plans/{id}` | Read/edit Draft | `nutrition.read`, `nutrition.edit`, GYM | plan DTO + version → plan | Draft-only edit; canonical foods; tables. |
| `POST /gyms/{gymId}/nutrition/plans/{id}/calculate` | Run approved engine | `nutrition.create/edit`, GYM | normalized inputs + engine version → calculation result | only approved engine v1; calculation snapshot; no UI authority. |
| `POST /gyms/{gymId}/nutrition/plans/{id}/generate` | Automatic/AI/Hybrid Draft | `nutrition.create`, GYM | mode/context/idempotency → Draft | canonical Food IDs, calculation/validation; plan/meals. |
| `POST /gyms/{gymId}/nutrition/plans/{id}/submit-review` | Draft → Review | `nutrition.submit_review`, GYM | notes/version → state | meal 1–12, totals/targets; review/audit. |
| `POST /gyms/{gymId}/nutrition/plans/{id}/review` | Review outcome/return | `nutrition.review`, GYM | outcome/notes/version → state | no approval by review alone; review table. |
| `POST /gyms/{gymId}/nutrition/plans/{id}/approve` | Review → Approved | `nutrition.approve`, GYM | reason/version → state | creator != approver; audit. |
| `POST /gyms/{gymId}/nutrition/plans/{id}/publish` | Approved → immutable snapshot | `nutrition.publish`, GYM | confirmation/version → published version | engine/food snapshots fixed; versions/audit. |
| `GET /gyms/{gymId}/nutrition/plans/{id}/versions/{version}` | Historical plan | `nutrition.read`, GYM | none → snapshot | no recalc; versions/calculation. |
| `POST /gyms/{gymId}/nutrition/plans/{id}/logs` | Log meal/food | `nutrition.logs.manage`, GYM | consumed meal/food DTO → log | version reference/quantity; logs. |

## CRM, Finance, Store, Inventory, Classes

| Method / route | Use case | Auth / permission / context | Request → response | Validation / audit / DB |
|---|---|---|---|---|
| `GET/POST/PATCH /gyms/{gymId}/crm/leads` | Lead list/create/update | `crm.leads.read`, `crm.leads.create`, `crm.leads.update`, GYM | lead DTO/query → lead/page | canonical six-stage pipeline; CRM tables/audit. |
| `POST /gyms/{gymId}/crm/leads/{id}/activities` | Record activity | `crm.activities.manage`, GYM | activity DTO → activity | actor/time; CRM audit. |
| `POST /gyms/{gymId}/crm/leads/{id}/follow-ups` | Schedule follow-up | `crm.followups.manage`, GYM | follow-up DTO → record | due/type/note/owner/completed; overdue query; CRM/audit. |
| `POST /gyms/{gymId}/crm/leads/{id}/convert` | Convert lead | `crm.convert`, `members.create`, GYM | member create ref → conversion/member | transaction/idempotency; CRM/member. |
| `POST /gyms/{gymId}/crm/leads/{id}/whatsapp/prepare` | Prepare manual WhatsApp message | `crm.activities.manage`, GYM | template/context → `{message,openUrl}` | no WhatsApp API/send; audit prepared action. |
| `GET/POST /gyms/{gymId}/finance/payments` | List/create payment | `finance.read`, `finance.payments.create`, GYM | payment DTO → payment | amount/method/member; finance/member. |
| `POST /gyms/{gymId}/finance/refunds` | Refund payment | `finance.refunds.create`, GYM | payment/refund DTO + reason → refund | full/partial; original link, currency/tax snapshot, reason, permission, audit. |
| `GET/POST/PATCH /gyms/{gymId}/finance/expenses` | Manage expenses | `finance.expenses.read`, `finance.expenses.manage`, GYM | expense DTO → expense/page | operational finance only; currency/method/audit; no ERP ledger. |
| `POST /gyms/{gymId}/finance/daily-closings` | Close business day | `finance.closing.manage`, GYM | declared totals/reason → closing | daily close/cash variance; server totals and audit. |
| `GET/POST/PATCH /gyms/{gymId}/store/products` | Product catalog | `store.products.read`, `store.products.manage`, GYM | product DTO/query → resource/page | SKU/price; commerce. |
| `GET/POST /gyms/{gymId}/store/purchases` | Purchase/receive inventory | `store.purchasing.manage`, GYM | order/receiving DTO → order | supplier/quantity/cost; commerce/inventory. |
| `POST /gyms/{gymId}/store/sales` | POS sale | `store.pos.sell`, GYM | sale/lines/payment DTO → sale | stock/price/total/idempotency; commerce/inventory/finance. |
| `POST /gyms/{gymId}/store/sales/{id}/returns` | Return/void | `store.pos.return` or `store.pos.void`, GYM | reason/lines → result | high-risk policy/audit; commerce/inventory/finance. |
| `GET/POST /gyms/{gymId}/inventory/movements` | View/adjust stock | `inventory.read`, `inventory.adjust`, GYM | movement DTO/query → movement/stock | no direct balance mutation; inventory ledger. |
| `POST /gyms/{gymId}/inventory/counts` | Physical count | `inventory.count.manage`, GYM | count DTO → count | transactional weighted-average movement; variance/audit. |
| `GET/POST/PATCH /gyms/{gymId}/classes` | Definitions/sessions | `classes.read`, `classes.manage`, GYM | class/session DTO → resource | one-time/weekly boundaries, capacity, two-hour default cutoff; classes. |
| `POST /gyms/{gymId}/classes/sessions/{id}/bookings` | Member booking/walk-in | `classes.booking.create`, `classes.booking.manage`, GYM | booking DTO → booking/waitlist | reject full unless waitlist enabled; FIFO; classes/audit. |
| `POST /gyms/{gymId}/classes/bookings/{id}/cancel` | Cancel booking | `classes.booking.manage`, GYM | reason → status | two-hour default configurable cutoff; no-show is separate attendance state; audit. |

## Member Portal, Documents, Reports, Print/PDF

| Method / route | Use case | Auth / permission / context | Request → response | Validation / audit / DB |
|---|---|---|---|---|
| `GET /member-portal/me` | Portal home/profile summary | Portal session, member scope | none → safe member DTO | sensitive allowlist; members/membership/attendance. |
| `GET /member-portal/me/training` | Published training view | Portal session | none → version snapshot | only published; training versions. |
| `GET /member-portal/me/nutrition` | Published nutrition view | Portal session | none → version snapshot | only published; nutrition versions. |
| `GET/POST /gyms/{gymId}/documents` | List/upload document metadata | `documents.read`, `documents.upload`, GYM | multipart handled by adapter or metadata DTO → document | subject/member/category/MIME/size/retention metadata; adapter/audit. |
| `GET /gyms/{gymId}/documents/{id}/download` | Authorized document access | `documents.read`, GYM | none → adapter stream/signed local response | scope/expiry; no raw storage credentials. |
| `GET/POST /gyms/{gymId}/reports/runs` | Run/list on-demand report export | `reports.read`, `reports.export`, GYM | report key/filters/date range → run/result ref | server-side source/calculation/date semantics; report_runs; no mandatory scheduler. |
| `GET /gyms/{gymId}/print/{documentType}/{id}` | Render print-ready document | relevant domain read + `print.execute`, GYM | template/version params → HTML/print DTO | source snapshot/version; no business mutation. |
| `GET /gyms/{gymId}/pdf/{documentType}/{id}` | Generate local PDF | relevant domain read + `pdf.generate`, GYM | render params → PDF stream/ref | local adapter, Arabic/RTL; renderer/font implementation note. |

## Final gap-resolution endpoint addendum — 2026-08-25

| Method / route | Use case | Auth / permission / context | Request → response | Validation / audit / DB |
|---|---|---|---|---|
| `POST /auth/password-reset/request` | Request reset | Public, rate-limited | email → generic accepted result | no enumeration; hash-only expiring token; `iam.password_reset_tokens`. |
| `POST /auth/password-reset/complete` | Complete reset | Reset-token exchange | token/new password → result | single-use/expiry/hash; session invalidation policy; audit. |
| `POST /auth/mfa/enroll` | Enroll TOTP | Authenticated, `auth.mfa.enroll` | enrollment request → protected setup challenge | verify before activation; `iam.mfa_factors`; no plaintext secret. |
| `POST /auth/mfa/disable` | Disable TOTP | Authenticated, `auth.mfa.disable` | step-up/reason → status | recovery/step-up; audit; MFA factor. |
| `POST /auth/mfa/recovery-codes/regenerate` | Rotate recovery codes | Authenticated, `auth.mfa.recovery` | step-up → one-time codes | hash-only; old codes revoked; audit. |
| `POST /member-portal/access` | Member-code portal exchange | Public policy `portal.member.access`, rate-limited; Gym derived from code | member code → scoped portal session | hash/attempt/expiry/revocation; `members.portal_access_codes/portal_sessions`; audit. |
| `POST /member-portal/logout` | Revoke portal session | Portal session/member policy `portal.member.logout` | none → revoked status | idempotent; portal session audit. |
| `GET/PATCH /gyms/{gymId}/finance/settings` | Read/update finance defaults | `finance.read` / `finance.settings.manage`, GYM | settings/version → settings | EGP default, tax 0%, methods, credit disabled; audit; finance settings. |
| `GET /gyms/{gymId}/notifications` | List in-app notifications | `notifications.read`, GYM/member scope | page/read filter → notification page | recipient scope; notifications. |
| `POST /gyms/{gymId}/notifications/{id}/read` | Mark notification read | `notifications.read`, recipient scope | none → read status | recipient ownership; audit where required. |
| `POST /gyms/{gymId}/classes/sessions/{id}/attendance` | Record attendance/no-show | `classes.attendance.manage`, GYM | member/status/time → attendance | one outcome per booking; no-show distinct; `classes.session_attendance`; audit. |
| `GET/PATCH /platform/monitoring/thresholds` | Read/update operational thresholds | `platform.monitoring.view/manage`, CP | threshold DTO/version → thresholds | configurable latency/5xx/DB/backup/migration/provisioning rules; audit; `platform.monitoring_thresholds`. |

## Phase 5B Authentication/RBAC contract addendum — 2026-08-26

`21_AUTH_RBAC_API_CONTRACT_ADDENDUM.md` is the approved, explicit extension for the Phase 5B Authentication/RBAC operations that were not fully described in the original catalog. Existing rows above remain unchanged. The addendum adds password change, own-session listing/revocation, Control Plane access catalog/user management, role assignment/revocation, and the documented recovery-code extension of `POST /auth/mfa/verify`. It adds no permission key and no business-module endpoint.

## Phase 7 provisioning route closure - 2026-08-29

The three historical provisioning rows above are the only Phase 7 public
operations. Their complete `/api/v1` schemas, asynchronous behavior,
permission, idempotency, lifecycle, error, audit, and secret-redaction rules
are finalized in `../PHASE_7/03_PROVISIONING_API_CONTRACT.md`. The canonical
routes are:

- `POST /api/v1/platform/provisioning`
- `GET /api/v1/platform/provisioning/{runId}`
- `POST /api/v1/platform/provisioning/{runId}/retry`

No `/migrate`, `/seed`, `/cancel`, database-create, or compatibility route is
admitted. Phase 7 uses `platform.provision`; the Phase 6 read-only routes
remain outside this operation set and continue to use `platform.view`.
