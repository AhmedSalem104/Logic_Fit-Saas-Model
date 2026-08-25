# LogicFit Print/PDF Decision

**Decision ID:** SC-009  
**Date:** 2026-08-25  
**Status:** CLASSIFIED / LOGICFIT PRODUCT DECISION  
**TOP GYM modification:** none

## TOP GYM behavior

The audited TOP GYM member portal action labeled PDF calls browser `window.print()`. Other legacy print paths construct Arabic/RTL print documents and may use a client-side `html2pdf` CDN dependency. This is legacy behavior evidence, not a LogicFit failure.

## LogicFit behavior

LogicFit provides two explicit local-capable capabilities:

1. **Professional Print:** an Arabic/English, RTL-aware, responsive print document with approved fonts and layouts for training, nutrition, members, and reports.
2. **PDF generation:** a local PDF renderer that does not require a paid provider, credit card, CDN, or remote runtime asset.

Both capabilities are behind internal rendering/adaptor boundaries so the business domain does not depend on a specific library or provider. Local development must work without paid external services. Production storage/provider choices remain adapter concerns.

The exact rendering library and final font packaging are Phase 2 technical contract items; this record does not guess them. The required behavior is fixed: local, Arabic/RTL-capable, traceable to backend/API data, and testable.

## Classification

The portal label mismatch is **legacy print behavior**, not a parity failure. The LogicFit professional Print + PDF requirement is a **new LogicFit product decision**.

