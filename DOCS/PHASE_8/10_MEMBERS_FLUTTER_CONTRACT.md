# Members Flutter Contract

**Status:** BLOCKED — profile tab boundary and API schemas require closure
**Client:** existing Flutter app using Dio, Riverpod, and GoRouter

## F-MEM-001 — Mobile Member list

| Item | Contract |
|---|---|
| Route | `/gym/members` |
| API | `GET /api/v1/gyms/{gymId}/members` |
| Permission | `members.read` |
| Purpose | Authorized staff Member search/list |
| States | Loading, paged loading, empty, error/retry, unauthorized, success |
| Offline | Existing catalog permits a cache of the last authorized list for read-only use; mutations remain online |
| UX | Cards, safe fields only, Arabic/RTL, Light/Dark themes |

## F-MEM-002 — Mobile Member profile

| Item | Contract |
|---|---|
| Route | `/gym/members/:id` |
| APIs | Member detail and approved timeline/detail APIs |
| Permission | `members.read` plus a separate permission for any future linked tab |
| Purpose | Authorized Member profile summary |
| States | Loading, cached/stale read, empty/not found, error/retry, unauthorized, success |
| Offline | Existing catalog permits safe cached read; no sensitive prefetch |
| UX | Safe allowlist, Arabic/RTL, Light/Dark themes |

The Phase 2 catalog names membership, attendance, measurements, and plans tabs. Attendance is separately scoped as `F-MEM-003`; measurements and plans are later-domain contracts. P8-G-006 requires a decision on the core profile surface before implementation.

## F-MEM-003 — Attendance boundary

`F-MEM-003` remains a separately scoped Attendance screen. It is documented to preserve traceability, but no Attendance API, table, permission, route implementation, or Member-core flow is authorized by this contract audit.

## Client security

Flutter uses the same API and server-side authorization as Web. It must not access SQL Server, calculate authoritative permissions, or create a second session/Member business rule system.
