# Members Flutter Contract

**Status:** GREEN — Flutter contract closed; implementation not authorized by this task
**Client:** existing Flutter application using Dio, Riverpod, and GoRouter

## F-MEM-001 — Mobile Member list

| Item | Contract |
|---|---|
| Route | `/gym/members` |
| API | `GET /api/v1/gyms/{gymId}/members` |
| Permission | `members.read` |
| Actions | Search, status filter, page, open profile |
| States | Loading, paged loading, empty, error/retry, unauthorized, success |
| Offline | Last authorized safe list may be cached read-only; mutations require network |
| UX | Safe cards, Arabic/RTL, Light/Dark |

## F-MEM-002 — Mobile Member profile

| Item | Contract |
|---|---|
| Route | `/gym/members/:id` |
| APIs | Member detail GET and Member timeline GET |
| Permission | `members.read`; future linked tabs require their own contracts |
| Content | Core profile, status, Member Code where contractually required, and Member-domain timeline |
| States | Loading, cached/stale read, empty/not found, error/retry, unauthorized, success |
| Offline | Safe cached read only; no sensitive prefetch; mutations require network |
| UX | Arabic/RTL, Light/Dark, safe allowlist |

## F-MEM-003 — Attendance boundary

`F-MEM-003` remains separately scoped Attendance. It is not part of the Phase 8 Members core implementation, API, schema, or tests.

## Client security

Flutter uses the same API and backend authorization as Web. It does not access SQL Server, calculate authoritative permission decisions, or create a second Member/session/business-rule system.
