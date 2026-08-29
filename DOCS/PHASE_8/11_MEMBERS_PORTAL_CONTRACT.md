# Members Portal Relationship Contract

**Status:** GREEN — relationship boundary closed

## Existing Portal authority

The approved Portal flow is:

`Member Code -> validation -> Gym context -> secure scoped Portal session -> Portal`

It is separate from Phase 5B staff/platform authentication. Portal sessions are Gym/member-scoped, expiring, rate-limited, revocable, and audited as defined by the existing Portal contract.

## Phase 8 relationship

- Core Member APIs do not change Portal authentication.
- Core Member create does not create a second Portal credential or session system.
- Member Code is consumed from the existing Portal access-code contract when that contract requires it; it is Gym-unique, is not a numeric database identifier, and is not mutable through normal Member PUT.
- Portal access material and raw codes are never returned as authentication secrets by the core Member API, logged, or placed in timeline metadata.
- Portal users never receive Admin Members permissions and do not call Admin Member APIs as a substitute for the Portal-safe projection.

## Phase boundary

Portal UI, code issuance/rotation/reveal, and Portal-specific API implementation remain governed by the existing Portal contract. Phase 8 only preserves the Member relationship and safe separation; it does not add Portal routes.

## Legacy reconciliation

TOP GYM membership-code behavior is reference evidence only. Its one-database routes and membership/payment coupling are not copied into LogicFit.
