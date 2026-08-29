# Phase 8 Members Gap and Conflict Register

**Gate status:** BLOCKED
**Rule:** No implementation may begin while a required gap is unresolved.

| ID | Gap / conflict | Classification | Evidence | Required approval / closure | Status |
|---|---|---|---|---|---|
| P8-G-001 | Canonical Member status values, default, transitions, archive visibility/recovery, and exact DELETE meaning are not enumerated | C / D | Phase 2 has `status` and history-preserving archive intent; API has DELETE; no enum/transitions | Product + security decision | OPEN |
| P8-G-002 | Exact grants for `members.read/create/update/delete/export` across the three concrete roles are absent | C / D | Permission identifiers are locked; current role assignments have no `members.*`; Platform Admin has no implicit Gym access | Security/RBAC decision; no new role/key without separate approval | OPEN |
| P8-G-003 | Phone/email uniqueness, duplicate behavior, Member Code relation, create idempotency, and concurrent retry semantics are not closed | C / D | Email uniqueness is configurable; duplicate policy explicitly not invented; generic idempotency is not Member-specific | Product + security/data decision | OPEN |
| P8-G-004 | Complete list/create/detail/update/delete request/response JSON schemas, nullability, date/version format, query allowlist, and privacy field allowlists are incomplete | C | API catalog gives routes and concepts, not complete DTO/query schemas | API/data contract approval | OPEN |
| P8-G-005 | Timeline core event types, source ownership, metadata allowlist, pagination/order/filtering, and future-domain inclusion are unresolved | C / D / E | Timeline table/route exist; source description includes future modules excluded from Phase 8 core | Product + privacy/security decision | OPEN |
| P8-G-006 | MEM-W-003/F-MEM-002 linked tabs conflict with the locked core boundary excluding memberships, attendance, measurements, plans, and other modules | E / D | Phase 2 screen catalog versus Phase 8 locked scope | Product/UI boundary decision | OPEN |

## Classified non-blockers / deferred items

| Item | Classification | Resolution |
|---|---|---|
| Missing root `DECISION_LOCK.md`, `IMPLEMENTATION_ROADMAP.md`, `CODEX_KICKOFF.md` | Documentation absence | Do not reconstruct; existing repository docs remain authority |
| `members.export` has no route | OUT OF SCOPE / DEFERRED | Keep identifier; no export implementation or invented endpoint |
| Member Portal code format/rotation details | DEPENDENCY / FUTURE PHASE | Preserve existing Portal auth; close in a Portal contract unless promoted to Phase 8 |
| `F-MEM-003` Attendance | OUT OF SCOPE / SEPARATE PHASE | Do not implement with Members core |
| Membership packages and billing | OUT OF SCOPE / FUTURE MODULE | No tables, routes, or side effects |
| TOP GYM single-DB/legacy routes and role grants | LEGACY DEFECT / REFERENCE ONLY | Do not copy; use LogicFit contracts |

## Gate conclusion

The six open items above are genuine implementation-blocking contract decisions. No code, schema, seed, client feature, database write, or TOP GYM change was made while recording them.
