using AssetTrack.Core.Enums;

namespace AssetTrack.Core.Entities;

public class ActivityLog
{
    public int Id { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    public int ActorUserId { get; set; }
    public User? ActorUser { get; set; }

    public ActivityEventType EventType { get; set; }

    public int? AssetId { get; set; }
    public Asset? Asset { get; set; }

    public int? TargetUserId { get; set; }
    public User? TargetUser { get; set; }

    public ReturnCondition? Condition { get; set; }
    public string? Notes { get; set; }
    public required string Description { get; set; }
}
