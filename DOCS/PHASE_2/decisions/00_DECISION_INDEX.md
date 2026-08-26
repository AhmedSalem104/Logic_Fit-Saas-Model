# Phase 2 Final Decision Package

**Date:** 2026-08-25  
**Status:** APPROVED  
**Authority:** Phase 2 Final Gap Resolution authorization  
**Scope:** Contract decisions only; no migrations, seeds, APIs, or business UI are implemented by this package.

These records close the approved Phase 2 contract gaps. They do not modify or supersede the Master Bible, Decision Lock, Phase 1 evidence, or TOP GYM. Where a value remains implementation-specific, it is explicitly marked **IMPLEMENTATION NOTE** and is not a product rule.

| Record | Decision | Primary contract impact |
|---|---|---|
| `01_AUTH_MFA_DECISION.md` | Server sessions, SQL-backed auth, TOTP MFA, reset/recovery controls | Auth, DB, API, permissions, flows |
| `02_PORTAL_AUTH_DECISION.md` | Member-code scoped portal session | Member, portal, QR, API, screens |
| `03_FINANCE_SCOPE_DECISION.md` | Operational Gym finance, not ERP | Finance DB/API/screens/flows |
| `04_CURRENCY_DECISION.md` | EGP default; configurable Gym currency; DECIMAL money | Finance/store DB/API |
| `05_REFUNDS_DECISION.md` | Full/partial refunds with permission, reason, link, audit | Finance/store DB/API/flows |
| `06_STORE_COSTING_DECISION.md` | Weighted average cost | Store/inventory DB/API/flows |
| `07_STORE_TAX_DECISION.md` | Explicit, extensible tax; default 0% | Finance/store DB/API/screens |
| `08_CREDIT_SALES_DECISION.md` | Credit disabled by default; no first-release AR | Store DB/API/screens |
| `09_CLASSES_DECISION.md` | Class/session/booking/waitlist/no-show scope | Classes DB/API/screens/flows |
| `10_RECURRENCE_DECISION.md` | One-time and weekly recurrence only | Classes DB/API/flows |
| `11_CAPACITY_WAITLIST_DECISION.md` | Configurable capacity; FIFO waitlist | Classes DB/API/flows |
| `12_CRM_PIPELINE_DECISION.md` | Six canonical default pipeline stages | CRM DB/API/screens/seed |
| `13_CRM_FOLLOWUP_DECISION.md` | Owned, due, completable follow-ups; overdue visible | CRM DB/API/screens |
| `14_DOCUMENTS_STORAGE_DECISION.md` | StorageAdapter; local filesystem then R2 target | Documents DB/API/screens |
| `15_RETENTION_DECISION.md` | Configurable retention; seven-year default for legal/financial | Documents DB/API/operations |
| `16_NOTIFICATIONS_DECISION.md` | In-app core with provider adapters | Notifications DB/API/screens |
| `17_REPORTS_DECISION.md` | On-demand, server-side, export/print/PDF capable | Reports DB/API/screens |
| `18_MONITORING_DECISION.md` | Configurable operational signals and thresholds | Operations DB/API/screens |
| `19_BACKUP_DR_DECISION.md` | RPO ≤24h; RTO ≤4h; verified backup/restore workflow | Control Plane DB/API/flows |
| `20_PAYMENT_METHODS_DECISION.md` | Configurable Cash/Card/Bank Transfer/Other | Finance/store DB/API/seed |
| `21_FOOD_UNITS_CONTRACT_DECISION.md` | Canonical units plus explicit supported conversions only | Seed/nutrition DB/API |
| `22_FEATURE_FLAGS_AUDIT_CONTRACT_NOTE.md` | Existing Control Plane flag/audit boundaries are sufficient for contracts | Platform DB/API/security |
| `21_AUTH_RBAC_API_DECISIONS.md` | Phase 5B Authentication/RBAC API gap closure: password, MFA recovery, sessions, access users, roles, and scope | Phase 5B API/flows/screens/tests/security |

## Gate result

All former Phase 2 gaps are either resolved by an approved product decision or classified as an implementation note. No decision in this package authorizes Phase 3 or feature implementation.
