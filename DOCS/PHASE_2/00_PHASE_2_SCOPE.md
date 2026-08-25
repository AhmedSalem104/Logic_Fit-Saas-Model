# Phase 2 — Data / API / Screen Contracts

**Status:** CONTRACT DESIGN ONLY  
**Phase:** 2  
**Project root:** `C:\Users\B-SMART\Desktop\LogicFit`  
**Legacy source:** `C:\Users\B-SMART\gym-membership-app`  
**Implementation status:** no business code, migration, production table, or seed file created.

## Objective

Convert the approved Phase 1 evidence and Master Bible requirements into canonical LogicFit contracts before Phase 3 seed work or any feature implementation.

The package defines:

- SQL Server Control Plane and per-Gym database boundaries;
- database entities and relationships;
- shared REST conventions and endpoint contracts;
- Web and Flutter screen inventories;
- user-flow contracts;
- permission and seed contracts;
- Training, Nutrition, Member, QR, and Print/PDF domain contracts;
- frontend/backend traceability and module dependencies;
- explicit resolved decisions and implementation notes.

## Authority and classification

Every contract statement is tagged or traceable to one of these authorities:

| Tag | Meaning |
|---|---|
| `SOURCE: TOP_GYM` | Observed legacy behavior/data from the completed read-only audit. It is evidence, not target architecture authority. |
| `SOURCE: MASTER_BIBLE` | Approved product scope, architecture, or implementation contract. |
| `DECISION: SC-*` | Approved Phase 1 LogicFit decision record. |
| `ARCHITECTURE REQUIREMENT` | Required to enforce SQL Server, Control Plane, DB-per-Gym, tenant isolation, backend authority, or client parity. |
| `LOGICFIT PRODUCT REQUIREMENT` | Approved LogicFit capability that is absent or incomplete in TOP GYM. |
| `NEW FEATURE` | LogicFit scope with no legacy behavior to copy. |
| `IMPLEMENTATION NOTE` | A technical choice needed to implement the contract but not a new business rule. |
| `RESOLVED` / `PRODUCT DECISION` / `IMPLEMENTATION NOTE` | Final Phase 2 classification used by the synchronized gap register. |

## Non-goals

This phase does not:

- create React business screens, Flutter business screens, backend business APIs, repositories, services, migrations, tables, or seed data;
- repair or modify TOP GYM;
- choose paid providers, production infrastructure, PDF libraries, or RPO/RTO values not approved by the Master Bible;
- turn TOP GYM numeric IDs into LogicFit primary keys;
- treat a frontend preview as business authority.

## Canonical cross-cutting rules

1. **Database:** Microsoft SQL Server only.
2. **Isolation:** Control Plane stores platform metadata. Each Gym has a separate operational database. Application routing selects exactly one approved Gym DB for a Gym-scoped request. There is no shared operational database for all Gyms.
3. **Identifiers:** Server-generated `uniqueidentifier` primary keys are implementation contract identifiers. Canonical seed matching uses deterministic `seed_key` values; TOP GYM numeric IDs are provenance only.
4. **Time:** Persist instants as UTC `datetime2(3)`. Date-only business values use SQL `date`. A local Gym timezone is carried where a calendar or schedule requires it; no unapproved global timezone default is assumed.
5. **Audit:** Mutable records carry `created_at_utc`, `created_by_user_id`, `updated_at_utc`, `updated_by_user_id`, and SQL Server `rowversion`. Deletable records use `deleted_at_utc`/`deleted_by_user_id` or an explicit archive state. High-risk actions also create an audit event.
6. **Concurrency:** API updates use an opaque version/ETag derived from `rowversion`; stale writes return `409 CONCURRENCY_CONFLICT`.
7. **Authority:** Backend validates permissions, tenant/Gym scope, state transitions, calculations, canonical IDs, and persistence. React and Flutter are clients of the same REST API.
8. **Errors:** APIs use a stable error code, safe message, request ID, field errors where applicable, and no secrets or cross-Gym existence leaks.
9. **Canonical versus custom data:** Seeded canonical records are distinguishable from Gym-owned extensions and a normal seed run never overwrites organization-owned records.
10. **Historical integrity:** Published Training/Nutrition records are immutable snapshots. New revisions reference a new version; historical values are not recalculated after formula/library changes.
11. **Lifecycle vocabulary:** The human workflow stage is called **Approval** in product prose; the persisted canonical state enum is `approved`. The same distinction is used consistently in Training/Nutrition API, DB, screen, and flow contracts.

## Finalized contract decisions

The former Phase 2 gaps are resolved or classified in `18_PHASE_2_GAP_REGISTER.md` and the `decisions/` package. No contract silently invents legacy behavior. Values marked **IMPLEMENTATION NOTE** are technical choices that must remain behind the approved interfaces and do not block the Phase 2 gate.

## Package map

| File | Contract |
|---|---|
| `01_DATABASE_CONTRACT.md` | Database architecture and shared SQL rules |
| `02_DATABASE_TABLE_CATALOG.md` | Control Plane/Gym table catalog and fields |
| `03_API_CONTRACT.md` | REST conventions and security contract |
| `04_API_ENDPOINT_CATALOG.md` | Endpoint-by-endpoint contract |
| `05_SCREEN_CONTRACT.md` | Screen contract schema and UX states |
| `06_WEB_SCREEN_CATALOG.md` | Web screen inventory |
| `07_FLUTTER_SCREEN_CATALOG.md` | Mobile scope and mobile screen inventory |
| `08_USER_FLOW_CONTRACT.md` | End-to-end actor flows |
| `09_PERMISSION_CONTRACT.md` | Canonical server-enforced permission matrix |
| `10_SEED_CONTRACT.md` | Canonical seed datasets and identity |
| `11_FRONTEND_BACKEND_TRACEABILITY.md` | Screen/action/API/use-case/service/repository/table mapping |
| `12_MODULE_DEPENDENCY_GRAPH.md` | Cross-module order and dependency rules |
| `13_TRAINING_CONTRACT.md` | Canonical Training model and lifecycle |
| `14_NUTRITION_CONTRACT.md` | Canonical Nutrition model and calculation snapshot |
| `15_MEMBER_CONTRACT.md` | Member, membership, measurements, timeline, documents |
| `16_QR_CONTRACT.md` | Public QR token and privacy contract |
| `17_PRINT_PDF_CONTRACT.md` | Local print/PDF contract |
| `18_PHASE_2_GAP_REGISTER.md` | Resolved decisions and implementation notes |
| `PHASE_2_STATUS_REPORT.md` | Phase gate and validation result |

## Final gap-resolution status — 2026-08-25

Phase 2 is **GREEN** after the approved decision package in `decisions/00_DECISION_INDEX.md`. The package closes the previously recorded product/contract gaps without starting implementation. Authentication, portal access, finance, commerce, classes, CRM, documents/storage, notifications, reports, monitoring, backup/DR, payment methods, food-unit conversion boundaries, feature flags, and audit boundaries are now explicit contracts. Remaining technology choices are implementation notes only.
