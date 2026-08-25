# Observability Foundation

ASP.NET Core JSON structured logs include:

- request ID
- method and URL
- HTTP status
- request duration
- error category for failures
- bootstrap/shutdown events

Database health is surfaced through readiness and database error handling. Health is liveness-only; readiness checks both configured Control Plane and default Gym SQL connections.

The foundation deliberately omits password, token, session, TOTP, SQL password, and member payload logging. Monitoring thresholds/alerts remain the approved Platform Operations contract and are not hard-coded into business logic in Phase 4.
