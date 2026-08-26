# .NET Authentication/RBAC Architecture Foundation

The authentication implementation is native .NET and follows the API-first Phase 5B vertical slice. It is limited to the approved Authentication/RBAC scope.

## Layers

- Domain: permission catalog, role catalog, session policy, and Gym-scope value objects.
- Application: current-user, session, password, TOTP, recovery-code, Gym-resolution, and seed contracts.
- Infrastructure: EF Core persistence, SQL-backed session store, PBKDF2 password hasher, RFC 6238 TOTP service, recovery-code generator, Control Plane/Gym contexts, and seed coordination.
- API: ASP.NET Core bootstrap, middleware, health/readiness/version endpoints, DI, CORS, rate limiting, error envelope, request ID, and security headers.

## Authentication authority

The future authentication workflow must be server-side and session-based. The browser/mobile client may hold only the secure session credential; it is not the authority for identity, permissions, Gym scope, or state transitions.

## Native security primitives already present

- PBKDF2-HMAC-SHA256 v1, 600,000 iterations, 16-byte random salt, 32-byte derived hash, fixed-time verification.
- TOTP provisioning and verification using a random 20-byte Base32 secret, RFC 6238 HMAC-SHA1, six digits, 30-second period, and ±1 time-step tolerance.
- Recovery codes generated from cryptographically secure random bytes and never logged.
- Session token is generated once, hashed with SHA-256 for persistence, and never returned from database queries.

The complete login, reset, MFA lifecycle, authorization handlers, audit events, and client screens are implemented in the Phase 5B files documented by `12_AUTH_RBAC_TRACEABILITY.md`. Final live browser/UAT verification remains a gate item.
