# Members Portal Relationship Contract

**Status:** Core Members contract does not modify Portal authentication; Portal policy is a future dependency

## Existing Portal authority

The Phase 2 contract defines Member Portal access as:

`Member Code -> Gym context -> scoped portal session`

It is not the Staff/Admin authentication flow from Phase 5B. The portal must remain separate from the Gym staff Member APIs and must not receive an Admin session or Admin permission implicitly.

## Phase 8 relationship

- Core Member create/read/update/archive APIs do not create Portal credentials or sessions.
- The core Member response must not expose a raw Portal access code, portal session, QR token, password, or authentication secret.
- Member Code may be displayed only under the separate approved Portal/Member privacy rules.
- The Portal may consume an approved safe Member projection; it must not call the staff/admin detail API as a substitute for a Portal contract.
- Portal authentication, code rotation/revocation, code reveal, and Portal UI are not implemented in this Members contract audit.

## Unresolved future dependency

The existing source documents do not close the Member Code generation format, length, uniqueness scope, mutability, reveal/rotation policy, or exact Portal-safe field allowlist. These are recorded as a future Portal contract dependency, not guessed in Phase 8. If the product requires Portal behavior in the Phase 8 core release, P8-G-007 must be explicitly promoted and resolved before implementation.

## Legacy reconciliation

TOP GYM's membership-code lookup and code reveal/rotate behavior is reference evidence only. Its one-database architecture, unrestricted legacy routes, and membership/payment coupling are not LogicFit authority.
