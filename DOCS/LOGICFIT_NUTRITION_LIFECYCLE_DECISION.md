# LogicFit Nutrition Lifecycle Decision

**Decision ID:** SC-008; calculation authority finalized by SC-015  
**Date:** 2026-08-25  
**Status:** CLASSIFIED / LOGICFIT PRODUCT DECISION  
**TOP GYM modification:** none

## Legacy observation

TOP GYM stores diet plans with generic statuses and nutrition snapshots but has no complete Review → Approval → Published → Snapshot lifecycle.

## LogicFit canonical lifecycle

Manual, Automatic, AI, and Hybrid creation modes all produce one canonical Nutrition Plan:

```text
Draft → Review → Approval → Published → Snapshot/Version
```

- The backend is the business authority for validation, calculations, food identity, quantities, totals, and state transitions.
- AI must reference canonical Food IDs and supported nutrition values. It cannot invent IDs/values, bypass validation, approve, or publish.
- A published plan retains the exact nutrition values used at publication. Later canonical Food Library changes do not rewrite historical snapshots.
- A failed generator or AI adapter leaves the Manual Builder available; calculation/validation failure blocks publish.

## Calculation boundary

The LogicFit boundary is resolved: a centralized backend calculation service is authoritative for BMR/TDEE (if approved for the product), calorie targets, macro targets, unit conversion, meal totals, and daily totals. Frontend calculations are UX previews only.

The calculation product decision is approved in `LOGICFIT_NUTRITION_CALCULATION_ENGINE_DECISION.md` (SC-015). The first engine version is `logicfit-nutrition-calculation-engine-v1.0.0`; its verified legacy formula evidence, LogicFit rounding/validation policy, test vectors, formula-version persistence, and immutable historical snapshots are authoritative for the later Nutrition contract.

## Source mapping rule

TOP GYM manual and intelligence calculations are preserved as legacy evidence and tests/fixtures, not as competing LogicFit authorities. TOP GYM is not modified.
