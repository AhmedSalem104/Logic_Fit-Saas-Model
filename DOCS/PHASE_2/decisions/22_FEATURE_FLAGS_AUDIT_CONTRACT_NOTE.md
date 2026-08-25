# Contract Note 2.22 — Feature Flags and Audit

**Status:** RESOLVED AS ARCHITECTURE / IMPLEMENTATION NOTE  
**Authority:** Master Bible Control Plane and Security scope  
**Scope:** Platform flags, audit, privacy, and cross-Gym administration.

## Contract resolution

- Feature flags remain Control Plane metadata evaluated for the resolved Organization/Gym context; they do not cross the Gym database boundary or grant permissions.
- Global, Organization, and Gym values use the existing platform settings/feature-flag contract. Precedence, cache invalidation, and emergency disable mechanics are implementation notes to be fixed before Foundation implementation and must be covered by tests.
- Audit is append-only, server-generated, request/session correlated, permission-aware, and redacts secrets/PII according to the security contract. Retention/export/integrity mechanics are implementation notes; no client can mutate audit history.
- The audit boundary is not a substitute for business authorization or tenant isolation.
