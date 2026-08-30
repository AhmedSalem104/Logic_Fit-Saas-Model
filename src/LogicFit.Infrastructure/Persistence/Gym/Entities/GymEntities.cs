namespace LogicFit.Infrastructure.Persistence.Entities;

public sealed class GymContextEntity
{
    public Guid GymContextId { get; set; }
    public Guid ControlPlaneGymId { get; set; }
    public string GymCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string TimezoneName { get; set; } = "Africa/Cairo";
    public string Status { get; set; } = "provisioning";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class GymUserEntity
{
    public Guid GymUserId { get; set; }
    public Guid ControlPlaneUserId { get; set; }
    public string Status { get; set; } = "active";
    public string DisplayName { get; set; } = string.Empty;
    public DateTime? LastPermissionSyncAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class GymAuditEventEntity
{
    public Guid AuditEventId { get; set; }
    public string? RequestId { get; set; }
    public Guid? ActorUserId { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public Guid? TargetId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime OccurredAtUtc { get; set; }
}

public sealed class MemberEntity
{
    public Guid MemberId { get; set; }
    public Guid GymId { get; set; }
    public string MemberCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateOnly RegistrationDate { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public string CreateIdempotencyKeyHash { get; set; } = string.Empty;
    public string CreateRequestFingerprint { get; set; } = string.Empty;
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public ICollection<MemberTimelineEventEntity> TimelineEvents { get; } = new List<MemberTimelineEventEntity>();
}

public sealed class MemberTimelineEventEntity
{
    public Guid TimelineEventId { get; set; }
    public Guid MemberId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime EventAtUtc { get; set; }
    public Guid? ActorUserId { get; set; }
    public string SourceType { get; set; } = "member";
    public Guid? SourceId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public MemberEntity? Member { get; set; }
}
