# Phase 7 - Provisioning Web Contract

**Status:** GREEN - the approved Platform Admin provisioning Web flow is implemented and verified in direct Chrome.
**Client:** React Web through the ASP.NET Core API only.
**Mobile:** no Platform provisioning UI.

## Screen

| Screen ID | Canonical route | Entry | APIs | Permission |
|---|---|---|---|---|
| `PA-W-004` | `/platform-admin/provisioning/:runId` | Existing Platform Admin organization/Gym registry action after a `202` response | `POST /api/v1/platform/provisioning`, `GET /api/v1/platform/provisioning/{runId}`, `POST /api/v1/platform/provisioning/{runId}/retry` | `platform.provision` for start/status/retry as defined by the API contract |

The Phase 2 identity `/platform/provisioning/:runId` remains the historical
screen reference; the Phase 6 Platform Admin route normalization is
`/platform-admin`.

## Start request UI

The existing Platform Admin organization/Gym entry point collects only the
contracted values: organization name/slug, Gym name/slug/timezone, registered
server ID selection, and Owner email/display name/initial password through a
protected local/admin workflow. It never accepts a physical database name,
Gym ID for a new request, connection string, credential, plan/billing field,
or private infrastructure detail.

No new screen ID is created for a provisioning form. `PA-W-004` is the
operation screen; the existing registry entry point supplies its request.

## Operation states and actions

The screen displays the exact run state values:

```text
Requested, Provisioning, Migrating, Seeding, Verifying, Active,
ProvisioningFailed, MigrationFailed, SeedingFailed, VerificationFailed
```

It displays current step, attempt, timestamps, safe server/database metadata,
owner-initialized status, and redacted failure category/code. It polls the
status API with bounded backoff. The retry action is enabled only when the
API returns `retryable=true`; it collects a safe reason and sends a new
idempotency key. There is no cancel action.

## Required UI states

- loading: initial request/status and polling indicator;
- empty: no operation data only when an authorized operation has no returned
  step payload, without treating cross-scope absence as a known resource;
- success: `Active`, with safe completion summary;
- failure: one of the four failure states, safe message, request ID, and
  retry affordance only when allowed;
- disabled: controls disabled during submission/poll retry or missing
  permission;
- validation: field-level API-compatible validation before submission;
- unauthorized/forbidden: standard `401`/`403` handling, no hidden-scope
  disclosure;
- responsive: desktop table/stepper and mobile stacked stepper;
- accessibility: keyboard focus, semantic status labels, and readable error
  association.

Arabic, RTL, light theme, dark theme, responsive layouts, and the existing
LogicFit design system are required. No client-side lifecycle transition or
permission decision is authoritative. No connection string, secret, raw
error payload, or owner password is rendered.

## Traceability and testing

The screen maps to `FLOW-PLAT-001`, the three API routes, the Control Plane
run/step records, the controlled audit events, and the API/browser tests in
`09_PROVISIONING_TRACEABILITY.md`. Flutter has no corresponding screen or
runtime route.

## Implementation mapping

`PlatformProvisioningPage` provides the contracted form at
`/platform-admin/provisioning` and the operation view at
`/platform-admin/provisioning/:runId`. It generates opaque idempotency keys,
submits only the approved request fields, polls the status endpoint, and
offers retry only when the server reports `retryable=true`. Its tests cover
request headers/body redaction, active completion, and retryable failure.
