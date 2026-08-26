# Tenant/Gym Isolation Implementation

**Status:** IMPLEMENTED — cross-Gym and platform/Gym boundary tests passing

## Resolution model

Every authenticated request resolves:

`current user + current session + optional current Gym + active permissions`

The session Gym is selected from an active, authorized assignment during login. A Gym session sees only assignments and resources for that Gym. A platform session sees Platform assignments; platform security operations can target an explicitly authorized active Gym without granting business-data access.

## Enforcement points

- `ResolveSessionAsync` rejects inactive users, inactive Gyms, revoked/expired sessions, and users with no active assignment in the session scope.
- `GetMeAsync` returns only assignments visible in the current session and rejects an unverified pending-MFA session.
- `HasPermissionAsync` checks active Gym status and exact permission/scope.
- Access list/create/status/role operations resolve target scope server-side; caller-supplied database names or connection strings are not accepted.
- Own-session operations require both `userId` ownership and authorized scope.
- Platform and Gym role boundaries are checked before target mutation.

## Required security outcomes

| Scenario | Expected |
|---|---|
| User A + Gym A | Allowed when active assignment and permission exist |
| User A + Gym B | `403 GYM_SCOPE_DENIED` or safe absence response according to endpoint contract |
| Missing/invalid Gym target | Safe denial; no cross-Gym query path |
| Inactive Gym | Session resolution/target operation denied |
| Inactive user | Session rejected |
| Revoked role | Next session resolution/permission evaluation denied |
| Gym actor → Platform role | `403 GYM_SCOPE_DENIED` |

## Tests

`AccessControlApiTests` exercises normal-user denial, other-Gym access denial, platform/Gym role boundary denial, and authorization audit. `AuthenticationApiTests` and `AuthContinuationApiTests` cover inactive/revoked session behavior.
