# Phase 8 Members Decision Register

**Status:** BLOCKED — decisions below are not silently resolved

Each unresolved decision must be approved before implementation. “Human approval required” is intentional and is not a product decision made by this audit.

## P8-D-001 — Authority and scope

- **Problem:** Establish whether legacy TOP GYM behavior can override LogicFit contracts.
- **Evidence:** Phase 2–7 LogicFit documents and source-consolidation decision make LogicFit authority; TOP GYM is read-only reference.
- **Options:** Use LogicFit authority; copy TOP GYM; merge both.
- **Selected:** Use LogicFit authority; TOP GYM is evidence only.
- **Impact:** All layers; prevents legacy single-DB/business-scope leakage.
- **Status:** RESOLVED.

## P8-D-002 — Core Member fields

- **Problem:** Define the core profile without future business data.
- **Evidence:** Phase 2 Member contract and table catalog list Member ID, Gym reference, full name, phone, optional email, registration date, optional notes, status, audit/version metadata.
- **Options:** Use only those fields; import TOP GYM membership/payment fields; add common CRM/health fields.
- **Selected:** Use only the locked core field set.
- **Impact:** Database, API, Web, Flutter, privacy.
- **Status:** RESOLVED for field names/limits; requiredness of registration date and status semantics remain P8-G-001/P8-G-004.

## P8-D-003 — Database boundary and seeding

- **Problem:** Decide where Member data and initial records live.
- **Evidence:** Control Plane + database-per-Gym architecture; Phase 3 explicitly excludes operational Member seed data.
- **Options:** Control Plane/shared tenant DB; selected Gym DB; demo Member seeds.
- **Selected:** Selected Gym DB; no Member seed data.
- **Impact:** Database, API scope, provisioning dependency, tests.
- **Status:** RESOLVED.

## P8-D-004 — Concrete role grants

- **Problem:** The five approved permission keys have no concrete grants in the current three-role assignment catalog.
- **Evidence:** Permission contract lists keys; current runtime has three roles and no `members.*` grants; Platform Admin has no implicit Gym business access.
- **Options:** Grant all to Gym Security Admin; split read/write across Gym roles; grant to authenticated users; grant to Platform Admin; add a role.
- **Recommended:** Human/security approval required; no option selected by this audit.
- **Impact:** Permission catalog/seed, API authorization, Web/Flutter actions, security tests.
- **Status:** UNRESOLVED — P8-G-002.

## P8-D-005 — Member lifecycle and delete

- **Problem:** `status` exists, but values/transitions and DELETE semantics are not canonical.
- **Evidence:** Phase 2 requires history-preserving archive/soft-delete; API uses DELETE; TOP GYM statuses are legacy.
- **Options:** Explicit Active/Inactive/Archived model; another approved lifecycle; physical delete; archive only.
- **Recommended:** Human product/security approval required; physical destructive deletion is not supported by existing history rules.
- **Impact:** Schema, API, list visibility, audit, UI, tests.
- **Status:** UNRESOLVED — P8-G-001.

## P8-D-006 — Duplicate, uniqueness, and idempotency

- **Problem:** Exact phone/email uniqueness and create retry semantics are not defined.
- **Evidence:** Phase 2 says no duplicate policy is invented and email uniqueness is configurable; common API contract defines idempotency for retryable creates but does not close the Member fingerprint.
- **Options:** Per-Gym unique phone/email; allow duplicates; warning-only; explicit idempotent create key; no idempotency.
- **Recommended:** Human product/security approval required; no option selected.
- **Impact:** DB indexes, create API, concurrency, UX, audit, tests.
- **Status:** UNRESOLVED — P8-G-003.

## P8-D-007 — API schemas and query model

- **Problem:** Routes and field concepts exist, but complete request/response/query contracts do not.
- **Evidence:** API catalog provides operation descriptions and common envelope; no complete Member JSON schemas or endpoint-specific search/sort/page allowlist.
- **Options:** Adopt TOP GYM behavior; adopt a new implicit schema; close an explicit LogicFit schema.
- **Recommended:** Explicit LogicFit schema approval required; no implicit or legacy schema selected.
- **Impact:** All API/client/test/documentation consumers.
- **Status:** UNRESOLVED — P8-G-004.

## P8-D-008 — Timeline source and event scope

- **Problem:** Timeline storage and route are catalogued, but the source list includes future modules excluded from this slice.
- **Evidence:** Phase 2 timeline table/route and future-domain projection description; Phase 8 core excludes those domains.
- **Options:** Core Member/audit events only; include future sources now; empty projection until future modules; another explicitly scoped set.
- **Recommended:** Human product/privacy approval required; no option selected.
- **Impact:** Timeline table/projection, API, privacy, Web/Flutter, tests.
- **Status:** UNRESOLVED — P8-G-005.

## P8-D-009 — Core profile surface

- **Problem:** MEM-W-003/F-MEM-002 list linked future-domain tabs, while the Phase 8 initial scope excludes them.
- **Evidence:** Phase 2 screen catalogs versus Phase 8 locked scope and explicit Attendance boundary.
- **Options:** Core identity/timeline only; tabs only when each future contract is available; implement linked modules now; placeholders.
- **Recommended:** Human product/UI approval required; no option selected. Placeholder business UI is not authorized by this audit.
- **Impact:** Web/Flutter routes, API calls, privacy, E2E.
- **Status:** UNRESOLVED — P8-G-006.

## P8-D-010 — Export

- **Problem:** `members.export` exists, but no export route or output contract exists.
- **Evidence:** Permission contract lists the key; API catalog has no export endpoint; Phase 8 says do not implement unless explicit.
- **Selected:** Retain documented permission identifier; defer export and do not infer a route.
- **Impact:** Permission traceability only; no Phase 8 implementation.
- **Status:** RESOLVED as deferred/out of scope.

## P8-D-011 — Member Portal policy

- **Problem:** Portal uses Member Code, but exact code policy is not fully specified.
- **Evidence:** Portal authentication flow is locked at a high level; exact format/rotation/reveal and safe projection are absent.
- **Selected:** Preserve Portal authentication and defer code-policy closure to the Portal contract. Do not change it in Members core.
- **Impact:** Future Portal slice; no current Member API/UI change.
- **Status:** Deferred dependency; becomes a blocker only if Portal behavior is brought into Phase 8 core.

## P8-D-012 — No operational seed

- **Problem:** Prevent demo/Member records from entering canonical seeds.
- **Evidence:** Phase 3 explicitly excludes Members and operational data.
- **Selected:** No Member seed data.
- **Impact:** DB initialization and tests.
- **Status:** RESOLVED.
