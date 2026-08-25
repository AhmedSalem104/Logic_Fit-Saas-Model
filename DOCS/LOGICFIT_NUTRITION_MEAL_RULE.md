# LogicFit Nutrition Meal Cardinality Rule

**Decision ID:** SC-010  
**Date:** 2026-08-25  
**Status:** RESOLVED

## Canonical rule

- Minimum: **1 meal/day**.
- Maximum: **12 meals/day**.
- UI default/common workflow: **3–6 meals/day**.
- Users may add or remove meals while remaining within 1–12.
- Meal ordering is explicit and backend-validated.

The source prompt's `36` is interpreted as the existing audited `3–6` notation. Literal 36 is not a LogicFit domain limit.

## Conflict resolution

TOP GYM evidence showed a UI input with `min=3`, `max=6`, `value=4`, while the backend normalizer accepted 1–12. The LogicFit rule intentionally separates the domain bound from the common UI workflow: the backend remains authoritative, and the UI starts with a practical 3–6 range without preventing valid 1–12 plans.

This is a LogicFit rule; TOP GYM is not modified.

