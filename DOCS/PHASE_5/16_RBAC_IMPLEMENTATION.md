# RBAC Implementation

**Status:** IMPLEMENTED — permission and access-administration API tests passing

## Canonical catalog

The implementation uses the locked catalog from `src/LogicFit.Domain/Constants/PermissionCatalog.cs`:

- 15 permission identifiers;
- 3 role definitions;
- 14 role-permission assignments.

No permission, role, or seed identity was added or renamed.

## Enforcement

`AuthenticationService.HasPermissionAsync` evaluates active assignments and the resolved scope. `RequirePermissionAsync` is called by each protected use case. Controllers use the standard ASP.NET Core authentication boundary, but permission and target-resource checks remain in the application layer so that UI visibility cannot become a security boundary.

## Approved administration

`platform.security.manage` authorizes only the approved Control Plane access operations:

- read access catalog;
- list access users;
- create one user with one initial role assignment;
- activate/disable a target user;
- assign/reactivate a canonical role;
- revoke a target role assignment.

Self-role changes are rejected. A Gym-scoped administrator cannot create or assign a Platform role. Platform-scoped operations must still target an explicitly authorized active Gym when a Gym scope is requested.

Role-definition CRUD, permission-definition CRUD, and administrative MFA reset remain outside Phase 5B.

## Implementation map

- `src/LogicFit.Application/Authentication/AuthenticationService.cs`
- `src/LogicFit.Infrastructure/Identity/SqlAuthRepository.cs`
- `src/LogicFit.Api/Controllers/AccessController.cs`
- `apps/web/src/components/AccessPage.tsx`
- `tests/LogicFit.ApiTests/AccessControlApiTests.cs`
- `tests/LogicFit.UnitTests/PermissionCatalogTests.cs`
