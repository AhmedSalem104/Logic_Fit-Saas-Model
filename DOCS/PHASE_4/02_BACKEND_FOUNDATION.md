# Backend Foundation

## Technology

- ASP.NET Core Web API.
- C# with nullable/implicit-using project defaults.
- .NET 10, pinned by `global.json`.
- Entity Framework Core with SQL Server.
- Visual Studio-compatible `LogicFit.sln`.

## Implemented boundary

`src/LogicFit.Api/Program.cs` owns process bootstrap, graceful shutdown, and listening. The official API currently exposes only:

- `GET /api/v1/health`
- `GET /api/v1/readiness`
- `GET /api/v1/version`

All responses use the Phase 2 envelope and carry a request ID. 404, rate-limit, validation, and internal failures use sanitized error envelopes.

## Database boundary

`ControlPlaneDbContext`, `GymDbContext`, `IGymDatabaseResolver`, and `GymDbContextFactory` own the separate Control Plane/Gym boundary. A future request context must resolve actor, scope, registry record, and Gym database before acquiring a Gym context. No client-supplied database name or connection string is accepted by the API.

## Foundation adapters

- `SqlServerConnectionFactory`: connection/health abstraction.
- `Pbkdf2PasswordHasher`: native password-hash implementation.
- `SqlSessionStore`: hash-only SQL-backed session persistence abstraction.
- `TotpService`: native Authenticator App/TOTP primitive.

The full authentication workflow and domain services are intentionally deferred to Phase 5 and later vertical slices.
