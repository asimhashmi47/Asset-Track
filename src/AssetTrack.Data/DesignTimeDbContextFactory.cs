using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AssetTrack.Data;

/// <summary>Lets `dotnet ef migrations` construct the context without the WPF app's DI container.</summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AssetTrackDbContext>
{
    public AssetTrackDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AssetTrackDbContext>();
        optionsBuilder.UseSqlite("Data Source=design_time.db");
        return new AssetTrackDbContext(optionsBuilder.Options);
    }
}
