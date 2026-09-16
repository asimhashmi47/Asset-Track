using AssetTrack.Core.Enums;

namespace AssetTrack.Core.Entities;

public class Asset
{
    public int Id { get; set; }
    public required string AssetTag { get; set; }
    public required string Name { get; set; }
    public required string Category { get; set; }
    public required string SerialNumber { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public AssetStatus Status { get; set; } = AssetStatus.Available;

    public int? CurrentUserId { get; set; }
    public User? CurrentUser { get; set; }

    public required string Company { get; set; }
    public string? Division { get; set; }
    public string? City { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ActivityLog> History { get; set; } = new List<ActivityLog>();
}
