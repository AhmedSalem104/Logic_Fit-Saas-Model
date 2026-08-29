# Phase 6 Platform Permission Contract

**Status:** GREEN — closed without adding permissions

## Canonical Phase 6 rule

The Phase 5B catalog remains authoritative: 15 permissions, 3 roles, and 14
role-permission assignments. Phase 6 adds no permission keys, aliases, or
role grants.

## Phase 6 permission mapping

| Operation | Permission | Scope | Role grant |
|---|---|---|---|
| Platform overview | `platform.view` | Platform Control Plane | Existing `platform-security-admin` grant |
| Organization read | `platform.view` | Platform Control Plane | Existing `platform-security-admin` grant |
| Gym registry read/detail | `platform.view` | Platform Control Plane; target Gym registry ID checked server-side | Existing `platform-security-admin` grant |
| Database registry read/detail | `platform.view` | Platform Control Plane; registry metadata only | Existing `platform-security-admin` grant |
| Monitoring snapshot | `platform.view` | Platform Control Plane; existing health/registry metadata only | Existing `platform-security-admin` grant |

`platform.security.manage` remains the Phase 5B access-administration
permission. It is not an alias for registry, monitoring, settings, flags,
server, or database-management permissions.

## Existing roles

- `platform-security-admin`: existing Platform grant of
  `platform.security.manage` and `platform.view`.
- `gym-security-admin`: existing Phase 5B security assignment, but no
  `platform.view`; it cannot access Phase 6 Platform routes by role name or
  by `platform.security.manage` alone.
- `gym-authenticated-user`: no Platform permission.

No role assignment is modified. Backend checks the permission and Platform
scope; UI visibility is only a UX reflection.

## Deferred permission references

Phase 2 references such as `platform.organizations.manage`,
`platform.servers.view/manage`, `platform.databases.view/manage`,
`platform.audit.view`, `platform.diagnostics`,
`platform.feature_flags.manage`, `platform.settings.manage`, and monitoring
keys are not Phase 6 permissions. They remain deferred security/product
decisions for the phase that needs their operations. No Phase 6 endpoint may
use an alias or bypass authorization.

## Phase 7 transition note - 2026-08-29

The subsequent Phase 7 human approval authorizes the separate permission
`platform.provision` for provisioning execution. Phase 7 implementation now
seeds it for the existing `platform-security-admin` role. This does not alter
the Phase 6 contract: all eight Phase 6 read-only routes continue to require
only `platform.view`, and no Phase 6 route uses `platform.provision`.
