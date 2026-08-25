# Decision 2.05 — Refunds and Returns

**Status:** APPROVED  
**Authority:** Phase 2 Final Gap Resolution  
**Scope:** Finance payments and Store sales.

## Decision

- Full and partial refunds are supported.
- A refund requires the exact server permission, a reason, a link to the original transaction, and an audit record.
- Completed sales are never hard-deleted. Refund and return status is represented explicitly, including `Refunded` and `Partially Refunded` where applicable.
- Store returns support full and partial quantities, refund, restocking, and audit. Inventory and financial effects are committed transactionally.
- Refund totals, tax components, currency, and remaining refundable balance are server-authoritative.
