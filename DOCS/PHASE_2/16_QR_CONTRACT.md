# Phase 2 QR Contract

**Authority:** `LOGICFIT_PUBLIC_QR_PRIVACY_CONTRACT.md` (SC-017).  
**Status:** approved contract; implementation deferred.

## Routes

- Administrative issuance/rotation/revocation: `/api/v1/gyms/{gymId}/members/{memberId}/qr-tokens`.
- Public lookup: `/api/v1/qr/{token}` and the Web deep-link `/qr/:token`.
- Public lookup uses `GET`; this is a side-effect-free API contract choice, while token issue/rotate/revoke use authenticated mutation methods.
- `/qr/:id` is not permission to expose a numeric ID; the route parameter is an opaque token.

## Storage contract

`members.qr_tokens` stores:

```text
qr_token_id
member_id (private FK)
token_verifier_hash
token_key_version
issued_at_utc
expires_at_utc?
revoked_at_utc?
rotated_from_id?
audit fields
```

The raw token is generated from at least 32 bytes of a CSPRNG, encoded URL-safely, returned only at issuance/rotation through an authorized response, and never persisted in ordinary fields or logs. It is not derived from member ID, phone, name, date, sequence, or database identity.

## Lifecycle

```text
Issued → Valid → Revoked
              ↘ Rotated/Expired
```

- Issue requires `members.qr.manage` and member scope.
- Rotate creates a new verifier and invalidates the previous token.
- Revoke is idempotent and records actor/reason/time.
- Expired/revoked/unknown tokens have the same generic public result.
- Token creation, rotation, revocation, expiry changes, and denied access are audited without raw values.

## Public response

The public success response is allowlisted:

```json
{
  "qrStatus": "valid",
  "gym": { "publicName": "..." }
}
```

Invalid responses do not reveal record existence. The public endpoint never returns Member ID, token hash, phone, email, address, payments, memberships, measurements, Training, Nutrition, progress, documents, or other sensitive fields.

## Security contract

- Rate-limit by token/IP policy; exact numeric thresholds are an implementation/security configuration note, while rate limiting itself is mandatory.
- `Cache-Control: no-store`.
- Tenant/Gym is resolved from the verified token relation; no client Gym ID is trusted.
- No raw token in request logs, analytics, errors, or telemetry.
- Public DTO is allowlisted and schema-tested.
- Cross-Gym lookup returns the same generic invalid result.

## Final portal separation note — 2026-08-25

The `/member-portal/access` member-code exchange and its scoped portal session are not QR behavior. QR remains an opaque, revocable/rotatable token lookup with the SC-017 minimal public-safe allowlist. Neither surface exposes numeric Member ID or sensitive member data.
