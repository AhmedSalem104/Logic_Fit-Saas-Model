# Decision 2.10 — Class Recurrence

**Status:** APPROVED  
**Authority:** Phase 2 Final Gap Resolution  
**Scope:** Class session generation.

## Decision

- Supported recurrence kinds are `one_time` and `weekly`.
- Every recurring definition has explicit start and end boundaries.
- A one-time session has one scheduled occurrence. A weekly definition produces only occurrences inside its approved boundaries.
- Timezone handling follows the Gym schedule context; the implementation must not add another recurrence engine or silently create occurrences outside the boundaries.
