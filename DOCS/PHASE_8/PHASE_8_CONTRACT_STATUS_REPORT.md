# Phase 8 Contract Status Report

**Date:** 2026-08-29
**Status:** GREEN — contract closure complete; implementation not authorized by this task

## Final scope

Phase 8 Members core covers Gym-scoped list/search/filter/page, create, detail, PUT update, DELETE archive, Member-domain timeline, Web screens MEM-W-001/002/003, Flutter screens F-MEM-001/002, RBAC/privacy/audit, and the existing Portal relationship. There is no physical Member delete and no operational Member seed.

## Final contract results

1. **Lifecycle:** `ACTIVE`, `INACTIVE`, `ARCHIVED`; create defaults ACTIVE; ACTIVE/INACTIVE may transition through PUT; DELETE moves to ARCHIVED; archived records are retained and not normally mutable.
2. **Database:** `members.members` in the selected Gym database with stable UUID, Gym scope, approved profile fields, status, audit metadata, row version, archive metadata, indexes, and no cross-Gym access.
3. **RBAC:** `gym-security-admin` receives all five Members permissions; `gym-authenticated-user` receives `members.read`; `platform-security-admin` receives no implicit Gym Member access.
4. **Uniqueness/idempotency:** Member Code is Gym-unique when required by Portal; phone/email are not globally unique; equivalent create replay returns the original result; conflicting replay/duplicate code is `409 DUPLICATE_RESOURCE`; stale mutation is `409 CONCURRENCY_CONFLICT`; archive is idempotent.
5. **API:** six canonical routes use GET collection, POST collection, GET item, PUT item, DELETE archive, and GET timeline under `/api/v1/gyms/{gymId}/members`.
6. **Queries:** page default 25/max 100; approved search fields are memberCode, firstName, lastName, displayName, phone, and email; status values are the three canonical statuses; default excludes ARCHIVED; deterministic createdAt-descending ordering.
7. **Privacy:** list/detail/timeline allowlists exclude authentication, infrastructure, future-domain sensitive data, arbitrary metadata, and secrets.
8. **Timeline:** only MEMBER_CREATED, MEMBER_UPDATED, MEMBER_ARCHIVED, and MEMBER_STATUS_CHANGED; occurredAt descending, eventId tie-breaker, safe metadata.
9. **Web:** MEM-W-001/002/003 on the existing React App Shell with Arabic/RTL, Light/Dark, responsive, loading/empty/error/success, and permission-aware UX.
10. **Flutter:** F-MEM-001/002 on the existing Flutter app; F-MEM-003 remains Attendance scope; Arabic/RTL, themes, safe cache/read behavior, and API-only access are defined.
11. **Portal:** existing Member Code -> Gym context -> scoped Portal session remains unchanged and separate from Admin APIs.
12. **TOP GYM:** reference behavior mapped; legacy single-DB routes, role grants, membership/payment coupling, and unscoped access are not copied.
13. **Tests:** Unit, Integration, API, Web, Flutter, security, data, privacy, audit, concurrency, idempotency, search/filter, RTL/Arabic/theme, and responsive test contracts are defined; no tests are implemented here.

## Consistency gate

- Phase 2–7 authority reviewed.
- Phase 2 Member `PATCH` shorthand reconciled to the explicitly approved Phase 8 `PUT` contract in affected documentation.
- No duplicate Members routes or permission identifiers.
- No role or lifecycle conflict.
- Timeline is bounded to Members.
- No Membership/Attendance/future-module leakage.
- No Member seed data.
- Portal authentication boundary preserved.
- No implementation, schema migration, database write, client code, seed, or TOP GYM change.

## Implementation result

The explicit Phase 8 implementation authorization has been executed. The
approved Members core is implemented and verified without changing the
contract boundary. Runtime and test evidence is recorded in
`17_MEMBERS_IMPLEMENTATION.md` and `18_MEMBERS_TEST_RESULTS.md`. The final
handoff remains limited to Members; no later phase or business module was
started.

## Next authorization boundary

Phase 9 and all other business modules remain stopped pending a separate
explicit authorization. This report does not authorize Memberships,
Attendance, Measurements, Training, Nutrition, Finance, Store, CRM, Classes,
Documents, Notifications, or any other module.
