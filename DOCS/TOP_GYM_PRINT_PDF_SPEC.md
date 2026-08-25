# TOP GYM Print/PDF Specification â€” Observed Behavior

**Audit date:** 2026-08-25  
**Status:** YELLOW â€” print paths exist; offline and portal semantics require review.

## Print entry points

| Output | Observed implementation |
|---|---|
| Member report | `print-enhancements.js` fetches member details, builds Arabic RTL document, opens print window. |
| Payment receipt | Same integration fetches member/payment details and builds receipt document. |
| Pricing | Fetches `/api/pricing` and builds subscription/pricing A4 document. |
| Workout plan | Fetches saved coaching system or uses draft and opens Arabic RTL A4 print window. |
| Nutrition plan | Same coaching print integration; includes targets/macros and ordered meals/items. |
| Coaching overview | Fetches `/api/clients/:id/training-overview` and prints member coaching summary. |
| Store receipt | `public/js/pages/store/store.js` builds a popup receipt and calls browser print. |
| Member Portal report | `public/js/member-portal.js` calls `window.print()`. |

Primary evidence: `public/js/integrations/print-enhancements.js`, `public/js/pages/coaching/coaching.js`, `public/js/member-portal.js`, `public/js/pages/store/store.js`.

## Document construction

The integration creates standalone HTML with `lang="ar"`, `dir="rtl"`, Arabic headings, A4 page rules, LTR isolation for numbers/dates, tables, headers, footers, and print-only CSS. Exercise print can include start/end images, instructions, tips, and common mistakes. Coaching print includes training/nutrition summaries, program version/status, targets, and rows.

`public/css/print.css` contains print media rules and standalone A4 rules used by the HTML-to-PDF path.

## PDF behavior

The admin/coaching PDF path lazy-loads:

```text
https://cdnjs.cloudflare.com/ajax/libs/html2pdf.js/0.10.1/html2pdf.bundle.min.js
```

It uses html2canvas scale 2, `useCORS`, JPEG quality `.98`, and jsPDF portrait A4 settings. The document is rendered in a hidden A4-width holder, then downloaded as a Blob/file.

This is a browser-side PDF dependency, not a backend PDF service and not a bundled offline adapter. Cairo fonts and some other UI assets also have external-resource paths.

**BLOCKED: SPECIFICATION GAP** â€” local/offline PDF behavior has not been proven without CDN/font access.

## Portal PDF mismatch

The Member Portal contains separate â€œprintâ€ and â€œPDFâ€ labels, but both handlers call `window.print()`. The portal documentation describes saving PDF through the browser print dialog. A dedicated portal-generated PDF file is therefore not evidenced.

**BLOCKED: SPECIFICATION GAP** â€” button label and implementation semantics differ; do not map it as a backend PDF contract.

## Data traceability

Observed trace for coaching print:

```text
Web event
  â†’ print-enhancements.js
  â†’ fetch /api/coaching or /api/clients/:id/training-overview
  â†’ controller
  â†’ coaching service
  â†’ SQL Server/library rows
  â†’ standalone HTML
  â†’ browser print or html2pdf Blob
```

Observed trace for member receipt:

```text
member-details-ui/app.js
  â†’ print-enhancements.js
  â†’ /api/members/:id/details
  â†’ member service/payment ledger
  â†’ receipt HTML/PDF/print
```

## QA evidence and risks

- Existing QA artifacts include print/PDF screenshots/PDFs and UI print checklists.
- No offline CDN-blocked PDF test was executed in this audit.
- Draft and saved coaching projections can differ: saved print loads richer library details; draft print uses current catalog projection.
- External fonts/CDN and browser popup/print permissions are runtime risks.
- Any LogicFit print/PDF implementation must be backed by the approved local adapter/asset policy, not inherited automatically.

## Source Consolidation Resolution - 2026-08-25

The portal PDF-named action is classified as legacy browser-print behavior, not as a LogicFit requirement failure. LogicFit requires professional local Print plus local PDF generation with Arabic/RTL support and no paid/CDN runtime dependency. See LOGICFIT_PRINT_PDF_DECISION.md.

