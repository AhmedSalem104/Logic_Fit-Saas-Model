# Phase 2 Print / PDF Contract

**Sources:** `LOGICFIT_PRINT_PDF_DECISION.md` (SC-009), TOP GYM print/PDF audit, Master Bible printing/PDF scope.  
**Status:** behavior contract defined; renderer implementation deferred.

## Capabilities

1. **Professional Print:** browser print-ready HTML/document with Arabic/English, RTL, approved fonts, A4-aware layout, and source traceability.
2. **PDF generation:** local-capable PDF output through an adapter; no paid provider, credit card, CDN, or remote runtime asset is required in local development.

The business domain depends on `PrintAdapter`/`PdfAdapter` behavior, not a chosen library. Exact renderer and font packaging are implementation notes, not business rules.

## Input/source contract

| Document | Authoritative source |
|---|---|
| Member report | Authorized member/membership/attendance/measurement DTOs; sensitive fields filtered. |
| Membership/payment receipt | Payment/membership transaction DTO and receipt identity. |
| Training plan | Published `training.training_versions` snapshot, never current mutable Draft. |
| Nutrition plan | Published `nutrition.nutrition_versions` plus calculation/food snapshots. |
| Exercise guide | Canonical exercise DTO/media references. |
| Reports | Authorized report query/run result with Gym scope. |
| Store/POS receipt | Immutable sale/return DTO and payment reference. |

No print/PDF endpoint performs business mutation or silently recalculates a historical snapshot.

## API/screen contract

- Web `PRT-W-001` opens `/gyms/{gymId}/print/{documentType}/{id}` for preview and browser print.
- PDF action calls `/gyms/{gymId}/pdf/{documentType}/{id}` and receives a local stream/reference.
- The request carries an approved document type, entity/version ID, locale, direction, and optional display settings; unknown template types fail validation.
- Authorization requires the source-domain read permission plus `print.execute` or `pdf.generate` as applicable.
- The response includes request ID and source version metadata, not internal secrets.

## Layout/accessibility contract

- Arabic and English content must render correctly; RTL is explicit, not inferred from browser locale alone.
- A4/print margins, page breaks, headers/footers, tables, exercise images, and nutrition totals are tested against approved fixtures later.
- Responsive Web preview collapses wide tables; generated PDF remains print-oriented.
- Flutter receives a share/download/open action only where the mobile screen is in scope; it does not implement a second renderer.
- Loading, unavailable renderer, missing media, forbidden, and empty states are explicit.

## Implementation notes

- Renderer library, approved font files, PDF metadata, and exact media embedding policy are `IMPLEMENTATION NOTE` items subject to the local Foundation contract.
- Reports are on-demand and server-side according to `decisions/17_REPORTS_DECISION.md`; generated-file retention follows `decisions/15_RETENTION_DECISION.md` and is configurable, with no silent deletion.
- TOP GYM's `window.print()` portal behavior is preserved as legacy evidence only; LogicFit's PDF capability is explicit and local-capable.

## Final report/finance/store print sources — 2026-08-25

Training and Nutrition print/PDF use immutable published snapshots. Member and finance outputs use authorized server DTOs and explicit currency/tax/refund snapshots. Store receipts use completed/returned sale snapshots. Reports identify source tables, calculation, permissions, filters, date semantics, and output before print/PDF rendering. No paid provider is required locally.
