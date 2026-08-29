# Members Permission Contract

**Status:** GREEN — permission and role grants closed
**Authorization system:** Phase 5B RBAC; no second system

## Permissions

| Permission | Scope | Operations |
|---|---|---|
| `members.read` | Authorized Gym | List, detail, timeline |
| `members.create` | Authorized Gym | Create |
| `members.update` | Authorized Gym | PUT profile/status update |
| `members.delete` | Authorized Gym | Archive through DELETE; never SQL DELETE |
| `members.export` | Authorized Gym | Contracted permission; implementation deferred from Phase 8 core |

No new permission key or alias is created.

## Approved role grants

| Canonical role | Members grants |
|---|---|
| `gym-security-admin` | `members.read`, `members.create`, `members.update`, `members.delete`, `members.export` |
| `gym-authenticated-user` | `members.read` |
| `platform-security-admin` | None; no automatic Gym business-data access |

These grants apply only in the role's authorized Gym scope. The Platform Admin role continues to use Platform APIs and does not receive implicit Member access.

## Enforcement

- The backend resolves the actor, Gym, role assignment, and permission for every request.
- A client-side hidden button or route guard is not authorization.
- Cross-Gym access is denied server-side.
- Inactive user, inactive Gym, revoked role, or missing permission produces the existing canonical authorization result.
- Member Code Portal access remains separate from staff RBAC.
