# Decision 2.07 — Store Tax

**Status:** APPROVED  
**Authority:** Phase 2 Final Gap Resolution  
**Scope:** Store sales, refunds/returns, finance summaries, and documents.

## Decision

- Default tax rate is `0%`.
- Tax is represented explicitly as a rate and calculated amount with currency and transaction snapshots.
- The schema and service are extensible for future tax activation; zero is a configuration default, not an application-wide hard-coded assumption.
- Tax changes apply according to the effective configuration for a new transaction; historical transactions retain their tax snapshot.
