# Permission-Based RBAC Model

Authorization is permission-based and enforced by the server. Role names are assignment/grouping data; they are not security checks in place of permissions. Frontend checks, when added, are presentation only.

## Approved catalog

Permissions:

`auth.login`, `auth.logout`, `auth.password.change`, `auth.password.reset`, `auth.password_reset.request`, `auth.password_reset.complete`, `auth.mfa.enroll`, `auth.mfa.verify`, `auth.mfa.disable`, `auth.mfa.recovery`, `auth.sessions.view`, `auth.sessions.revoke`, `auth.session.manage`, `platform.security.manage`, `platform.view`.

Roles:

- `gym-authenticated-user` — Gym scope;
- `gym-security-admin` — Gym scope;
- `platform-security-admin` — Platform scope.

The approved catalog has 15 permissions, 3 roles, and 14 role-permission assignments. The source of the canonical .NET catalog is `src/LogicFit.Domain/Constants/PermissionCatalog.cs`.

## Assignment rules

- A user must be active and authenticated before protected permission evaluation.
- A Gym-scoped assignment is evaluated only for the resolved Gym context.
- A Platform-scoped assignment is evaluated only for an explicitly authorized platform operation.
- Cross-Gym reads, writes, session enumeration, and permission enumeration must fail server-side.
- Self-service must not grant administration capabilities.
- Role and permission changes require explicit permission and audit events when the full workflow is implemented.

The complete endpoint-level matrix is intentionally deferred to the resumed Authentication/RBAC vertical slice; no business-module permissions were added during this correction.
