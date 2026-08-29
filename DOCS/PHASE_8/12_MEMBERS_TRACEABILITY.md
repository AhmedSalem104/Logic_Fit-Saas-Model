# Members Traceability Matrix

**Status:** BLOCKED until the Phase 8 gap register is approved
**Test entries are test-contract identifiers only; no tests are implemented by this audit.**

| Requirement | Database | API | Permission | Web | Flutter | Test contract | Audit | Documentation |
|---|---|---|---|---|---|---|---|---|
| Gym-scoped Member identity | `members.members.member_id`, `gym_id` | All six Member routes | Operation-specific Members permission | MEM-W-001/002/003 | F-MEM-001/002 | `SEC-MEM-GYM-*` | Scope in each mutation/read audit context | 01, 02, 03, 08 |
| List Members | `members.members` + approved indexes | `GET /gyms/{gymId}/members` | `members.read` | MEM-W-001 | F-MEM-001 | `API-MEM-LIST-*`, `WEB-MEM-LIST-*`, `FL-MEM-LIST-*` | Read access/security event policy to close | 03, 09, 10 |
| Create Member | `members.members` + audit/version | `POST /gyms/{gymId}/members` | `members.create` | MEM-W-002 | No create screen explicitly catalogued; online mutation only if later approved | `API-MEM-CREATE-*`, duplicate/idempotency tests | Member-created event | 02, 03, 05, 08 |
| Read Member detail | `members.members` | `GET /gyms/{gymId}/members/{memberId}` | `members.read` | MEM-W-003 | F-MEM-002 | `API-MEM-DETAIL-*`, privacy/scope tests | Read policy to close | 03, 08, 09, 10 |
| Update Member | `members.members`, row version, audit | `PATCH /gyms/{gymId}/members/{memberId}` | `members.update` | MEM-W-002 | No approved mutation screen beyond catalog scope | `API-MEM-UPDATE-*`, concurrency tests | Member-updated event | 02, 03, 05, 06 |
| Delete/archive Member | `members.members` archive/deletion representation unresolved | `DELETE /gyms/{gymId}/members/{memberId}` | `members.delete` | MEM-W-001/003 action subject to closure | No approved delete UI | `API-MEM-DELETE-*`, lifecycle tests | Archive/delete event | 03, 06, 08, 09 |
| Member timeline | `members.timeline_events` evidence; source set unresolved | `GET /gyms/{gymId}/members/{memberId}/timeline` | `members.read` | MEM-W-003 | F-MEM-002 only for approved safe projection | `API-MEM-TIMELINE-*`, privacy/order tests | Event projection/read policy to close | 03, 07, 08 |
| Members permission identifiers | No new permission data authorized by this audit | Used by routes above | Five existing identifiers | Permission-aware controls | Permission-aware controls | `SEC-MEM-RBAC-*` | Authorization denial events through existing audit | 04 |
| No operational Member seed | No Member seed rows | N/A | N/A | Empty state supported | Empty state supported | `DB-MEM-NO-SEED` | N/A | 00, 02, 13 |
| Portal separation | Portal tables/contracts separate from core Member API | No Portal route added | `portal.member.*` remains separate | No Portal admin shortcut | No Portal change | `SEC-MEM-PORTAL-SEPARATION` | Portal audit per its own contract | 11, 13 |

## Traceability result

The locked routes and known fields are traceable. Rows marked “to close” prevent an implementation-ready contract. No orphan implementation is authorized.
