# Phase 7 - Provisioning Security Contract

**Status:** GREEN - approved provisioning security controls are enforced and verified locally.
**Security authority:** existing Phase 5B authentication, SQL-backed sessions,
permission evaluation, MFA state, and single audit system.

## Authorization

| Control | Contract |
|---|---|
| Actor | Authenticated Platform Admin with Platform scope |
| Permission | `platform.provision` |
| Role grant | Existing `platform-security-admin` only; the implementation seeds the approved `platform.provision` grant |
| MFA | Start and retry require the existing Phase 5B verified-MFA session; no second authentication system |
| Gym users/owners | Always denied from Platform provisioning routes |
| Server enforcement | Permission, Platform scope, operation ownership, state, and idempotency are checked before adapter/database access |
| Client behavior | UI visibility is a convenience only; the API is authoritative |

`platform.view` authorizes Phase 6 read-only registry/overview operations and
cannot authorize provisioning. `platform.security.manage` retains its Phase
5B access-administration meaning and is not an alias.

## Threat controls

- The request cannot provide a physical database name, connection string,
  secret reference, password, private key, server credential, or existing Gym
  ID for a new Gym.
- The server generates organization, Gym, operation, database-registry, and
  database names as applicable.
- Server target is reloaded from the Control Plane registry and must be
  selectable at execution time; client labels are not trusted.
- `Idempotency-Key` and a canonical request fingerprint prevent duplicate
  operations and key reuse with different input.
- A unique active-operation constraint and the local single-reader worker
  prevent concurrent execution for one Gym in the supported local runtime.
- A retry cannot change the target, request, owner, server, or database.
- Existing database ownership is proved before a retry can reuse a partial
  database. Unknown ownership fails safely and requires operator resolution.
- SQL access remains parameterized and adapter-managed. No client-supplied
  identifiers are concatenated into queries.
- Failure responses expose a safe category/code and request ID only. Stack
  traces, provider payloads, and connection details remain server logs under
  redaction policy.
- Rate limiting uses the existing expensive/high-risk Platform mutation
  policy and returns `429 RATE_LIMITED`; no new rate-limit subsystem is
  created.

## Secret policy

The following are prohibited in API responses, structured logs, audit
metadata, request fingerprints, and normal Control Plane columns:

- passwords and password hashes;
- TOTP secrets and recovery codes;
- session tokens/secrets;
- database passwords and connection strings;
- private keys; and
- raw provider credentials or worker payloads.

The owner initial password is accepted only through the existing protected
Phase 5B user-creation path, hashed immediately, and never returned or
logged. No email/SMS delivery provider is introduced. The local development
activation path is the existing protected admin workflow with an
admin-supplied initial password; production invitation delivery is outside
this contract.

## Audit requirements

Phase 7 uses the existing append-only `audit.events` system. The exact
controlled vocabulary is:

```text
PROVISIONING_REQUESTED
PROVISIONING_STARTED
PROVISIONING_DATABASE_CREATED
PROVISIONING_MIGRATING
PROVISIONING_SEEDING
PROVISIONING_VERIFYING
PROVISIONING_ACTIVATED
PROVISIONING_FAILED
PROVISIONING_RETRY_STARTED
```

Each applicable event includes operation ID, Gym ID, organization ID, server
ID, database ID when available, lifecycle state, request ID, actor ID, and a
safe failure category/reason when applicable. System execution uses the
existing approved system-actor convention; it does not create a second audit
identity. Denied provisioning attempts are recorded through the existing
security-audit policy without secrets.

## Boundary

Provisioning is a Control Plane operation. It can create a Gym registry
record and the isolated Gym database, but it cannot read or expose Gym
members, payments, attendance, training, nutrition, store, CRM, or other
business records. Phase 5B cross-Gym and Platform/Gym isolation remains a
regression requirement.

## Implementation verification

The runtime catalog contains 16 permissions, 3 roles, and 15 role-permission
assignments after adding the human-approved `platform.provision` grant to
`platform-security-admin`. Start and retry reject a session without verified
MFA with `403 MFA_REQUIRED`; read-only status does not require a second
step-up. Provisioning API tests verify Gym Owner denial and safe response/audit
metadata.
