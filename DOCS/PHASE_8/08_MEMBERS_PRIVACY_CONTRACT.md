# Members Privacy Contract

**Status:** BLOCKED — exact list/detail/timeline DTO allowlists need closure

## Data classes

| Data | Classification | Contract treatment |
|---|---|---|
| Member ID, Gym ID, status, registration date, audit/version metadata | Operational identifier/state | Return only where required by the endpoint and authorization |
| Full name, phone, email, notes | Personal data | Gym-scoped; purpose-limited; no unnecessary list exposure |
| Membership, payment, attendance, health/measurement, training, nutrition, CRM, documents | Separate/sensitive domain data | Not part of Member-core responses; future contract and permission required |
| Portal access codes/sessions and QR tokens | Authentication or access secret | Separate contracts; never returned in Member core APIs |

## Response boundaries

The list, detail, and timeline endpoints must each have an explicit field allowlist. Phase 2 says the list uses authorized fields and the profile uses sensitive filtering, but the complete JSON allowlists are not present. This is P8-G-004/P8-G-005.

## Logging and audit

- Do not log full request bodies for Member mutations.
- Do not log authentication secrets or database infrastructure secrets.
- Audit records should identify the actor, Gym, Member, action, request ID, and safe changed-field metadata as allowed by the existing audit contract.
- Notes and contact values must not be copied into exception messages or structured logs without an approved operational need.

## Tenant and role controls

Privacy is enforced by backend authentication, effective permission, and Gym scope. Hiding a field in React or Flutter is not an authorization boundary. Platform Admin access to Platform APIs does not imply unrestricted access to Member records.

## Export

`members.export` is an existing permission identifier, but no Phase 8 export endpoint or output contract is present. Export is deferred; no download, print, or bulk endpoint may be inferred from the screen catalog.
