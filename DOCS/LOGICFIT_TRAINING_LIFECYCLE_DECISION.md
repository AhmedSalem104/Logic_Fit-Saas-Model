# LogicFit Training Lifecycle Decision

**Decision ID:** SC-007  
**Date:** 2026-08-25  
**Status:** CLASSIFIED / LOGICFIT PRODUCT DECISION  
**TOP GYM modification:** none

## Legacy observation

TOP GYM uses generic program statuses such as `draft`, `active`, `paused`, `completed`, and `archived`. Its observed service/schema does not implement a complete human review, approval, publish, and immutable snapshot lifecycle.

## LogicFit canonical lifecycle

All creation modes produce the same canonical Training Plan and follow the same lifecycle:

```text
Draft → Review → Approval → Published → Snapshot/Version
```

- **Draft:** editable working plan.
- **Review:** structural and domain validation has run; coach review is required.
- **Approval:** an authorized human records the approval decision.
- **Published:** the approved plan is available to its intended consumers.
- **Snapshot/Version:** the exact published content is retained; a later change creates a new revision rather than mutating the published one.

Manual, Automatic, AI, and Hybrid generation all use this lifecycle. AI can suggest or refine only canonical Exercise IDs and cannot bypass validation, approval, permissions, or publish controls.

## Source mapping rule

TOP GYM `active` is not automatically equivalent to LogicFit `Published`. Legacy imports retain original status/provenance and require the LogicFit import policy to determine whether a human review is needed. No TOP GYM row is changed.

## Authority and traceability

This decision follows Master Bible `03_DOMAIN_MODULES/29_TRAINING_GENERATOR_MANUAL_AI.md`: one canonical plan, human review/approval, no AI auto-publish, and immutable published versions. Exact SQL status columns, approval role codes, and audit-event schema belong to the Phase 2 contract.

The approval authority is finalized in `LOGICFIT_APPROVAL_PERMISSION_MATRIX.md` (SC-016). The exact Training permissions are `training.create`, `training.edit`, `training.submit_review`, `training.review`, `training.approve`, and `training.publish`. Backend permission checks, tenant/Gym scope, creator/approver separation, and immutable Published snapshots are mandatory; role labels are configurable profiles rather than hard-coded authority.
