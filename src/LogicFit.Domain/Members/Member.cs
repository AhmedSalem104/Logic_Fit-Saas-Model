namespace LogicFit.Domain.Members;

public static class MemberStatuses
{
    public const string Active = "ACTIVE";
    public const string Inactive = "INACTIVE";
    public const string Archived = "ARCHIVED";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>(StringComparer.Ordinal) { Active, Inactive, Archived };

    public static IReadOnlySet<string> NormalUpdateStatuses { get; } =
        new HashSet<string>(StringComparer.Ordinal) { Active, Inactive };
}

public sealed record MemberProfile(
    Guid MemberId,
    Guid GymId,
    string MemberCode,
    string FullName,
    string Phone,
    string? Email,
    DateOnly RegistrationDate,
    string? Notes,
    string Status)
{
    public MemberProfile Update(
        string fullName,
        string phone,
        string? email,
        DateOnly registrationDate,
        string? notes,
        string status)
    {
        if (Status == MemberStatuses.Archived)
        {
            throw new InvalidOperationException("Archived members cannot be updated.");
        }

        if (!MemberStatuses.NormalUpdateStatuses.Contains(status))
        {
            throw new ArgumentException("A normal member update must use an active or inactive status.", nameof(status));
        }

        return this with
        {
            FullName = fullName,
            Phone = phone,
            Email = email,
            RegistrationDate = registrationDate,
            Notes = notes,
            Status = status
        };
    }

    public MemberProfile Archive()
    {
        if (Status == MemberStatuses.Archived)
        {
            return this;
        }

        return this with { Status = MemberStatuses.Archived };
    }
}
