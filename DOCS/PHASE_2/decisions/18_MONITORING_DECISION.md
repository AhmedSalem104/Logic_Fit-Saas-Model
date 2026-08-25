# Decision 2.18 — Monitoring and Operational Thresholds

**Status:** APPROVED  
**Authority:** Phase 2 Final Gap Resolution  
**Scope:** Platform operations.

## Decision

Monitor application health, database health/connectivity, request errors, HTTP 500, HTTP 401/403 patterns, API latency, migration failures, backup failures, provisioning failures, and storage failures.

Default configurable thresholds are: latency warning above one second and critical above three seconds; sustained/repeated 5xx is warning and a five-percent error rate over the monitoring window is critical; any database connectivity failure is critical; backup failure is critical; any Gym migration failure is critical/partial failure; and any provisioning-step failure yields failed provisioning. Thresholds are operational configuration, not business logic.
