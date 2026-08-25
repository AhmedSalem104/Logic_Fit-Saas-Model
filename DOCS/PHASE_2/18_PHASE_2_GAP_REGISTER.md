# Phase 2 Contract Gap Register

**Status:** GREEN — all former Phase 2 gaps are resolved by approved product decisions or explicitly classified implementation notes.  
**Rule:** no decision is silently filled in another contract. No TOP GYM modification is required.

## Resolved contract normalizations

- **Seed representation:** The Phase 1 mapping's possible `food-units.json`/`nutrition-values.json` names are normalized to the higher-authority Master Bible `units.json` plus nutrition fields embedded in `foods.json`; see `10_SEED_CONTRACT.md`. This is not an open gap.
- **Lifecycle naming:** Product prose calls the human stage **Approval**; persisted/API state is `approved`; see `00_PHASE_2_SCOPE.md`, `13_TRAINING_CONTRACT.md`, and `14_NUTRITION_CONTRACT.md`. This is not two competing states.

| ID | Area | Classification | Resolution / evidence | Contract follow-up |
|---|---|---|---|---|
| CG-001 | Authentication | `PRODUCT DECISION` | Resolved by `decisions/01_AUTH_MFA_DECISION.md`: SQL-backed server sessions, secure hash adapter, reset-token handling, rate limits, invalidation, TOTP MFA, recovery, and redacted audit. | Concrete library/cost/complexity values are implementation notes; server authority is fixed. |
| CG-002 | Finance | `PRODUCT DECISION` | Resolved by `decisions/03_FINANCE_SCOPE_DECISION.md`, `decisions/04_CURRENCY_DECISION.md`, `decisions/05_REFUNDS_DECISION.md`, and `decisions/20_PAYMENT_METHODS_DECISION.md`: operational finance, EGP default/configurable currency, DECIMAL money, full/partial refunds, methods, daily close/variance. | No ERP/GL scope; server calculations and audit are required. |
| CG-003 | Store/Inventory | `PRODUCT DECISION` | Resolved by `decisions/06_STORE_COSTING_DECISION.md`, `decisions/07_STORE_TAX_DECISION.md`, `decisions/08_CREDIT_SALES_DECISION.md`, and `decisions/05_REFUNDS_DECISION.md`: Weighted Average Cost, explicit 0% tax default, credit disabled, stateful returns/refunds, transactional movements. | Unsupported conversions fail; no first-release AR or unapproved batch/expiry workflow. |
| CG-004 | Classes/Booking | `PRODUCT DECISION` | Resolved by `decisions/09_CLASSES_DECISION.md`, `decisions/10_RECURRENCE_DECISION.md`, and `decisions/11_CAPACITY_WAITLIST_DECISION.md`: one-time/weekly bounded recurrence, capacity, FIFO waitlist, two-hour default cutoff, attendance/no-show separation. | Gym schedule timezone is configuration; no extra recurrence engine. |
| CG-005 | CRM | `PRODUCT DECISION` | Resolved by `decisions/12_CRM_PIPELINE_DECISION.md` and `decisions/13_CRM_FOLLOWUP_DECISION.md`: six default stages, lead owner/contact/timeline/conversion, due follow-ups, overdue visibility, manual WhatsApp only. | Tenant stage customization is later configuration; no WhatsApp API. |
| CG-006 | Documents/Storage | `PRODUCT DECISION` | Resolved by `decisions/14_DOCUMENTS_STORAGE_DECISION.md` and `decisions/15_RETENTION_DECISION.md`: local filesystem, StorageAdapter/R2 target, metadata, permission/audit, configurable retention, seven-year default for legal/financial/contractual files. | Exact provider limits/scanning library are implementation notes; no silent deletion. |
| CG-007 | Notifications/Queues | `PRODUCT DECISION` | Resolved by `decisions/16_NOTIFICATIONS_DECISION.md`: in-app is required locally; future Email/SMS/Push are adapters and not paid local dependencies; recipient/read/related/action fields are persisted. | Retry/provider mechanics remain adapter implementation notes. |
| CG-008 | Reports | `PRODUCT DECISION` | Resolved by `decisions/17_REPORTS_DECISION.md`: on-demand server-side reports with source/calculation/permissions/date semantics, filters, paging, sorting, export, print/PDF. | No mandatory scheduled engine in first release. |
| CG-009 | Monitoring/Operations | `PRODUCT DECISION` | Resolved by `decisions/18_MONITORING_DECISION.md`: monitored health/error/latency/migration/backup/provisioning/storage signals and configurable warning/critical defaults. | Collection/escalation adapters and windows are implementation notes; thresholds are not business logic. |
| CG-010 | Backup/DR | `PRODUCT DECISION` | Resolved by `decisions/19_BACKUP_DR_DECISION.md`: RPO ≤24h, RTO ≤4h, automated/manual/verified backup, restore confirmation/reason/permission/audit/retry, local test requirement. | Media/encryption/schedule implementation is adapter/runbook work. |
| CG-011 | Food Units | `RESOLVED` | Resolved by `decisions/21_FOOD_UNITS_CONTRACT_DECISION.md` and `10_SEED_CONTRACT.md`: `units.json` destination, explicit source-backed conversions only, unsupported conversion fails. | Phase 3 extracts verified metadata only; no general-knowledge conversion. |
| CG-012 | Member Portal Auth | `PRODUCT DECISION` | Resolved by `decisions/02_PORTAL_AUTH_DECISION.md`: member-code exchange, Gym/member-scoped secure session, rate limit, revocation, logout, audit; no traditional member login. | QR remains separate under `16_QR_CONTRACT.md`. |
| CG-013 | Platform feature flags | `IMPLEMENTATION NOTE` | Resolved by `22_FEATURE_FLAGS_AUDIT_CONTRACT_NOTE.md`: Control Plane evaluation for resolved Organization/Gym context; flags never grant permission or cross Gym DB boundaries. | Precedence/cache/disable mechanics are tested Foundation implementation details. |
| CG-014 | Audit/Privacy | `IMPLEMENTATION NOTE` | Resolved by `22_FEATURE_FLAGS_AUDIT_CONTRACT_NOTE.md` plus QR/storage/auth contracts: append-only server audit, request/session correlation, secret/PII redaction, permission-aware access. | Retention/export/integrity mechanics are implementation/runbook details; no client mutation. |

## Implementation notes (not blockers)

- Exact SQL clustered-index choice, migration framework, repository framework, API framework, React/Flutter libraries, and PDF renderer are implementation notes subject to Foundation review.
- Local filesystem versus production R2 is already adapter-bound; no provider is hard-coded.
- Monitoring metric provider and queue provider are adapters; local fake providers are required.
- Seed JSON files, SQL migrations, application code, and production configuration are Phase 3+ work and were not created.
- Password-hash library/cost, TOTP library, exact rate-limit window, PDF renderer/font package, SQL migration framework, monitoring collection interval, and storage malware-scanning provider are implementation notes behind the approved contracts.

## Final handling rule

Phase 2 is GREEN. Phase 3 and later implementation must preserve the classifications above, update affected contracts/tests/docs when implementation details are selected, and must not treat a contract document as implemented behavior.
