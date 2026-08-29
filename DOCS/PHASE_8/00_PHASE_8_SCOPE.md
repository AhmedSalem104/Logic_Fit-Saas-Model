# Phase 8 — Members Scope

**Contract status:** GREEN — contract closure complete; implementation is not authorized by this task
**Closure date:** 2026-08-29

## Authority and closure

This package applies the explicit human approvals for P8-G-001 through P8-G-006. LogicFit Phase 2–7 documentation remains the project authority, with the final Phase 8 approval acting as the explicit update for the Members operation contract. Stale Phase 2 Member references to `PATCH` are reconciled to the approved `PUT` operation in the affected documentation; no runtime code is changed.

The root files `DECISION_LOCK.md`, `IMPLEMENTATION_ROADMAP.md`, and `CODEX_KICKOFF.md` remain absent. No contents were reconstructed.

## Final Phase 8 core scope

Phase 8 Members core contains:

- Gym-scoped Member list, search, status filtering, and deterministic paging;
- Member creation;
- Member detail;
- Member profile update;
- history-preserving Member archive through the DELETE route;
- Member-domain timeline;
- existing Members permissions and Phase 5B authorization/audit;
- Web screens `MEM-W-001`, `MEM-W-002`, and `MEM-W-003`;
- Flutter screens `F-MEM-001` and `F-MEM-002`.

The canonical API family is `/api/v1/gyms/{gymId}/members`. No Member seed data is permitted.

## Final decisions

- Member status is `ACTIVE`, `INACTIVE`, or `ARCHIVED`.
- There is no physical Member delete; `members.delete` means archive.
- `gym-security-admin` receives all five Members permissions.
- `gym-authenticated-user` receives `members.read` only.
- `platform-security-admin` receives no automatic Gym Member access.
- Member IDs are system-generated and immutable.
- The existing Portal contract requires a Member Code for Portal-enabled Members; it is Gym-unique and is not an internal database identifier.
- Phone and email are not globally unique; existing stronger authoritative rules would take precedence if later documented.
- Create idempotency, optimistic concurrency, and idempotent archive behavior are required.
- List defaults are page size 25, maximum 100, `createdAt` descending with a stable ID tie-breaker; archived rows require an explicit status filter.
- Timeline is limited to Member-domain events: `MEMBER_CREATED`, `MEMBER_UPDATED`, `MEMBER_ARCHIVED`, and `MEMBER_STATUS_CHANGED`.
- Future Memberships, Attendance, Measurements, Training, Nutrition, Store, Finance, CRM, Classes, Documents, and Notifications are not implemented or represented by placeholder features.

## Boundaries

Phase 5B owns authentication, sessions, MFA, RBAC, Gym context, and the single audit system. Phase 6 owns Platform Foundation. Phase 7 owns Gym provisioning. Phase 8 consumes those foundations and owns only the Member core contract. Member Portal authentication remains separate, and `F-MEM-003` remains Attendance scope.

## Classification result

| Classification | Closure result |
|---|---|
| A — Locked | Architecture, Gym database boundary, core table, core fields, route family, operation set, permissions, screen IDs, no Member seeds, API envelope, and module boundaries |
| B — Implied | Server-side Gym authorization, Phase 5B RBAC/audit reuse, EF Core persistence, row-version concurrency, history-preserving archive, and no direct client/database access |
| C — Contract gap | Closed by P8-G-001 through P8-G-006 and the schemas in this package |
| D — Product/security decision | Closed by the explicit human approval recorded in `15_PHASE_8_DECISIONS.md` |
| E — Documentation drift | PATCH references reconciled to PUT; future-domain tabs explicitly bounded |

## Implementation boundary

This is a contract package only. No C#, EF migration, SQL change, database write, seed change, API implementation, React feature, Flutter feature, test implementation, or TOP GYM change is included.
