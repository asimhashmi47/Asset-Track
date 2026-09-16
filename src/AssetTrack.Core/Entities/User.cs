using AssetTrack.Core.Enums;

namespace AssetTrack.Core.Entities;

public class User
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public required string PasswordSalt { get; set; }
    public required string DisplayName { get; set; }
    public UserRole Role { get; set; } = UserRole.Staff;
    public required string Company { get; set; }
    public required string Division { get; set; }
    public required string City { get; set; }
    public string? Designation { get; set; }
    public string? Contact { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Asset> AssignedAssets { get; set; } = new List<Asset>();
}
