# P6-D-004 — Plans and Subscription Scope

**Status:** APPROVED

## Problem

The Phase 2 catalog mentions plans, but commercial pricing, trials,
subscriptions, billing state, and limits are not contracted for this phase.

## Existing evidence

The approval prohibits commercial billing workflows and external billing
providers in Phase 6. The current EF model has no plan table or plan
relationship needed by the admitted read-only views.

## Options

1. Add a non-commercial plan catalog now.
2. Add commercial subscription behavior now.
3. Defer plan/subscription behavior until a dedicated approved contract.

## Recommendation

**Selected: Option 3.** Phase 6 has no plan table, plan API, pricing field,
subscription state, trial state, billing state, limits workflow, or payment
gateway.

## Impact

Organization and overview DTOs contain no plan/subscription fields. Future
commercial behavior requires its own contract and does not alter the Phase 6
read-only surface.

## Affected surfaces

- **DB:** No `platform.plans` table or organization plan FK in Phase 6.
- **API:** No plan or billing route; no plan fields in current DTOs.
- **Permissions:** No plan/billing permission.
- **Web:** No plan management or billing screen.
- **Flutter:** No Platform commercial screen.
- **Tests:** Regression checks ensure no commercial provider or speculative
  billing behavior enters Phase 6.
