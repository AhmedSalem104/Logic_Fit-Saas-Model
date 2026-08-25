# Decision 2.17 — Reports

**Status:** APPROVED  
**Authority:** Phase 2 Final Gap Resolution  
**Scope:** Operational and platform reports.

## Decision

- Reports are primarily on-demand in the first release; a mandatory scheduled reporting engine is not included.
- Report queries run server-side and support the relevant date range, Gym scope, filters, pagination, sorting, export, print, and PDF output.
- Each report contract identifies source tables, calculation, permissions, filters, date semantics, and output shape.
- The browser must not load massive datasets merely to calculate a report. Export/PDF generation uses server-side or adapter-backed execution.
