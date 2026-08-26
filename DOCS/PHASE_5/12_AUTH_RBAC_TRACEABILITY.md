# Phase 5B Authentication/RBAC Traceability

**Status:** IMPLEMENTED — API, direct Chrome, and Flutter runtime checks passing; final full-flow E2E/release gate pending  
**Scope:** Authentication, SQL-backed sessions, password management, TOTP MFA, RBAC, Gym isolation, user access administration, and security audit only.  
**Authority:** Locked Phase 2 API catalog and Authentication/RBAC addendum, Phase 5 decisions, and the approved .NET 10 architecture.

The implementation is API-first. React and Flutter call the ASP.NET Core API and never connect to SQL Server. The former Fastify/Node implementation is retired and is not part of the runtime path.

The only approved password-reset routes are:

- `POST /api/v1/auth/password-reset/request`
- `POST /api/v1/auth/password-reset/complete`

The slash-separated password route spelling is non-canonical and is not implemented.

## Step 0 checkpoint evidence

| Check | Result | Evidence |
|---|---|---|
| Baseline | PASS | Baseline commit `7a6842e2f7b6e85b2a2154c4ebbe027387cf7f4b`; official remote remains `https://github.com/AhmedSalem104/Logic_Fit-Saas-Model.git`. |
| Solution build | PASS | `dotnet build LogicFit.sln --nologo`; 8 projects, 0 warnings, 0 errors. |
| .NET tests | PASS | Full solution test run: Unit 5, Integration 2, API 17. |
| Web checks | PASS | `npm run typecheck`, `npm test -- --run` (6), and `npm run build`. |
| Flutter checks | PASS | `flutter analyze` and `flutter test` (2). |
| SQL/EF | PASS | Existing Control Plane/Gym databases use the official EF history; no new competing migration system was created. |
| Canonical library seed | PASS | Phase 3 identity and counts are unchanged: exercises 1,133; muscles 297; foods 367; anatomy mappings 194. |
| Auth catalog | PASS | 15 permissions, 3 roles, 14 role-permission assignments are preserved; no new permission key was added. |
| TOP GYM | PASS | `C:\Users\B-SMART\gym-membership-app` was not modified. Its pre-existing dirty state was not reverted. |

## Requirement traceability

| Requirement | Database | Domain/Application | API | Web | Flutter | Tests | Status |
|---|---|---|---|---|---|---|---|
| Credential login and account status | `iam.users`, `iam.credentials`, `iam.user_gym_roles`, `iam.sessions`, `audit.events` | `AuthenticationService.LoginAsync`, `IPasswordHasher`, `ISessionStore` | `POST /api/v1/auth/login` | `LoginPage` `/login` | `LoginScreen` `/login` | `AuthenticationApiTests`; Web login tests; Flutter validation test | IMPLEMENTED/VERIFIED |
| MFA-pending login | `iam.mfa_factors`, `iam.sessions` | `LoginAsync`, `VerifyMfaAsync` | `POST /api/v1/auth/mfa/verify` | `LoginPage` MFA state | `LoginScreen` MFA state | `AuthContinuationApiTests`; Web MFA test | IMPLEMENTED/VERIFIED |
| Session refresh/rotation | `iam.sessions` | `RefreshAsync`, `SqlSessionStore` | `POST /api/v1/auth/refresh` | `apiClient.refresh`, `AuthProvider.refresh` | API client path available through `AuthController` | `AuthContinuationApiTests` | IMPLEMENTED/VERIFIED |
| Current session logout | `iam.sessions`, `audit.events` | `LogoutAsync` | `POST /api/v1/auth/logout` | App shell sign-out | Authenticated mobile shell sign-out | `AuthenticationApiTests`; `AuthContinuationApiTests` | IMPLEMENTED/VERIFIED |
| Current actor/scopes | `iam.users`, `iam.user_gym_roles`, `iam.roles`, `iam.role_permissions`, `iam.permissions` | `ResolveSessionAsync`, `GetMeAsync`, `HasPermissionAsync` | `GET /api/v1/auth/me` | `AuthProvider`, protected route, `AuthenticatedHome` | GoRouter guard and auth state | `AuthenticationApiTests`; pending-MFA denial test | IMPLEMENTED/VERIFIED |
| Password change | `iam.credentials`, `iam.users`, `iam.sessions`, `audit.events` | `ChangePasswordAsync` | `POST /api/v1/auth/password/change` | `SecurityPage` password form | `SecurityMobileScreen` password form | `AuthContinuationApiTests` | IMPLEMENTED/VERIFIED |
| Password-reset request/complete | `iam.password_reset_tokens`, `iam.credentials`, `iam.sessions`, `audit.events` | `RequestPasswordResetAsync`, `CompletePasswordResetAsync` | Hyphenated routes only | `PasswordResetPage` | `PasswordResetScreen` | `AuthContinuationApiTests` | IMPLEMENTED/VERIFIED |
| TOTP enrollment/enablement | `iam.mfa_factors`, `audit.events` | `EnrollMfaAsync`, `VerifyMfaAsync`, AES-GCM protection | `POST /api/v1/auth/mfa/enroll`, `POST /api/v1/auth/mfa/verify` | `SecurityPage` MFA panel | `SecurityMobileScreen` MFA panel | `AuthContinuationApiTests`; unit TOTP tests | IMPLEMENTED/VERIFIED |
| TOTP disablement | `iam.mfa_factors`, `iam.mfa_recovery_codes`, `iam.sessions`, `audit.events` | `DisableMfaAsync`, step-up verification | `POST /api/v1/auth/mfa/disable` | `SecurityPage` | `SecurityMobileScreen` | `AuthContinuationApiTests` | IMPLEMENTED/VERIFIED |
| Recovery-code rotation/consumption | `iam.mfa_recovery_codes`, `iam.mfa_factors`, `audit.events` | `RegenerateRecoveryCodesAsync`, `VerifyMfaAsync` | Regeneration route plus `mfa/verify` with `method=recovery_code` | `SecurityPage`, `LoginPage` | `SecurityMobileScreen`, `LoginScreen` | `AuthContinuationApiTests`; unit recovery tests | IMPLEMENTED/VERIFIED |
| Own session list/revoke | `iam.sessions`, `audit.events` | `ListSessionsAsync`, `RevokeOwnedSessionAsync` | `GET /api/v1/auth/sessions`, `POST /api/v1/auth/sessions/{sessionId}/revoke` | `SecurityPage` session table | `SecurityMobileScreen` session list | `AuthContinuationApiTests` | IMPLEMENTED/VERIFIED |
| Permission evaluation | `iam.roles`, `iam.permissions`, `iam.role_permissions`, `iam.user_gym_roles` | `HasPermissionAsync`, `RequirePermissionAsync` | All protected endpoints | Permission-aware visibility only | GoRouter/presentation visibility only | `AccessControlApiTests`; unit catalog tests | IMPLEMENTED/VERIFIED |
| Access catalog | `iam.permissions`, `iam.roles`, `iam.role_permissions` | `GetAccessCatalogAsync` | `GET /api/v1/platform/access/catalog` | `AccessPage` | No mobile UI by contract | `AccessControlApiTests` | IMPLEMENTED/VERIFIED |
| User list/create/status | `iam.users`, `iam.credentials`, `iam.user_gym_roles`, `iam.sessions`, `audit.events` | Access service methods | `GET/POST/PATCH /api/v1/platform/access/users...` | `AccessPage` | No mobile UI by contract | `AccessControlApiTests` | IMPLEMENTED/VERIFIED |
| Role assignment/revocation | `iam.user_gym_roles`, `iam.roles`, `audit.events` | `EnsureRoleAssignmentAsync`, `RevokeRoleAssignmentAsync` | Exact role-assignment routes | `AccessPage` | No mobile UI by contract | `AccessControlApiTests` | IMPLEMENTED/VERIFIED |
| Gym isolation | Control Plane Gym registry and scoped IAM assignments | `ResolveSessionAsync`, target-scope checks, permission checks | Server-side 401/403/404 decisions | UI cannot widen scope | UI cannot widen scope | Cross-Gym and platform/Gym tests in `AccessControlApiTests` | IMPLEMENTED/VERIFIED |
| Security audit/redaction | `audit.events` | `AuditAsync`, redacted failure paths | Sensitive mutations and denials | No secret display | No secret display | Audit redaction and denial assertions | IMPLEMENTED/VERIFIED |

## Exact implemented API map

| Method | Route | Auth/permission |
|---|---|---|
| POST | `/api/v1/auth/login` | Anonymous; auth rate limit |
| POST | `/api/v1/auth/refresh` | Anonymous transport exchange; auth rate limit |
| POST | `/api/v1/auth/logout` | Current authenticated session |
| GET | `/api/v1/auth/me` | Current authenticated session |
| POST | `/api/v1/auth/mfa/verify` | MFA challenge or authenticated pending factor; MFA rate limit |
| POST | `/api/v1/auth/mfa/enroll` | `auth.mfa.enroll` |
| POST | `/api/v1/auth/mfa/disable` | `auth.mfa.disable`; step-up; MFA rate limit |
| POST | `/api/v1/auth/mfa/recovery-codes/regenerate` | `auth.mfa.recovery`; step-up; MFA rate limit |
| POST | `/api/v1/auth/password-reset/request` | Anonymous; generic response; auth rate limit |
| POST | `/api/v1/auth/password-reset/complete` | Reset-token exchange; auth rate limit |
| POST | `/api/v1/auth/password/change` | `auth.password.change`; all sessions revoked on success |
| GET | `/api/v1/auth/sessions` | `auth.sessions.view`; own authorized scope |
| POST | `/api/v1/auth/sessions/{sessionId}/revoke` | `auth.sessions.revoke`; ownership required |
| GET | `/api/v1/platform/access/catalog` | `platform.security.manage` |
| GET | `/api/v1/platform/access/users` | `platform.security.manage`; target scope |
| POST | `/api/v1/platform/access/users` | `platform.security.manage`; atomic user + initial role |
| PATCH | `/api/v1/platform/access/users/{userId}/status` | `platform.security.manage`; `If-Match` |
| PUT | `/api/v1/platform/access/users/{userId}/role-assignments/{roleId}` | `platform.security.manage`; target scope |
| POST | `/api/v1/platform/access/users/{userId}/role-assignments/{assignmentId}/revoke` | `platform.security.manage`; target scope |

## Security invariants

- Passwords use the native PBKDF2-SHA256 adapter with per-password random salt and 600,000 iterations.
- Session tokens are 32-byte opaque random values; only SHA-256 hashes are stored.
- Session resolution rejects revoked, idle-expired, absolute-expired, inactive-user, inactive-Gym, and no-active-scope sessions.
- Password change/reset revokes all existing sessions and never returns a new token.
- TOTP secrets are protected with AES-GCM using the deployment-provided 32-byte key; recovery-code hashes are stored only after generation.
- Login and MFA errors are enumeration-safe; rate limiting is enforced by ASP.NET Core middleware.
- Pending MFA verification is bound to the authenticated pending session and bearer transport; knowing a challenge ID alone cannot complete it.
- RBAC and Gym scope are enforced server-side. Web/Flutter checks are UX only.
- Audit records contain actor/target/action/result/safe reason/request ID only; secrets are excluded.

## Current gate state

The API, Web, Flutter, unit, integration, API, direct Chrome login/shell, and local Windows Flutter launch verification listed above are passing. The final Phase 5B release gate still requires a repeatable full-flow client E2E/UAT execution and a clean, reviewed Git checkpoint. No other LogicFit business module may start until that gate is explicitly closed.

## Latest verification update — 2026-08-26

The direct Chrome run expanded beyond the initial shell smoke path and covered the implemented Login, TOTP/recovery-code, password, session/security, and access-administration flows against the live ASP.NET Core API. The Web scope-selection correction was verified: a Platform actor can keep an explicitly selected authorized Gym target, and inactive role reactivation carries the approved row-version precondition. The final remaining client-side evidence is a repeatable interactive Flutter auth E2E/UAT run; the available Flutter evidence is analyzer, widget tests, and a clean Windows launch.
