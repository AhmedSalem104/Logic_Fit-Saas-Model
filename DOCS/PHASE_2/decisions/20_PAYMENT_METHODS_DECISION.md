# Decision 2.20 — Payment Methods

**Status:** APPROVED  
**Authority:** Phase 2 Final Gap Resolution  
**Scope:** Finance and Store.

## Decision

- Payment methods are configurable per Gym.
- Initial conceptual methods are `Cash`, `Card`, `Bank Transfer`, and `Other`.
- The enabled set is tenant configuration and is snapshotted on the transaction where needed for historical reporting.
- No external payment gateway is required or integrated in the first local implementation.
