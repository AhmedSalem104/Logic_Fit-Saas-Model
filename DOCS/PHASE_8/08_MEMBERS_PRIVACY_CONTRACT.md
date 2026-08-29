# Members Privacy Contract

**Status:** GREEN — privacy and response boundaries closed

## Core field classification

| Field/data | Classification | Core response rule |
|---|---|---|
| `memberId`, `gymId`, status, registration date, version | Operational identifiers/state | Return only in the approved scoped DTO |
| `fullName`, phone, email, Member Code | Personal/contact or protected Portal data | Return only in approved administrative projection; Member Code remains governed by Portal rules |
| notes | Personal free text | Detail/update only as approved; never copied into logs or timeline metadata |
| Membership, payment, attendance, measurement, training, nutrition, CRM, documents | Separate or sensitive domains | Excluded from Phase 8 core DTOs and timeline |
| Passwords, hashes, MFA, recovery codes, sessions, QR/raw Portal secrets | Authentication/access secrets | Never stored or returned by Members APIs |

## List projection

The list returns only `memberId`, approved Member Code representation when the existing Portal contract requires it, `fullName`, normalized phone, optional email, registration date, status, timestamps, and opaque version. It does not return authentication, infrastructure, future-domain, or arbitrary metadata.

## Detail projection

Detail returns the approved core profile and safe audit/version metadata. It does not include password data, MFA, recovery, session, QR, membership, payment, attendance, health, training, nutrition, CRM, document, or infrastructure values.

## Timeline and audit redaction

Timeline exposes only the four Member-domain event types and allowlisted safe metadata. Audit records identify Member/Gym/actor/request and safe changed-field information. Secrets and unnecessary personal payloads are redacted.

## Authorization and isolation

The backend enforces the actor, Gym, role, and permission for every response. Platform Admin access to Platform APIs does not grant Member data access. React/Flutter field hiding is not a privacy or authorization boundary.

## Export

`members.export` remains a contracted permission assigned to `gym-security-admin`, but no export endpoint or output format is part of Phase 8 core. It is documented as `CONTRACTED PERMISSION / IMPLEMENTATION DEFERRED`.
