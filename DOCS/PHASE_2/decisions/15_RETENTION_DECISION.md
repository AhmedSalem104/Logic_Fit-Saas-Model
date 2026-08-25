# Decision 2.15 — Document Retention

**Status:** APPROVED  
**Authority:** Phase 2 Final Gap Resolution  
**Scope:** Documents, generated files, backups/operations where applicable.

## Decision

- Retention is configurable per deployment/Gym.
- Default retention for financial, legal, and contractual documents is seven years.
- Temporary/generated files may use a shorter configured retention.
- Files are never deleted silently. Retention, deletion, legal hold where configured, and purge actions require the applicable permission and audit evidence.
- The stored retention metadata is part of the document contract and does not imply an implementation-specific storage provider.
