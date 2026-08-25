# TOP GYM Members Specification â€” Observed Behavior

**Audit date:** 2026-08-25  
**Status:** YELLOW â€” legacy behavior captured; privacy and target-architecture gaps remain.

## Member fields and creation

Observed member fields:

| Field | Observed rule/source |
|---|---|
| `fullName` | Required; service limit 120. |
| `phone` | Required input; normalized/validated; service length 5â€“30. |
| `email` | Optional; validated up to 254. |
| `registrationDate` | Registration date. |
| `notes` | Optional; service limit 1000. |
| Membership type/plan | Required for membership creation; pricing is backend-driven. |
| Start/end date | Membership period. |
| Discount/amount due/amount paid/payment method | Financial fields validated by service; payment methods include cash/card/transfer/other. |
| Membership notes | Optional membership-specific notes. |

Evidence: `public/index.html`, `public/js/app.js`, `src/services/member-service.js`, `database/schema.sql`.

## List/search/details

`GET /api/members` supports search, status, sort, page, and page size. The service caps page size and computes membership state from dates/freezes. Observed list/dashboard statuses include active, expiring soon, expired, frozen, and inactive-related alerts.

`GET /api/members/:id/details` returns member identity, memberships, freezes, membership events, payment transactions, membership-code preview, and financial summary. Training/nutrition overview and attendance are loaded through separate endpoints in the frontend.

Evidence: `src/routes/members.routes.js`, `src/controllers/members.controller.js`, `src/services/member-service.js`, `public/js/app.js`.

## Membership lifecycle

Observed actions:

- Create member with optional membership and initial payment.
- Edit member/membership fields.
- Add membership.
- Renew membership.
- Freeze and resume membership; effective end date accounts for freeze days.
- Add payment to the transaction ledger.
- Delete member according to backend rules.
- Show receipt number and print payment receipt.

Membership state is calculated from period/freeze context. Freeze uses bounded days and a configured/observed freeze limit; the precise policy must be retained from the service and not re-created from UI labels.

## Attendance, training, nutrition, payments, documents

- Attendance is a separate API/domain and is shown from the member surface.
- Training/nutrition are linked through coaching summary/overview endpoints, not embedded in the member details response.
- Payments and receipt rows are included in member details.
- Member documents/attachments/file-storage behavior was not found in the inspected implementation.

**BLOCKED: SPECIFICATION GAP** â€” no TOP GYM evidence establishes a member documents subsystem or StorageAdapter behavior.

## Portal and privacy

The member portal uses a membership code lookup and returns a sanitized report containing membership, payment, freeze, attendance, and financial summary data. The portal does not expose internal code hashes/ciphertext or administrative notes in the observed response.

The administrative member details surface allows Owner-only membership-code reveal/resend/rotate actions, and manual WhatsApp invite behavior is present.

`/qr/:id` is a separate server-rendered route. Its authorization/privacy boundary requires review because the audit found it outside the normal protected API route path.

**BLOCKED: SPECIFICATION GAP** â€” intended QR privacy contract is not proven by legacy evidence.

## Permissions

Observed member route permission families include `members.read/create/update/delete`, membership read/create/freeze/renew, `payments.create`, and `memberships.read`. Frontend visibility is a hint; server middleware and route-permission resolution are authoritative.

## Print/PDF

Member report and payment receipt print documents are generated from member detail/payment API data. Arabic RTL styles and A4 rules are present. Portal â€œPDFâ€ is a browser print action, not a separate confirmed PDF artifact.

## Member gaps relevant to LogicFit

1. No tenant/gym isolation in the legacy single-database model.
2. No Control Plane/per-gym database mapping.
3. No native mobile member client.
4. No confirmed documents/files domain.
5. QR page privacy contract unresolved.
6. Payment and membership behavior must be re-specified against the LogicFit financial/audit rules; legacy behavior alone is not authority.

## Source Consolidation Resolution - 2026-08-25

The absence of a TOP GYM documents/files subsystem is not a TOP GYM defect against LogicFit. Documents are approved LogicFit scope in the Master Bible feature map/checklist and must use the LogicFit StorageAdapter policy. No legacy document fields or storage behavior are invented here. If the LogicFit QR surface is retained, the approved contract is `LOGICFIT_PUBLIC_QR_PRIVACY_CONTRACT.md` (SC-017): opaque random token, revocable/rotatable, minimal public-safe allowlist, and no sensitive member data.

