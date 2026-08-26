# Security Foundation and Cleanup

## Implemented foundation

- server-side SQL-backed sessions;
- password hashing abstraction with native PBKDF2 implementation;
- TOTP abstraction with native RFC 6238 implementation;
- cryptographically secure recovery-code generation;
- opaque hashed session tokens;
- request IDs and structured JSON logs;
- global error sanitization middleware;
- security headers;
- CORS allow-list configuration;
- ASP.NET Core rate-limiter foundation;
- SQL Server parameterized EF Core access;
- options validation for SQL Server and runtime session settings;
- no credentials/secrets in `.env.example`.

## Never log or return

Passwords, password hashes, reset tokens, TOTP secrets, recovery-code values, session secrets, or private member data must not enter logs or normal responses. The implemented Authentication/RBAC flow uses enumeration-safe authentication/MFA errors and redacted audit metadata.

## Cleanup result

The abandoned Fastify/Node backend, Node bcrypt/otplib/mssql adapters, Node auth/migration/seed runners, and duplicate configuration were removed. React/Web and Flutter/Mobile dependencies were not removed or modified. The only backend/runtime implementation is the .NET solution.

## Phase 5B API addendum synchronization — 2026-08-26

The approved API extension is documented in `../PHASE_2/21_AUTH_RBAC_API_CONTRACT_ADDENDUM.md`. It preserves the existing error envelope and permission keys, uses one MFA verification route for TOTP/recovery-code methods, and keeps platform security operations separate from Gym business access. Administrative MFA reset and role/permission catalog mutation are not part of Phase 5B.

## Dependency review

The approved minimum security remediation remains `System.Security.Cryptography.Xml` 9.0.18. Restore/build/test verification after the pin reports no NU1903 warning. No unrelated authentication package was added.

## Phase 5B implementation review

The custom `LogicFitSession` handler is the only runtime authentication scheme. Authorization is enforced by application permission and target-scope checks. Pending MFA challenges are bound to the authenticated pending session; challenge IDs alone are insufficient. The final live UAT must continue to verify that no secrets appear in structured logs.
