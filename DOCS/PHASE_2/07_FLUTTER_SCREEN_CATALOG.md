# Phase 2 Flutter Screen Catalog

**Platform:** Flutter/Dart + Riverpod + GoRouter + Dio + JSON serialization  
**Scope rule:** Flutter is a real iOS/Android client, not Flutter Web. It shares the REST contracts with Web and does not receive business authority.

## Mobile scope labels

- **MOBILE REQUIRED:** member/trainer workflows explicitly called for by the Master Bible or with a clear mobile use case.
- **MOBILE OPTIONAL:** useful mobile companion; Web remains primary until approved.
- **WEB ONLY:** infrastructure-heavy or desktop-first workflow.

| ID / scope / route | Actor / purpose / navigation | State/API/permission | Offline and responsive behavior | Data source |
|---|---|---|---|---|
| `F-AUTH-001` MOBILE REQUIRED `/login` | User login; GoRouter auth shell → MFA/home. | Dio `POST /auth/login`; auth permission; loading/invalid/rate-limited. | Do not cache credentials; offline explains unavailable. RTL/localized form. | `iam.users/sessions` DTO. |
| `F-MEM-001` MOBILE REQUIRED `/gym/members` | Staff search member; shell → profile/create. | `GET /gyms/{gymId}/members`; `members.read`; paginated loading/empty/error. | Cache last authorized list read-only; mutations require online. Cards, not wide table; RTL. | `members.members`. |
| `F-MEM-002` MOBILE REQUIRED `/gym/members/:id` | Staff/member profile summary; tabs to membership/attendance/measurements/plans. | member detail endpoints; `members.read` plus tab permissions; safe field allowlist. | Offline cached read with stale label; no sensitive prefetch; RTL. | members/memberships/attendance/measurements and published plan DTOs. |
| `F-MEM-003` MOBILE REQUIRED `/gym/members/:id/attendance` | Staff check-in/out; member may view. | attendance read/check-in/out; mutation idempotency; loading/success/conflict. | Queue only explicitly idempotent check-in with visible pending state; no silent replay. | `members.attendance_records`. |
| `F-MEM-004` MOBILE REQUIRED `/gym/members/:id/measurements` | Trainer record/view exact measurements. | measurements read/create/update; field validation; offline draft may be local but sync conflict visible. | Numeric inputs, unit labels, RTL; no invented fields. | `members.body_measurements`. |
| `F-TRN-001` MOBILE REQUIRED `/gym/training` | Trainer/member plan list and workout session launch. | plan list/version/session APIs; `training.read`, `training.sessions.manage`; states. | Published plan/session data may be cached; writes use idempotency and conflict UI. | training plans/versions/sessions/set logs. |
| `F-TRN-002` MOBILE REQUIRED `/gym/training/plans/:id/review` | Trainer review/return/approve when permission exists. | review/approve APIs; exact permission and creator separation; no publish unless granted. | Online required for transitions; disabled offline; RTL summary sections. | plans/reviews/version candidate. |
| `F-TRN-003` MOBILE REQUIRED `/gym/training/sessions/:id` | Member/trainer log sets and finish session. | session/set APIs; `training.sessions.manage`; planned vs actual. | Local draft set logs can queue only with idempotency; visible sync state. | published version/session/set logs. |
| `F-NUT-001` MOBILE REQUIRED `/gym/nutrition` | Member view published nutrition and meal log. | nutrition version/log endpoints; `nutrition.read/logs.manage`; loading/empty/error. | Published snapshot/read cache; meal log queue only with idempotency; RTL. | nutrition versions/meals/logs. |
| `F-NUT-002` MOBILE OPTIONAL `/gym/nutrition/plans/:id/review` | Nutritionist review/approve companion. | review/approve APIs; `nutrition.review/approve`; online only. | Read-only cache; transitions disabled offline. | nutrition plan/calculation/reviews. |
| `F-CRM-001` MOBILE OPTIONAL `/gym/crm/follow-ups` | Staff see due follow-ups and record activity. | CRM follow-up/activity APIs; permissions; offline activity draft requires explicit sync state. | Compact list; no automatic WhatsApp; RTL. | crm follow-ups/activities. |
| `F-CLS-001` MOBILE REQUIRED `/gym/classes` | Member/staff view schedule and book/cancel. | classes/session/booking APIs; `classes.booking.*`; capacity conflict states. | Cache schedule; booking/cancel online only until policy approves offline. | classes definitions/sessions/bookings/waitlist. |
| `F-POR-001` MOBILE REQUIRED `/member-portal` | Member portal home/profile/membership/attendance. | portal endpoints; member session scope; sensitive allowlist. | Read-only cache with stale marker; RTL/localization. | member portal DTOs. |
| `F-POR-002` MOBILE REQUIRED `/member-portal/plans` | Member training/nutrition/progress/documents/notifications. | portal plan/document endpoints; scopes; empty/loading/error. | Published snapshots cache; document download requires online/adapter. | published versions/reports/documents. |
| `F-QR-001` MOBILE OPTIONAL `/qr/:token` | Public QR result from a scanner/deep link. | unauthenticated QR endpoint; no permission; generic invalid/rate-limit. | No token persistence beyond route handling; no sensitive cache; RTL-safe minimal result. | QR verifier + public Gym name only. |
| `F-LIB-001` MOBILE OPTIONAL `/gym/library/exercises` | Trainer exercise guide/search, not canonical administration. | library read; active canonical/custom scope; cached read. | Offline cached library may be stale; no edits; cards/media fallback. | library exercises/muscles/media. |
| `F-LIB-002` MOBILE OPTIONAL `/gym/library/foods` | Member/nutritionist food guide/search. | library read; cached read; no edits. | Offline read with stale label; RTL. | library foods/categories/units. |

## Web-only / excluded from Flutter in this contract

Platform Admin overview and infrastructure, organizations/servers/databases, provisioning, global migrations, backups/restore, deployments, access administration, full finance/closing, POS, inventory adjustment/count management, full library CRUD, full Training/Nutrition builders, report configuration, and print/PDF administration are **WEB ONLY** until a separate mobile use case is approved.

## Shared mobile technical contract

- Riverpod owns request/cache state; Dio calls the same REST endpoints; JSON serialization models API DTOs.
- GoRouter guards authentication and route-level UX only; backend permissions remain authoritative.
- Offline mode never invents business results or bypasses lifecycle/permission checks.
- Mutating offline queues require an endpoint-approved idempotency key and visible conflict/retry state; otherwise the action is disabled.
- Arabic/English localization, RTL, empty/error/loading/disabled/success states are required for every listed mobile screen.

## Final mobile contract addendum — 2026-08-25

| ID | Scope / route | Actor and behavior | API / permission / state | Offline / RTL |
|---|---|---|---|---|
| `F-POR-000` | MOBILE REQUIRED `/portal/access` | Member enters approved Member Code; no username/password. | `POST /member-portal/access`; public rate-limited exchange; scoped/revocable portal session; generic invalid/expired/revoked states. | Access requires network; no code/session persistence in logs; mobile-first RTL. |
| `F-NOT-001` | MOBILE REQUIRED `/notifications` | Staff/member reads in-app notifications and follows safe deep links. | notification list/read APIs; `notifications.read`; recipient-scoped. | Cache read state only with server reconciliation; RTL/loading/empty/error. |

Finance, POS/Inventory, and infrastructure-heavy monitoring/backup remain Web-first in this contract. The approved class, CRM, training, nutrition, member, portal, and notification mobile workflows use the same API contracts and never calculate authoritative values locally.
