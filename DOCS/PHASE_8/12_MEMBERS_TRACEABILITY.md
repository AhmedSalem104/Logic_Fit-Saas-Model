# Members Traceability Matrix

**Status:** GREEN — all approved core requirements mapped
**Test entries are test-contract identifiers; this task implements no tests.**

| Requirement | Database / EF | API | Permission | Web | Flutter | Test contract | Audit | Documentation |
|---|---|---|---|---|---|---|---|---|
| Gym-scoped Member identity | `members.members.member_id`, `gym_id` | All six Members routes | Operation-specific `members.*` | MEM-W-001/002/003 | F-MEM-001/002 | `SEC-MEM-GYM-*` | Safe actor/Gym/Member context | 01, 02, 03, 08 |
| List/search/filter/page | `members.members` indexes | `GET /api/v1/gyms/{gymId}/members` | `members.read` | MEM-W-001 | F-MEM-001 | `API-MEM-LIST-*`, `WEB-MEM-LIST-*`, `FL-MEM-LIST-*` | Read/scope policy | 03, 05, 09, 10 |
| Create | `members.members`, audit, idempotency boundary | `POST /api/v1/gyms/{gymId}/members` | `members.create` | MEM-W-002 | F-MEM-001/002 action only where existing screen permits | `API-MEM-CREATE-*`, duplicate/idempotency tests | `MEMBER_CREATED` | 02, 03, 05, 06 |
| Detail | `members.members` | `GET /api/v1/gyms/{gymId}/members/{memberId}` | `members.read` | MEM-W-003 | F-MEM-002 | `API-MEM-DETAIL-*`, privacy/scope tests | Safe read context | 03, 08, 09, 10 |
| Profile/status update | `members.members`, row version, audit | `PUT /api/v1/gyms/{gymId}/members/{memberId}` | `members.update` | MEM-W-002 | F-MEM-002 only if existing mobile action is enabled | `API-MEM-UPDATE-*`, concurrency tests | `MEMBER_UPDATED`, `MEMBER_STATUS_CHANGED` | 02, 03, 05, 06 |
| Archive | Archive metadata/status; no SQL DELETE | `DELETE /api/v1/gyms/{gymId}/members/{memberId}` | `members.delete` | MEM-W-001/003 action | F-MEM-002 action only if authorized | `API-MEM-ARCHIVE-*`, idempotency/security tests | `MEMBER_ARCHIVED` | 03, 06, 08, 09 |
| Member timeline | `members.timeline_events` Member-domain projection | `GET /api/v1/gyms/{gymId}/members/{memberId}/timeline` | `members.read` | MEM-W-003 | F-MEM-002 | `API-MEM-TIMELINE-*`, privacy/order tests | Source events are safe and redacted | 03, 07, 08 |
| Members permission grants | Existing Phase 5B RBAC model; no new role | All protected routes | Five approved identifiers | Permission-aware controls | Permission-aware controls | `SEC-MEM-RBAC-*` | Denials use existing audit | 04, 15 |
| No operational seed | No Member seed rows | N/A | N/A | Empty state | Empty state | `DB-MEM-NO-SEED` | N/A | 00, 02, 13 |
| Portal separation | Existing Portal code/session tables remain separate | No Portal route added | `portal.member.*` remains separate | No Admin shortcut | No Portal change | `SEC-MEM-PORTAL-SEPARATION` | Portal audit per its contract | 11, 13 |

## Approved test contract

Before implementation, define and then execute Unit, Integration, API, Web, Flutter, security, validation, duplicate/idempotency, concurrency, paging/search/filter, privacy, audit, RTL/Arabic/theme, and responsive tests for the mapped requirements. This audit creates no test implementation.
