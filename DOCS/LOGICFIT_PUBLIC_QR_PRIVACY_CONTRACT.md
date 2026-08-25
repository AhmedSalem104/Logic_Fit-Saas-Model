# LogicFit Public QR Privacy Contract

**Decision ID:** SC-017  
**Status:** APPROVED  
**Approved:** 2026-08-25  
**Scope:** Public QR lookup if the LogicFit QR surface is retained; this does not modify TOP GYM.

## Public identifier

The public route is `/qr/:token`. The legacy label `/qr/:id` must not be interpreted as permission to expose a numeric member ID. The token is an opaque, cryptographically random, URL-safe value generated from at least 32 bytes of a CSPRNG. It is not derived from Member ID, phone number, name, date, sequence, or any predictable field.

LogicFit stores a verifier/hash of the token with the internal member relation; the raw token is not stored in ordinary database fields and the Member ID is never encoded in or returned from the public token. Tokens support revocation and rotation, with optional expiry. A revoked, rotated, or expired token is invalid and the previous token cannot be reused.

## Minimal public response

The default valid response is an allowlisted object containing only:

```json
{
  "qrStatus": "valid",
  "gym": { "publicName": "..." }
}
```

Invalid, revoked, and expired tokens return a generic safe status without revealing whether a member record exists. No member profile is exposed by default.

## Forbidden data

The public endpoint must never return or infer:

- numeric or internal Member ID, token hash, or database identifiers;
- phone, email, address, identity documents, or contact details;
- payments, balances, memberships, or financial history;
- measurements, health data, Training plans, Nutrition plans, or progress data;
- any other sensitive or tenant-internal field.

## Security and operational rules

- Apply tenant/Gym isolation before producing the response.
- Rate-limit lookup attempts and use generic errors.
- Send `Cache-Control: no-store` for token lookup responses.
- Never write raw tokens to logs, analytics, URLs outside the intended route, or error messages.
- Audit token creation, rotation, revocation, expiry changes, and denied access.
- Keep the response schema allowlisted; adding a field requires a new privacy review.

## Required tests

The later Member/Portal vertical slice must test token non-predictability, numeric-ID rejection, revoke/rotate/expiry behavior, generic invalid responses, forbidden-field absence, rate limiting, no-store headers, cross-Gym isolation, and audit coverage.
