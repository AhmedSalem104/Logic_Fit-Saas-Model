# Phase 2 Screen Contract

Every LogicFit screen must have an inventory row in `06_WEB_SCREEN_CATALOG.md` or `07_FLUTTER_SCREEN_CATALOG.md`. A row is a contract, not an implementation task.

## Required screen fields

| Field | Contract requirement |
|---|---|
| Screen ID / name / module | Stable documentation identifier and product module. |
| Platform / route | Web or Flutter route; public/member route is explicit. |
| Actor / permission | Actor plus exact backend permission(s); role names are not authority. |
| Purpose / entry | Why the screen exists and approved entry points. |
| Layout / sections | Shell, cards, tabs, tables, drawers, modals, and responsive behavior at contract level. |
| Fields / actions | Field key, type, requiredness/default/validation, action and API. Every persisted field maps to a backend source. |
| Data / API | REST endpoint(s), use case, DB entities, and source classification. |
| States | Loading, empty, error, validation, disabled/permission denied, success, stale/concurrency where applicable. |
| RTL / responsive | Arabic/English direction, mobile behavior, table/card transformation, keyboard/accessibility expectations. |
| Print/PDF | Whether a print/PDF action exists and which immutable/current source it renders. |

## Shared screen rules

- UI hides/disables actions for usability but the backend remains the authority.
- Forms use controlled keys that match API DTO names or an explicitly documented mapper.
- Draft builders show server-calculated previews as previews; clients never publish based on a local calculation.
- Tables support approved filters/sorts only; pagination is server-side.
- Empty, error, loading, and permission-denied states are explicit, not blank screens.
- RTL applies to Arabic content, forms, tables, print previews, and PDF output; numeric values retain readable direction/formatting.
- Published Training/Nutrition screens are read-only snapshots. Editing starts a new revision.
- Public QR has no member profile surface and follows `16_QR_CONTRACT.md`.

## Field trace notation

`screen.field → API DTO → use case → table.column` is required for persisted or business-significant values. A value sourced from a calculation or adapter must name the engine/adapter and version.

## State contract

| State | Minimum behavior |
|---|---|
| Loading | Preserve shell/context, show progress, prevent duplicate mutation. |
| Empty | Explain scope/filter and provide only an authorized next action. |
| Validation error | Field-level safe error codes; preserve user input. |
| Permission denied | Explain unavailable action without leaking resource existence. |
| Server error | Safe message + request ID + retry where idempotent. |
| Stale draft | Reload/compare/retry path; never silently overwrite. |
| Success | Confirm state transition and refresh authoritative data. |

## Screen contract status

The catalogs define approved scope and source traceability. Visual design tokens and component implementation belong to the shared Foundation phase, not Phase 2.

## Final gap-resolution screen rules — 2026-08-25

- `SYS-W-001` includes login rate-limit, session-expired, logout, password-reset request/complete, and TOTP enroll/verify/disable/recovery states. Raw passwords, reset tokens, and TOTP secrets are never displayed or logged.
- `POR-W-000`/`F-POR-000` use Member Code access only. They show validation, rate-limit, expired/revoked, and scoped-session states; they do not ask for a member username/password or expose numeric Member ID.

## Phase 5B Authentication/RBAC addendum — 2026-08-26

The existing authentication screen IDs are extended by `21_AUTH_RBAC_API_CONTRACT_ADDENDUM.md`: `SYS-W-001`/`F-AUTH-001` cover password-change and MFA recovery sub-flows; `SYS-W-002`/`F-AUTH-001` may expose safe own-session listing/revocation; and `PA-W-007` remains the Web-only access administration surface for users and role assignments. No new mobile platform-administration screen is required.
- The Web and Flutter recovery sub-flows call only `POST /api/v1/auth/password-reset/request` and `POST /api/v1/auth/password-reset/complete`. No slash-separated password route exists in the approved contract.
- Finance screens show Gym currency (EGP by default), explicit tax, payment method, daily close/cash variance, and full/partial refund reason/permission states.
- Store screens show Weighted Average Cost-derived inventory information, explicit 0% default tax, configured payment methods, no credit-sale action by default, and Draft/Completed/Voided/Refunded/Partially Refunded states.
- Classes screens show recurrence boundaries, capacity, FIFO waitlist, cancellation cutoff (two-hour default), attendance, and separate no-show states. CRM screens show the six default stages and overdue follow-ups.
- Documents, notifications, reports, monitoring, and backup screens expose permission-safe metadata and server state; implementation-provider details never become UI authority.
