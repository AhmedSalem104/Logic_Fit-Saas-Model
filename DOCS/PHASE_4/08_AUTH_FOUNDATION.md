# Authentication Foundation

This phase creates infrastructure only. The complete authentication/RBAC vertical slice is Phase 5.

## Implemented

- Options-validated session settings and secure session boundary.
- `SqlSessionStore` implementation using `iam.sessions`.
- Opaque session token generation and SHA-256 token-hash persistence.
- `Pbkdf2PasswordHasher` using PBKDF2-HMAC-SHA256 v1.
- `TotpService` native Authenticator App/TOTP primitive.
- Control Plane tables for credentials, sessions, TOTP factors, password reset tokens, and recovery-code hashes.

## Not implemented yet

Login, logout route, password reset flow, MFA enrollment UI, recovery workflow, role assignment UI, and complete permission enforcement. No raw token, password, or TOTP secret is returned or logged by the foundation.
