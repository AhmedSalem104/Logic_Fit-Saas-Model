# Decision 2.03 — Finance Scope

**Status:** APPROVED  
**Authority:** Phase 2 Final Gap Resolution  
**Scope:** Operational Gym finance.

## Decision

LogicFit Finance includes payments, revenue, refunds, expenses, configurable payment methods, daily close, cash variance, financial summaries, reports, and audit trail. It is not a full ERP or accounting package.

The first release does not include double-entry accounting, general ledger, trial balance, balance sheet, or a full chart of accounts. Such capabilities require a future approved product decision.

All monetary values are server-calculated and persisted with explicit currency and SQL Server `DECIMAL` precision/scale. Finance remains Gym-scoped and auditable.
