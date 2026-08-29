# Members Permission Contract

**Status:** BLOCKED — concrete role grants are not defined by the current canonical RBAC assignments
**RBAC system:** Phase 5B; no second authorization system

## Approved permission identifiers

| Permission | Intended operation | Scope | API mapping |
|---|---|---|---|
| `members.read` | Read Member list, detail, and timeline | Authorized Gym | List, detail, timeline |
| `members.create` | Create a Member | Authorized Gym | Create |
| `members.update` | Change mutable Member profile data | Authorized Gym | Update |
| `members.delete` | History-preserving delete/archive operation | Authorized Gym | Delete/archive |
| `members.export` | Export Member data if a future endpoint is contracted | Authorized Gym | No Phase 8 core route currently exists |

These identifiers come from the locked Phase 2 permission contract and the Phase 8 initial scope. No new permission key, alias, or role is authorized.

## Role reconciliation

The concrete canonical roles remain:

- `gym-authenticated-user`
- `gym-security-admin`
- `platform-security-admin`

The current canonical runtime catalog contains no `members.*` grants. Phase 2 defines the permission identifiers and generic Gym Owner/Manager profile guidance, but it does not close exact grants for these three concrete roles.

| Role | Members grants in current canonical assignments | Phase 8 decision |
|---|---|---|
| `gym-authenticated-user` | None evidenced | Exact grant set required |
| `gym-security-admin` | None evidenced | Exact grant set required |
| `platform-security-admin` | No implicit Gym business access | Must remain denied unless explicit Gym-scoped grant is approved |

This is P8-G-002. Implementation must not seed or infer grants until it is resolved.

## Authorization rules

- Every request requires Phase 5B authentication and resolved Gym scope.
- The backend evaluates the permission for the requested Gym and operation.
- A client-provided role, permission, or Gym identifier is never trusted.
- Cross-Gym access is denied by server-side scope enforcement.
- A Platform operation and a Gym Member operation remain different security surfaces.
- The UI may hide unavailable actions but cannot authorize them.

## Export

The permission is documented because it is already canonical. No Phase 8 API catalog route currently implements export, and no export behavior, format, field set, limits, or audit contract exists. It is therefore deferred and must not be implemented as an implied list/download action.
