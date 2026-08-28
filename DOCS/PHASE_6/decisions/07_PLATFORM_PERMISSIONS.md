# P6-D-007 — Platform Permission Catalog and Role Grants

**Status:** APPROVED

## Problem

Phase 2 references Platform permissions beyond the canonical 15-key catalog,
while Phase 5B established the actual seeded catalog and grants.

## Existing evidence

The canonical catalog contains 15 permissions, 3 roles, and 14 assignments.
`platform.view` is granted to `platform-security-admin`;
`platform.security.manage` is the Phase 5B access-administration permission
and is not a registry-management alias.

## Options

1. Add missing Platform permissions or aliases.
2. Admit only operations authorized by existing `platform.view` and defer
   operations without a canonical permission.

## Recommendation

**Selected: Option 2.** All admitted Phase 6 routes require authenticated
Platform scope and `platform.view`. No permission key, alias, role, or grant
is added or changed.

## Impact

The backend must enforce the permission and Platform scope. `gym-security-admin`
and `gym-authenticated-user` do not gain Platform access. Platform Admin UI
visibility is only a client-side reflection of backend authorization.

## Affected surfaces

- **DB:** Existing IAM permission/role/assignment rows remain unchanged.
- **API:** Every admitted route uses `platform.view`; no mutation route is
  admitted.
- **Permissions:** Canonical 15-key list remains unchanged.
- **Web:** Admitted Platform screens require the same permission.
- **Flutter:** No Platform UI.
- **Tests:** 401/403, platform-vs-Gym scope, and role-grant regression tests.
