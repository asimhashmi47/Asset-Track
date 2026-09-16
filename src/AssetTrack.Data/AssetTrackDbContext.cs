using AssetTrack.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssetTrack.Data;

public class AssetTrackDbContext(DbContextOptions<AssetTrackDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<LookupItem> LookupItems => Set<LookupItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AssetTrackDbContext).Assembly);
    }
}
