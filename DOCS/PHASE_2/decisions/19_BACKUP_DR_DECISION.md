# Decision 2.19 — Backup and Disaster Recovery

**Status:** APPROVED  
**Authority:** Phase 2 Final Gap Resolution  
**Scope:** Control Plane and Gym database operations.

## Decision

- Production targets are RPO ≤24 hours and RTO ≤4 hours.
- The contract supports automated and manual backup, status, verification, restore procedure, audit, failure state, and retry.
- Before risky/destructive operations, an available and verified backup is required where the runbook marks it mandatory.
- Restore requires the exact permission, explicit confirmation, reason, and audit record.
- Local development must test backup, verification, restore, schema/data/migration validation, health, and smoke test before production preparation.
- Encryption, backup media, schedule, retention, and adapter implementation are implementation notes constrained by these targets and the local-first rule.
