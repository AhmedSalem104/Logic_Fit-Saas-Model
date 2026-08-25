# Decision 2.08 — Credit Sales

**Status:** APPROVED  
**Authority:** Phase 2 Final Gap Resolution  
**Scope:** Store/POS and finance.

## Decision

- Credit sales are disabled by default.
- The first-release UI and business workflow do not allow credit sales and do not implement accounts-receivable workflows.
- The data model may preserve a future-compatible payment-state value, but it cannot be used unless a later approved feature enables it.
- Existing members are referenced through `member_id`; a sale does not create a duplicate member.
