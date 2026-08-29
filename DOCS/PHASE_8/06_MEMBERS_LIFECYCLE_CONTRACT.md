# Members Lifecycle and Concurrency Contract

**Status:** BLOCKED — canonical Member status and delete semantics require approval

## Evidence already available

- The Member contract includes a `status` field but does not enumerate a canonical Member lifecycle.
- The API catalog calls the DELETE operation an archive/soft-delete operation.
- The permission contract describes `members.delete` as a high-risk archive operation where history exists.
- The database contract requires history-preserving deletion using deletion metadata or an explicit archive state.
- TOP GYM statuses and membership statuses are legacy evidence, not LogicFit Member lifecycle authority.

## Required closed lifecycle

The implementation-ready contract must specify, without inference:

1. the complete Member status vocabulary;
2. the initial status on create;
3. every allowed transition and the authorized permission/actor;
4. whether inactive and archived Members appear in default list results;
5. whether an archived Member can be restored and by whom;
6. the exact meaning of `DELETE /members/{memberId}`;
7. whether archive metadata, an explicit status, or both are persisted;
8. whether physical purge is ever allowed and under what separate authority.

Until this is approved, no status enum, default, transition, or delete response may be invented.

## Concurrency

The existing contract requires optimistic concurrency for mutable roots using the opaque row-version convention. The closed API must define:

- the response version field and `If-Match` representation;
- whether create idempotency is mandatory and how the request is fingerprinted;
- the exact `409` payload for stale updates;
- behavior when archive/delete races with update or another archive;
- whether an already archived Member receives success, `404`, or a defined idempotent response.

These are tracked in P8-G-003 and P8-G-004. No physical deletion is authorized by this audit.

## Audit

Creation, profile changes, status/archive changes, and concurrency/security violations must use the existing audit system. Audit payloads must contain safe identifiers and change metadata only; no secrets or unnecessary personal data.
