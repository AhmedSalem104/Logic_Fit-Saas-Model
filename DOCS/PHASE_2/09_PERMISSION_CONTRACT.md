# Phase 2 Permission Contract

**Authority:** backend permission grants + tenant/Gym scope. Role labels are configurable profiles only.  
**Phase 1 decision:** `LOGICFIT_APPROVAL_PERMISSION_MATRIX.md` (SC-016).  
**No UI-only permissions:** every listed action is checked server-side.

## Permission grammar

`<domain>.<resource>.<action>` is a stable key. The permission catalog is stored in Control Plane `iam.permissions`; grants are evaluated for the target Gym before repository access. A role may bundle permissions, but a role name never authorizes an action by itself.

## Training and Nutrition exact catalog

| Permission | Scope | Allowed transition/action | Mandatory constraint |
|---|---|---|---|
| `training.create` | Gym/member | Create Manual/Automatic/AI/Hybrid Draft | Canonical Exercise IDs; no direct publish. |
| `training.edit` | Gym/plan | Edit Draft | Published versions immutable; rowversion required. |
| `training.submit_review` | Gym/plan | Draft→Review | Validation must pass; submitter audited. |
| `training.review` | Gym/plan | Review/return changes | Review alone cannot approve/publish. |
| `training.approve` | Gym/plan | Review→Approved | Creator cannot approve own plan when workflow required. |
| `training.publish` | Gym/plan | Approved→Published + snapshot | Explicit permission; Platform Admin has no implicit Gym grant. |
| `nutrition.create` | Gym/member | Create Manual/Automatic/AI/Hybrid Draft | Approved calculation engine and canonical Food IDs. |
| `nutrition.edit` | Gym/plan | Edit Draft | Published snapshots immutable; rowversion required. |
| `nutrition.submit_review` | Gym/plan | Draft→Review | Meals 1–12; totals/targets validation. |
| `nutrition.review` | Gym/plan | Review/return changes | Review alone cannot approve/publish. |
| `nutrition.approve` | Gym/plan | Review→Approved | Creator cannot approve own plan when workflow required. |
| `nutrition.publish` | Gym/plan | Approved→Published + snapshot | Explicit permission; formula/food versions snapshotted. |

## Gym operational permissions

| Domain | Canonical permissions | Scope/rule |
|---|---|---|
| Members | `members.read`, `members.create`, `members.update`, `members.delete`, `members.export` | Gym/member scope; delete is archive/high-risk where history exists. |
| Member QR | `members.qr.manage` | Gym/member scope; issue/rotate/revoke only; public lookup has no authenticated permission. |
| Memberships | `memberships.read`, `memberships.create`, `memberships.update`, `memberships.freeze`, `memberships.renew` | Member/membership scope; financial fields require finance permission. |
| Attendance | `attendance.read`, `attendance.check_in`, `attendance.check_out`, `attendance.manage` | Gym/member scope; backend prevents duplicate open records. |
| Measurements | `measurements.read`, `measurements.create`, `measurements.update`, `measurements.delete` | Sensitive member scope; exact audited fields only. |
| CRM | `crm.leads.read`, `crm.leads.create`, `crm.leads.update`, `crm.leads.delete`, `crm.activities.manage`, `crm.followups.manage`, `crm.convert` | Gym scope; conversion also requires `members.create`; WhatsApp is manual prepare/open. |
| Finance | `finance.read`, `finance.payments.create`, `finance.refunds.create`, `finance.expenses.read`, `finance.expenses.manage`, `finance.closing.manage`, `finance.reports.export` | Financial data filtered; refunds/closing high-risk and audited. |
| Store | `store.products.read`, `store.products.manage`, `store.suppliers.manage`, `store.purchasing.manage`, `store.pos.sell`, `store.pos.return`, `store.pos.void`, `store.expenses.manage` | Gym/store scope; return/void high-risk. |
| Inventory | `inventory.read`, `inventory.adjust`, `inventory.count.manage`, `inventory.manage` | Append-only movement; adjustments require audit and later policy. |
| Classes | `classes.read`, `classes.manage`, `classes.sessions.manage`, `classes.booking.create`, `classes.booking.manage`, `classes.attendance.manage` | Member/staff scope; one-time/weekly, capacity, FIFO waitlist, cutoff, and no-show rules are server-enforced. |
| Documents | `documents.read`, `documents.upload`, `documents.update`, `documents.delete` | Subject/member scope; StorageAdapter and privacy controls. |
| Canonical libraries | `library.read`, `library.create`, `library.update`, `library.delete` | Read canonical/custom; mutations apply only to Gym-owned extensions. |
| Reports | `reports.read`, `reports.export`, `print.execute`, `pdf.generate` | Source-domain permission still required; output has Gym scope. |
| Settings | `settings.read`, `settings.manage`, `branding.manage`, `feature_flags.manage` | Gym settings/branding; flags may be platform-scoped. |
| Portal | `portal.member.read`, `portal.member.log`, `portal.member.feedback` | Member self-scope; never replaces backend domain permissions. |
| Training plans/sessions | `training.read`, `training.sessions.read`, `training.sessions.manage` | Member/trainer scope; published version required for sessions. |
| Nutrition plans/logs | `nutrition.read`, `nutrition.logs.read`, `nutrition.logs.manage` | Member self-scope or explicitly granted coach scope. |
| Platform/Gym dashboards | `dashboard.read` | Scope-specific aggregate only; no sensitive data bypass. |

## Platform permissions

| Area | Permissions |
|---|---|
| Platform view | `platform.view`, `platform.audit.view`, `platform.logs.view`, `platform.diagnostics` |
| Organizations/Gyms | `platform.organizations.manage` |
| Servers/DBs | `platform.servers.view`, `platform.servers.manage`, `platform.databases.view`, `platform.databases.manage` |
| Provisioning | `platform.provision` |
| Migrations | `platform.migrations.view`, `platform.migrations.execute`, `platform.migrations.retry` |
| Backups/restore | `platform.backups.view`, `platform.backups.manage`, `platform.restore` |
| Deployments | `platform.deploy.view`, `platform.deploy` |
| Domains/storage | `platform.domains.manage`, `platform.storage.manage` |
| Seeds | `platform.seeds.view`, `platform.seeds.execute` |
| Access/security | `platform.security.manage` |
| Feature/settings | `platform.feature_flags.manage`, `platform.settings.manage`, `platform.white_label.manage` |
| DR | `platform.disaster_recovery` |

High-risk permissions require confirmation, reason, request ID, audit, and step-up/MFA where configured: restore, global migration, deployment, database decommission, organization deactivation/deletion, security changes, permission changes, global seed execution, POS void/refund, and published plan approval/publish as applicable.

## Role profile matrix

Profiles are configurable bundles, not authority:

| Profile | Baseline examples |
|---|---|
| Creator | `training.create/edit/submit_review`; `nutrition.create/edit/submit_review` |
| Reviewer | `training.review`; `nutrition.review` |
| Approver | `training.approve`; `nutrition.approve` |
| Publisher | `training.publish`; `nutrition.publish` |
| Gym Owner/Manager | Explicitly assigned Gym permissions only. |
| Platform Admin | Explicit Control Plane permissions only; no implicit Gym-plan approval/publish. |

## Denial rules

The backend rejects self-approval, cross-Gym IDs, missing exact permission, wrong lifecycle state, stale version, client-only claims, edits to Published snapshots, and public QR requests that attempt to include forbidden fields. All denials are safe and auditable where security policy requires.

## Final permission addendum — 2026-08-25

Permissions are canonical backend authority and are assigned to configurable roles/profiles; role names never grant implicit authority.

## Phase 5B API addendum — 2026-08-26

The Authentication/RBAC API extension uses the existing locked permission identifiers only: `auth.password.change`, `auth.mfa.verify`, `auth.sessions.view`, `auth.sessions.revoke`, and `platform.security.manage` as applicable. No new permission key is introduced. Exact route-level scope, self-role restrictions, status behavior, and platform/Gym boundaries are defined in `21_AUTH_RBAC_API_CONTRACT_ADDENDUM.md` and `decisions/21_AUTH_RBAC_API_DECISIONS.md`.

| Domain | Canonical permissions | Scope / rule |
|---|---|---|
| Authentication/session | `auth.session.manage`, `auth.password_reset.request`, `auth.password_reset.complete`, `auth.mfa.enroll`, `auth.mfa.verify`, `auth.mfa.disable`, `auth.mfa.recovery` | Server/session scope; public reset request is rate-limited and returns generic results; self-scope for MFA. |
| Member Portal | `portal.member.access`, `portal.member.logout` | Member-code exchange/session scope; portal session only; no staff permission substitution. |
| Finance settings | `finance.settings.manage` | Gym settings; currency, tax, payment methods, and credit default; audit. |
| Notifications | `notifications.read`, `notifications.manage` | Recipient/read scope or explicitly assigned management scope. |
| Monitoring | `platform.monitoring.view`, `platform.monitoring.manage` | Control Plane operational thresholds and health; no Gym data bypass. |
| Classes attendance | `classes.attendance.manage` | Selected Gym/session/member; separate attendance/no-show state. |

Training and Nutrition retain the exact permission-based approval matrix: creator may create/edit Draft and submit review; Reviewer needs `*.review`; Approver needs `*.approve`; Publisher needs `*.publish`; creator self-approval is rejected; Platform Admin has no implicit Gym plan approval/publish.

## Phase 7 provisioning permission approval - 2026-08-29

The final Phase 7 human approval adds the dedicated permission
`platform.provision` to the forward canonical contract. It authorizes only
the asynchronous Platform provisioning operation and its safe status/retry
scope; `platform.view` is not an alias and `platform.security.manage` is not
reused. The permission is critical/high-risk, Control Plane scoped, requires
the existing Phase 5B verified-MFA step-up, and is audited with the Phase 7
provisioning vocabulary.

The only approved role grant is the existing Platform role
`platform-security-admin`. No Gym role receives this permission and no new
role is created. Phase 7 implementation has now applied the approved EF/seed
change: the current runtime catalog contains 16 permissions, 3 roles, and 15
role-permission assignments. The Phase 5B baseline count remains historical
traceability; Phase 6 routes continue to require only `platform.view`.
