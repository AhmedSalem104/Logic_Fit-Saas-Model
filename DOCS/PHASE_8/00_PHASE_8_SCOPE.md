# Phase 8 — Members Scope

**Contract status:** BLOCKED — contract audit complete, implementation not authorized
**Audit date:** 2026-08-29
**Module:** Members (first approved business module)

## Purpose

This package closes the Phase 8 Members specification before any production or business implementation. It records the requirements that are already authoritative, the requirements implied by those contracts, and the decisions that still require explicit product or security approval.

No C#, EF migration, SQL change, seed change, API implementation, React feature, Flutter feature, test implementation, or database write is authorized by this document.

## Authority reviewed

- `DOCS/MASTER_INDEX.md`
- All available `DOCS/PHASE_2/` contracts, especially the database, API, screen, flow, permission, dependency, member, and QR contracts
- `DOCS/PHASE_3/` canonical seed contracts
- `DOCS/PHASE_4/` foundation and architecture contracts
- `DOCS/PHASE_5/` authentication/RBAC and Members readiness material
- `DOCS/PHASE_6/` Platform Foundation contracts
- `DOCS/PHASE_7/` provisioning contracts and implementation material
- `DOCS/TOP_GYM_*` source-reference/audit documents

The root files `DECISION_LOCK.md`, `IMPLEMENTATION_ROADMAP.md`, and `CODEX_KICKOFF.md` are absent. They were not reconstructed. The repository documentation itself states that their absence is not permission to invent decisions.

## Locked initial scope

Phase 8 contains the Members core slice only:

- list Members;
- create a Member;
- read Member details;
- update a Member;
- delete/archive a Member according to the closed lifecycle decision;
- read a Member timeline;
- Web screens `MEM-W-001`, `MEM-W-002`, and `MEM-W-003`;
- Flutter screens `F-MEM-001` and `F-MEM-002`;
- the five existing Members permission identifiers:
  `members.read`, `members.create`, `members.update`, `members.delete`, and `members.export`.

There is no operational Member seed data. Phase 3 library seeds are unchanged.

## Explicitly outside the core slice

The following are dependencies or later phases, not Phase 8 core implementation:

- membership packages, subscriptions, billing, payments, renewals, and refunds;
- attendance (`F-MEM-003` remains separately scoped);
- measurements, training, nutrition, store, finance, CRM, classes, reports, notifications, and documents;
- Member Portal authentication changes;
- QR implementation;
- provisioning and Platform Foundation work.

## Requirement classification

| Classification | Phase 8 result |
|---|---|
| A — Locked | Architecture, Gym database boundary, core table name and approved fields, route family and operation set, permission identifiers, screen IDs, no Member seed data, API envelope/error conventions, Phase 8 business boundary |
| B — Implied by locked contract | Server-side Gym authorization, reuse of Phase 5B authentication/RBAC/audit, EF Core-only persistence, row-version concurrency protection, soft-delete/archive intent where history exists, no direct client/database access |
| C — Contract gap | Complete DTOs, query allowlists, status values/transitions, uniqueness/idempotency rules, timeline event source and payload, exact privacy field allowlists, profile tab boundary |
| D — Product/security decision required | Concrete role grants for the five permissions, lifecycle/delete semantics, duplicate and retry policy, timeline scope, profile surface, exact Member Code/Portal policy if included |
| E — Contract conflict/drift | Phase 2 profile screens enumerate linked future-domain tabs, while the locked core scope excludes those domains; the timeline source catalog also references future domains |

## Phase boundaries

- Phase 5B owns authentication, sessions, MFA, RBAC, Gym context, and audit infrastructure.
- Phase 6 owns Platform Foundation and must not expose unrestricted Gym Member business data.
- Phase 7 owns provisioning and must not create Member records.
- Phase 8 owns the scoped Member profile and timeline contract only.
- Memberships, attendance, measurements, and other operational modules remain separate vertical slices.

## Current gate result

The Phase 8 contract is **BLOCKED** until the exact decisions in `16_PHASE_8_GAP_REGISTER.md` are approved. The audit deliberately does not infer business behavior from TOP GYM or from common SaaS practice.
