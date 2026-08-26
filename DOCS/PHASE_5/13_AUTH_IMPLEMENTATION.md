# Authentication Implementation

**Status:** IMPLEMENTED — API and automated verification passing

## Authority and boundary

Authentication is implemented in the official ASP.NET Core/.NET 10 API. React and Flutter are clients only. Credential, account-status, scope, and session decisions are made by the backend.

## Flow

1. Normalize the email and validate the request shape.
2. Load the Control Plane user and password credential by normalized email.
3. Verify the password through `IPasswordHasher`.
4. Require an active user and at least one active, authorized scope.
5. Select the deterministic active Gym scope (ordered by Gym ID) when present; otherwise select the active Platform scope.
6. Create one SQL-backed opaque session. If an enabled TOTP factor exists, create an `mfa_pending` session; otherwise create a verified `staff` session.
7. Update last-login time only after a fully verified login.
8. Write a redacted success/failure audit event.

## Implementation map

| Layer | Files |
|---|---|
| API | `src/LogicFit.Api/Controllers/AuthController.cs`, `Authentication/SessionAuthenticationHandler.cs`, `Authentication/AuthApiResults.cs` |
| Application | `src/LogicFit.Application/Authentication/AuthenticationService.cs`, `AuthContracts.cs` |
| Infrastructure | `src/LogicFit.Infrastructure/Identity/SqlAuthRepository.cs`, `Security/Pbkdf2PasswordHasher.cs`, `Security/SqlSessionStore.cs` |
| Domain authority | `src/LogicFit.Domain/Constants/PermissionCatalog.cs`, approved value objects/policies |
| Web | `apps/web/src/components/LoginPage.tsx`, `apps/web/src/lib/auth.tsx`, `apps/web/src/lib/api.ts` |
| Flutter | `apps/mobile/lib/main.dart`, `apps/mobile/lib/auth.dart` |
| Tests | `tests/LogicFit.ApiTests/AuthenticationApiTests.cs`, `AuthContinuationApiTests.cs`, `LogicFit.UnitTests/SecurityPrimitivesTests.cs` |

## Error behavior

Invalid credentials, inactive users, missing active scope, and invalid sessions use safe messages and do not disclose which account condition failed. Unauthenticated protected requests return the existing API error envelope with `401`.

## Scope boundary

This implementation does not include Member Portal authentication, Members, or any business module. Member-code portal access remains a later vertical slice under its approved contract.
