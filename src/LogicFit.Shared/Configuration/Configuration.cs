namespace LogicFit.Shared;

public sealed class SqlServerOptions
{
    public string Server { get; set; } = "localhost";
    public string ControlPlaneDatabase { get; set; } = "LogicFit_ControlPlane_Local";
    public string DefaultGymDatabase { get; set; } = "LogicFit_Gym_001_Local";
    public bool IntegratedSecurity { get; set; } = true;
    public string? User { get; set; }
    public string? Password { get; set; }
    public bool TrustServerCertificate { get; set; } = true;
    public bool Encrypt { get; set; } = false;
}

public sealed class LogicFitRuntimeOptions
{
    public string Environment { get; set; } = "Development";
    public string Version { get; set; } = "0.1.0";
    public string CorsOrigins { get; set; } = "http://localhost:5173";
    public string MfaIssuer { get; set; } = "LogicFit";
    public int PasswordMinimumLength { get; set; } = 12;
    public int SessionIdleTimeoutSeconds { get; set; } = 1800;
    public int SessionAbsoluteLifetimeSeconds { get; set; } = 28800;
    public int MfaChallengeSeconds { get; set; } = 300;
    public int PasswordResetSeconds { get; set; } = 900;
}
