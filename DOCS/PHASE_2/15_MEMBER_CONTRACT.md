# Phase 2 Member Contract

**Sources:** TOP GYM Members/Measurements audit and exact runtime fields; Master Bible Members/Portal/Storage scope; SC-017 QR privacy.  
**Status:** canonical boundary defined; implementation deferred.

## Member aggregate

```text
Member
  id
  fullName              required, max 120
  phone                 required, normalized, max 30
  email?                max 254
  registrationDate
  notes?                max 1000
  status
  created/updated/version metadata
```

These fields are the audited TOP GYM member fields. No extra personal/health fields are added merely for convenience.

## Linked domains

| Area | Contract | Source / table |
|---|---|---|
| Membership | Member may have membership records with package/period/freeze/payment linkage, lifecycle events, and receipts. Exact package catalog is not invented. | `SOURCE: TOP_GYM`; `members.memberships`, `membership_events`. |
| Attendance | Check-in/out records are separate and queryable by member/time. | `SOURCE: TOP_GYM`; `members.attendance_records`. |
| Measurements | Exact fields: measuredAt, weightKg, heightCm, bodyFatPercent, chestCm, waistCm, hipsCm, armsCm, thighsCm, notes. | `SOURCE: TOP_GYM`; `members.body_measurements`. |
| Training | Member has linked Draft/Admin views by permission and Published plan/session views in Portal. | `SOURCE: TOP_GYM` + LogicFit lifecycle; training tables. |
| Nutrition | Member has linked plan/calculation/log views; Portal receives Published snapshots only. | `SOURCE: TOP_GYM` + SC-015; nutrition tables. |
| Documents | LogicFit requirement; document metadata/versions use `StorageAdapter`, local filesystem in development. | `NEW FEATURE`; documents tables. |
| Timeline | Authorized projection of membership, attendance, measurement, CRM, training/nutrition and payment events. | TOP GYM evidence + LogicFit audit architecture. |
| QR | Public lookup uses SC-017 opaque token; it does not expose Member ID or profile data. | `DECISION: SC-017`; `members.qr_tokens`. |

## Member API/screen mapping

| Capability | Screen/API | Permission | Data |
|---|---|---|---|
| Search/list | `MEM-W-001`, `GET /gyms/{gymId}/members` | `members.read` | members.members |
| Create/edit | `MEM-W-002`, POST/PUT member | `members.create/update` | members.members |
| Profile | `MEM-W-003`, member detail/timeline | `members.read` + tab-specific permission | linked member tables |
| Membership | `MEM-W-004`, membership/freeze/renew/payment | membership/finance permissions | memberships/events/payments |
| Attendance | profile/mobile attendance | attendance permissions | attendance records |
| Measurements | `MEM-W-005`, measurements endpoints | measurements.* | body_measurements |
| Documents | `DOC-W-001`, documents endpoints | documents.* | document records/versions + adapter |
| Portal | `POR-W-001/002`, portal endpoints | member self scope | safe projections/published versions |
| QR | `POR-W-003`, `/qr/:token` | public constrained route | hashed token + public Gym branding |
| Print/PDF | `PRT-W-001`, print/pdf | source read + print/pdf permission | member/membership/version snapshots |

## Validation and privacy

- Member reads/writes require selected Gym scope and backend authorization.
- Phone/email normalization and exact TOP GYM limits are validated server-side.
- Financial/health fields are returned only to authorized endpoints/tabs.
- Member Portal uses an explicit safe DTO, not the administrative member aggregate.
- Public QR returns only the SC-017 allowlist and uses no numeric Member ID.
- Documents never expose storage credentials or unrestricted object references.
- A member archive/deletion operation must preserve required financial/audit/history references; document retention follows the configurable policy with a seven-year default for financial/legal/contractual documents.

## Final portal/storage consistency — 2026-08-25

Member Portal entry uses `Member Code → Gym context → scoped portal session`, not a traditional member username/password. Portal access codes and sessions are revocable/rate-limited and are never exposed as numeric Member IDs. QR remains a separate opaque public-safe lookup. Member documents use the StorageAdapter metadata contract and permission/audit-controlled retention.
