# Security Foundation

Implemented protections:

- ASP.NET Core security headers middleware.
- Explicit CORS origin configuration.
- ASP.NET Core rate-limit foundation.
- ASP.NET Core options/configuration validation.
- Parameterized EF Core SQL Server access.
- EF Core migration safety and database-name-guarded local transition scripts.
- Hash-only session/reset/recovery persistence boundaries.
- Sanitized API error messages and development-only safe diagnostics.
- Request ID propagation without credential logging.

Security rules carried forward from approved contracts:

- No production secrets are committed.
- No passwords, access tokens, session secrets, TOTP secrets, storage credentials, or private member data are emitted in diagnostics/logs.
- Backend remains the authority for future authorization and Gym isolation.
- Local external notification providers remain disabled.
