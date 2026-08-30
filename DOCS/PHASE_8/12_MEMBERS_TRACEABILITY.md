# Members Traceability Matrix

**Status:** GREEN - approved core requirements are implemented and mapped.
**Evidence:** `17_MEMBERS_IMPLEMENTATION.md` and `18_MEMBERS_TEST_RESULTS.md`.

| Requirement | Database / EF | API | Permission | Web | Flutter | Test / verification | Audit | Documentation |
|---|---|---|---|---|---|---|---|---|
| Gym-scoped Member identity | `members.members.member_id`, `gym_id`; `GymDbContext` | All six Members routes | Operation-specific `members.*` | `apps/web/src/components/MemberPages.tsx` | `apps/mobile/lib/members.dart` | `MembersAuthorizationAndGymIsolationAreEnforced` | Safe actor/Gym/Member context | 01, 02, 03, 08, 17 |
| List/search/filter/page | `members.members` query indexes | `GET /api/v1/gyms/{gymId}/members` | `members.read` | MEM-W-001 | F-MEM-001 | API, Web, Flutter, and direct Chrome checks | Read/scope policy | 03, 05, 09, 10, 17, 18 |
| Create | `members.members`, idempotency columns/constraint, timeline | `POST /api/v1/gyms/{gymId}/members` | `members.create` | MEM-W-002 | Read-only relationship in F-MEM-001/002 | API duplicate/idempotency tests and Web flow | `MEMBER_CREATED` | 02, 03, 05, 06, 17, 18 |
| Detail | `members.members` | `GET /api/v1/gyms/{gymId}/members/{memberId}` | `members.read` | MEM-W-003 | F-MEM-002 | API privacy/scope assertions; Web/Flutter tests | Safe read context | 03, 08, 09, 10, 17 |
| Profile/status update | `members.members`, `row_version`, timeline/audit | `PUT /api/v1/gyms/{gymId}/members/{memberId}` | `members.update` | MEM-W-002 | Read-only relationship in F-MEM-002 | API stale-version test and domain tests | `MEMBER_UPDATED`, `MEMBER_STATUS_CHANGED` | 02, 03, 05, 06, 17, 18 |
| Archive | `status=ARCHIVED`; no SQL DELETE path | `DELETE /api/v1/gyms/{gymId}/members/{memberId}` | `members.delete` | MEM-W-001/003 action | No mutation UI in F-MEM-002 | API archive/idempotency/concurrency tests and direct Chrome | `MEMBER_ARCHIVED` | 03, 06, 08, 09, 17, 18 |
| Member timeline | `members.timeline_events` Member-domain projection | `GET /api/v1/gyms/{gymId}/members/{memberId}/timeline` | `members.read` | MEM-W-003 | F-MEM-002 | API event/privacy/order tests; Web/Flutter tests | Safe source events | 03, 07, 08, 17, 18 |
| Members permission grants | `PermissionCatalog`; Control Plane seed verification | All protected routes | Five approved identifiers | Permission-aware controls | Backend/API authorization | Permission and Gym-isolation regressions | Existing audit system | 04, 15, 17, 18 |
| No operational seed | No Member seed code or rows | N/A | N/A | Empty state | Empty state | `EfFoundationTests` and SQL count check | N/A | 00, 02, 13, 17, 18 |
| Portal separation | Existing Portal code/session tables remain separate | No Portal route added | `portal.member.*` remains separate | No Admin shortcut | No Portal change | Portal-boundary review | Portal audit per its contract | 11, 13, 17 |

## Executed verification

- Backend: `tests/LogicFit.UnitTests/MemberDomainTests.cs`, `tests/LogicFit.ApiTests/MembersApiTests.cs`, and the complete solution test run.
- Database: `tests/LogicFit.IntegrationTests/EfFoundationTests.cs`, EF migration listing/application, SQL Server schema and seed checks.
- Web: `apps/web/src/MemberPages.test.tsx`, full Web test suite, typecheck, production build, and direct Chrome flow.
- Flutter: `apps/mobile/test/members_test.dart`, full Flutter test suite, analyze, Android APK build, and Windows build.
- Security/data: RBAC, Gym isolation, 401/403, duplicate/idempotency, optimistic concurrency, archive, pagination/search/filtering, timeline allowlist, and audit assertions are covered by API tests and direct runtime verification.
