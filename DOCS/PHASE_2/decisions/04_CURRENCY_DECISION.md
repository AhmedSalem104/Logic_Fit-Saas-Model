# Decision 2.04 — Currency and Monetary Representation

**Status:** APPROVED  
**Authority:** Phase 2 Final Gap Resolution  
**Scope:** Finance, Store, Inventory, reports, receipts, and PDF output.

## Decision

- Default Gym currency is EGP (`EGP`).
- Currency is configurable per Gym for future extensibility; changing a setting does not rewrite historical transactions.
- Monetary amounts use SQL Server `DECIMAL(19,4)` (or a stricter compatible precision/scale approved during implementation) and never floating point.
- Currency is explicit on monetary transaction/summary contracts and is included in snapshots and exports.
- Rounding is a versioned server-side calculation concern; clients do not calculate authoritative totals.
