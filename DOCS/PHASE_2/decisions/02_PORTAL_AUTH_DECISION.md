# Decision 2.02 — Member Portal Authentication

**Status:** APPROVED  
**Authority:** Phase 2 Final Gap Resolution  
**Scope:** Member Portal access; separate from staff/platform login.

## Decision

- The Portal does not create a traditional username/password login for members.
- The approved access flow is `Member Code → validation → Gym context → secure portal session/token → Portal`.
- Member codes are validated against protected data; raw codes are not logged. Portal sessions are Gym-scoped, member-scoped, expiring, rate-limited, revocable, and audited where appropriate.
- A portal session/token must not expose or derive a public numeric Member ID. Public URLs use opaque identifiers when a URL identifier is necessary.
- Portal responses use the approved member-safe projection and never grant administrative member permissions.
- Logout and server-side revocation are supported.

## Separation from QR

QR remains governed by `16_QR_CONTRACT.md` and the approved public QR privacy contract. A QR token is not a substitute for a portal session and never expands the public-safe allowlist.
