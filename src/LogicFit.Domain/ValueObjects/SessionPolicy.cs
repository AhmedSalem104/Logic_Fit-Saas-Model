namespace LogicFit.Domain.ValueObjects;

public sealed record SessionPolicy(
    TimeSpan IdleTimeout,
    TimeSpan AbsoluteLifetime,
    TimeSpan MfaChallengeLifetime,
    TimeSpan PasswordResetLifetime);
