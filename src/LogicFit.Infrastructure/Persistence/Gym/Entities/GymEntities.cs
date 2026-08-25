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
