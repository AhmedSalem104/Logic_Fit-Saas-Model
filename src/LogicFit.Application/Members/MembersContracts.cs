using LogicFit.Domain.Members;

namespace LogicFit.Application;

public sealed record MemberListQuery(
    int Page,
    int PageSize,
    string? Search,
    IReadOnlySet<string> Statuses,
    string SortField,
    bool SortDescending);

public sealed record CreateMemberCommand(
    string? FullName,
    string? Phone,
    string? Email,
    DateOnly? RegistrationDate,
    string? Notes);

public sealed record UpdateMemberCommand(
    string? FullName,
    string? Phone,
    string? Email,
    DateOnly? RegistrationDate,
    string? Notes,
    string? Status);

public sealed record MemberSummaryDto(
    Guid MemberId,
    string MemberCode,
    string FullName,
    string Phone,
    string? Email,
    DateOnly RegistrationDate,
    string Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string Version);

public sealed record MemberDetailDto(
    Guid MemberId,
    Guid GymId,
    string MemberCode,
    string FullName,
    string Phone,
    string? Email,
    DateOnly RegistrationDate,
    string? Notes,
    string Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string Version);

public sealed record MemberArchiveDto(
    Guid MemberId,
    string Status,
    DateTime ArchivedAtUtc,
    string Version);

public sealed record MemberTimelineItemDto(
    Guid EventId,
    Guid MemberId,
    Guid GymId,
    string EventType,
    DateTime OccurredAt,
    Guid? ActorId,
    IReadOnlyDictionary<string, string?> Metadata);

public sealed record MemberPageDto(
    IReadOnlyList<MemberSummaryDto> Items,
    int Page,
    int PageSize,
    int Total,
    bool HasNext);

public sealed record MemberTimelinePageDto(
    IReadOnlyList<MemberTimelineItemDto> Items,
    int Page,
    int PageSize,
    int Total,
    bool HasNext);

public static class MembersContract
{
    public const string ReadPermission = "members.read";
    public const string CreatePermission = "members.create";
    public const string UpdatePermission = "members.update";
    public const string ArchivePermission = "members.delete";
    public const string ExportPermission = "members.export";
    public const int DefaultPageSize = 25;
    public const int MaximumPageSize = 100;
    public const string DefaultSortField = "createdAt";

    public static IReadOnlySet<string> DefaultStatuses { get; } =
        new HashSet<string>(StringComparer.Ordinal) { MemberStatuses.Active, MemberStatuses.Inactive };

    public static IReadOnlySet<string> TimelineEventTypes { get; } =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "MEMBER_CREATED",
            "MEMBER_UPDATED",
            "MEMBER_ARCHIVED",
            "MEMBER_STATUS_CHANGED"
        };
}

public interface IMembersService
{
    Task<AuthResult<MemberPageDto>> ListAsync(
        AuthenticatedUser currentUser,
        Guid gymId,
        MemberListQuery query,
        AuthRequestContext context,
        CancellationToken cancellationToken = default);

    Task<AuthResult<MemberDetailDto>> CreateAsync(
        AuthenticatedUser currentUser,
        Guid gymId,
        CreateMemberCommand command,
        string? idempotencyKey,
        AuthRequestContext context,
        CancellationToken cancellationToken = default);

    Task<AuthResult<MemberDetailDto>> GetAsync(
        AuthenticatedUser currentUser,
        Guid gymId,
        Guid memberId,
        AuthRequestContext context,
        CancellationToken cancellationToken = default);

    Task<AuthResult<MemberDetailDto>> UpdateAsync(
        AuthenticatedUser currentUser,
        Guid gymId,
        Guid memberId,
        UpdateMemberCommand command,
        byte[] expectedVersion,
        AuthRequestContext context,
        CancellationToken cancellationToken = default);

    Task<AuthResult<MemberArchiveDto>> ArchiveAsync(
        AuthenticatedUser currentUser,
        Guid gymId,
        Guid memberId,
        byte[]? expectedVersion,
        AuthRequestContext context,
        CancellationToken cancellationToken = default);

    Task<AuthResult<MemberTimelinePageDto>> TimelineAsync(
        AuthenticatedUser currentUser,
        Guid gymId,
        Guid memberId,
        int page,
        int pageSize,
        AuthRequestContext context,
        CancellationToken cancellationToken = default);
}
