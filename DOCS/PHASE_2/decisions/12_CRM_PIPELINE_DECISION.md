# Decision 2.12 — CRM Pipeline

**Status:** APPROVED  
**Authority:** Phase 2 Final Gap Resolution  
**Scope:** Leads and conversion.

## Decision

Canonical default stages, in order, are: `New`, `Contacted`, `Qualified`, `Trial`, `Won`, `Lost`. Stages are Gym/tenant-configurable later, but new tenants start with these defaults.

A Lead carries source, owner, status/stage, contact data, notes, activities, next follow-up, conversion state, and timeline. WhatsApp is manual prepare/open-chat only; no WhatsApp API is part of this contract.
