# P6-D-010 — Platform API Schemas

**Status:** APPROVED

## Problem

The Phase 2 endpoint catalog names broad Platform routes but does not provide
complete endpoint-specific DTOs, filters, errors, and source rules.

## Existing evidence

The generic API contract fixes `/api/v1`, envelopes, request IDs, pagination,
filtering, safe errors, and redaction. The Phase 6 approval requires a
complete contract before implementation and preserves the existing namespace.

## Options

1. Implement broad catalog routes without endpoint schemas.
2. Admit only a fully specified read-only route set and defer every other
   route.

## Recommendation

**Selected: Option 2.** The canonical Phase 6 API is exactly the eight
read-only routes in `03_PLATFORM_API_CONTRACT.md`:

- `GET /api/v1/platform/overview`
- `GET /api/v1/platform/organizations`
- `GET /api/v1/platform/organizations/{organizationId}`
- `GET /api/v1/gyms`
- `GET /api/v1/gyms/{gymId}`
- `GET /api/v1/platform/databases`
- `GET /api/v1/platform/databases/{databaseId}`
- `GET /api/v1/platform/monitoring`

Each route has exact auth, permission, scope, DTO, query, validation, error,
source, redaction, and audit behavior in the canonical contract.

## Impact

There is one route per admitted operation, no compatibility namespace, and no
implementation authorization for an incomplete or deferred route.

## Affected surfaces

- **DB:** DTO fields map only to existing Control Plane registry/health sources.
- **API:** Exact route and schema list is canonical.
- **Permissions:** Existing `platform.view` for every admitted route.
- **Web:** `PA-W-001`, `PA-W-002`, `PA-W-003`, `PA-W-005`, and `PA-W-009`.
- **Flutter:** No Platform UI; future clients use the same API contract.
- **Tests:** Contract snapshots, API source/scope/redaction, authorization,
  filtering, pagination, and dependency-failure tests.
