# LogicFit Approval Permission Matrix

**Decision ID:** SC-016  
**Status:** APPROVED  
**Approved:** 2026-08-25  
**Scope:** Training and Nutrition plan lifecycle approval; permissions are the authority, not hard-coded role names.

## Permission catalog

| Domain | Permission |
|---|---|
| Training | `training.create`, `training.edit`, `training.submit_review`, `training.review`, `training.approve`, `training.publish` |
| Nutrition | `nutrition.create`, `nutrition.edit`, `nutrition.submit_review`, `nutrition.review`, `nutrition.approve`, `nutrition.publish` |

The same lifecycle semantics apply to Manual, Automatic, AI, and Hybrid creation modes. AI may propose content through an adapter; it has no permission to approve or publish.

## State/action matrix

| Permission | Required state | Authorized action | Required audit facts | Constraint |
|---|---|---|---|---|
| `*.create` | none or new Draft | Create a Draft | creator, tenant/Gym, source mode, timestamp | Backend validates member/context and scope |
| `*.edit` | Draft | Edit Draft content | editor, changed fields, timestamp | Published snapshots are immutable |
| `*.submit_review` | Draft | Move Draft to Review | submitter, timestamp, version | Submitter must have tenant/Gym scope |
| `*.review` | Review | Review, request changes, or record review outcome | reviewer, outcome, notes, timestamp | Review does not itself approve or publish |
| `*.approve` | Review with required review complete | Move to Approved | approver, reason/notes, timestamp | If approval workflow is required, approver cannot equal creator |
| `*.publish` | Approved | Publish and create immutable snapshot | publisher, snapshot/version, timestamp | Explicit permission is required; no implicit Platform Admin bypass |

`*` means the matching `training` or `nutrition` prefix. A request is allowed only when the actor has the exact permission, the correct tenant/Gym scope, and the current state permits the transition. The backend re-checks all conditions; UI visibility is not authorization.

## Role/permission profile matrix

These are configurable role profiles, not hard-coded authority. A deployment may grant or remove permissions, but the backend always evaluates the resulting permission set and scope.

| Configurable profile | Training baseline | Nutrition baseline | Intended capability |
|---|---|---|---|
| Creator | create, edit, submit_review | create, edit, submit_review | Build and submit Drafts; cannot self-approve |
| Reviewer | review | review | Review and return for changes; cannot approve or publish unless separately granted |
| Approver | approve | approve | Approve eligible Review plans; never bypasses creator separation |
| Publisher | publish | publish | Publish Approved plans and create snapshots |
| Gym Owner/Manager | Explicitly assigned permissions only | Explicitly assigned permissions only | No implicit authority; may combine capabilities only through grants |
| Platform Admin | No implicit Gym-plan permissions | No implicit Gym-plan permissions | Control Plane access does not grant Gym-plan approval/publish without explicit scoped grants |

The matrix is permission-based even when one user has multiple profiles. If the same person creates a plan, that person cannot approve that same plan where the approval workflow is required, even if they possess `*.approve`; another eligible actor must approve it. A user may publish only after the Approved state and exact `*.publish` permission are verified.

## Required backend checks

- Resolve the actor's permissions from the authorization store for the target tenant/Gym.
- Enforce lifecycle state transitions server-side.
- Enforce creator/approver separation for the same plan.
- Enforce tenant isolation and member scope.
- Record denied attempts and successful transitions in audit logs.
- Do not infer permission from a role label, UI route, JWT display claim, or Platform Admin status.

## Minimum negative tests

The later vertical-slice tests must reject: a creator approving their own plan; a reviewer approving without `*.approve`; a publisher publishing a Draft or Review plan; a Platform Admin without an explicit scoped permission; cross-Gym access; client-only permission claims; and edits to a Published snapshot.
