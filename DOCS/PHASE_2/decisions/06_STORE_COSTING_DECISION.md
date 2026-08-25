# Decision 2.06 — Store Costing

**Status:** APPROVED  
**Authority:** Phase 2 Final Gap Resolution  
**Scope:** Store and Inventory.

## Decision

- Inventory costing uses Weighted Average Cost.
- Cost updates and stock movements are calculated and persisted transactionally with purchases, sales, returns, and stock adjustments.
- Historical movement cost is preserved; changing future purchase cost does not rewrite completed transactions.
- The costing service is backend authority. UI totals and client calculations are informational only.
