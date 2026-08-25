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

Passwords, password hashes, reset tokens, TOTP secrets, recovery-code values, session secrets, or private member data must not enter logs or normal responses. Authentication error messages must be enumeration-safe when the full workflow is implemented.

## Cleanup result

The abandoned Fastify/Node backend, Node bcrypt/otplib/mssql adapters, Node auth/migration/seed runners, and duplicate configuration were removed. React/Web and Flutter/Mobile dependencies were not removed or modified. The only backend/runtime implementation is the .NET solution.

## Known review item

The current .NET restore reports NU1903 warnings for the transitive `System.Security.Cryptography.Xml` 9.0.0 package through EF tooling dependencies. This is a dependency-maintenance risk for the next release gate; it is not a second backend or an implementation blocker for this local foundation.
