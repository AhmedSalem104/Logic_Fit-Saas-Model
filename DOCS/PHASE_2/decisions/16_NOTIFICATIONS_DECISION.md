# Decision 2.16 — Notifications

**Status:** APPROVED  
**Authority:** Phase 2 Final Gap Resolution  
**Scope:** In-app notifications and future delivery adapters.

## Decision

- `IN-APP` is the core LogicFit notification type and must work locally without external services.
- Email, SMS, and Push are future adapters, not first-release local requirements and never mandatory paid dependencies.
- A notification carries type, recipient, title, body, read/unread state, created time, related entity, and optional action/deep link.
- Provider adapters, retries, and delivery attempts remain separate from the in-app persistence contract.
