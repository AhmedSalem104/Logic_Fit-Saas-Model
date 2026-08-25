# Database scripts

Local SQL Server transition helpers are explicit, database-name-guarded scripts. The normal migration entry point is the .NET API command:

```powershell
dotnet run --project src/LogicFit.Api/LogicFit.Api.csproj -- --migrate
```

The `phase5-local-ef-baseline.sql` and `phase5-local-anatomy-schema-alignment.sql` files are one-time LOCAL transition evidence for the existing draft databases. They are not the application migration system and must not be used against TOP GYM, staging, or production.
