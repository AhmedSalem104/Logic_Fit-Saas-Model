# Phase 7 - Contract Gap Register

**Status:** GREEN - all Phase 7 contract gaps are closed by the final human
approvals and synchronized contract documents. Implementation is authorized
and the implementation status is recorded separately.

| ID | Area | Classification | Resolution |
|---|---|---|---|
| P7-G-001 | Authority files | A | Root kickoff files are absent by approved P6-D-001; do not reconstruct them. |
| P7-G-002 | Phase boundary | A | Phase 6/7/8 ownership is closed by P7-D-012. |
| P7-G-003 | Topology | A | Control Plane plus database-per-Gym remains locked. |
| P7-G-004 | EF/seed authority | A | EF Core and the existing .NET Phase 3 seed executor are the only authorities. |
| P7-G-005 | Provisioning flow | B | Async request, worker, status, retry, and activation flow are closed. |
| P7-G-006 | Provisioning permission | D | `platform.provision`, existing `platform-security-admin` grant, verified MFA step-up, and audit are approved. |
| P7-G-007 | Organization/Gym creation | D | Nested request input creates new Control Plane registry rows; existing Gym IDs are not accepted. |
| P7-G-008 | Plan input | D | No client plan field and no commercial behavior; technical metadata requires a future contract. |
| P7-G-009 | API schemas | D | The three exact routes and complete schemas are in `03_PROVISIONING_API_CONTRACT.md`. |
| P7-G-010 | Lifecycle | D | Exact run states, transitions, failure mapping, and retry property are in `05_PROVISIONING_LIFECYCLE.md`. |
| P7-G-011 | Placement/naming | D | Registered server target and `LogicFit_Gym_{gymId:N}_{environment}` are closed. |
| P7-G-012 | Provisioning tables | C | Complete run/step/server/database fields, keys, indexes, and secret exclusions are in `02_PROVISIONING_DATABASE_CONTRACT.md`. |
| P7-G-013 | Owner timing/activation | D | Owner is initialized after verification and before activation through Phase 5B and receives the existing `gym-security-admin` role. |
| P7-G-014 | Partial recovery | D | Same-run/same-target retry, retention, ownership proof, restart recovery, and no cleanup are closed. |
| P7-G-015 | Backup/restore | D | Explicitly excluded from fresh provisioning. |
| P7-G-016 | Migration/seed exposure | D | Internal workflow steps only; no public APIs or extra permissions. |
| P7-G-017 | Monitoring/status | C | Status is the existing operation GET; Phase 6 monitoring may read safe registry status; no second system. |
| P7-G-018 | Audit detail | D | Exact nine-event vocabulary and safe metadata are closed. |
| P7-G-019 | Web screen | B/D | Existing `PA-W-004` is finalized as `/platform-admin/provisioning/:runId` with API/state/action contracts. |
| P7-G-020 | Flutter | A | `NO FLUTTER UI REQUIRED FOR PHASE 7 PROVISIONING`. |
| P7-G-021 | Backup execution | A | Deferred to separate Platform Operations capability. |
| P7-G-022 | Phase 8 Members | A | Members and all member business data remain Phase 8. |
| P7-G-023 | Canonical Owner role key | D - resolved by human approval | Gym Owner is explicitly mapped to the existing canonical `gym-security-admin` role. No new role key, alias, rename, or unrelated role grant is created. The provisioning actor remains `platform-security-admin` with `platform.provision`; Gym Owner cannot provision another Gym. |

## Gate rule

P7-G-023 is resolved. No Phase 7 contract blocker remains. The Phase 5B role
catalog and role grants remain canonical; Phase 7 adds only the approved
`platform.provision` permission/grant through the implementation migration.
No TOP GYM change was made.
