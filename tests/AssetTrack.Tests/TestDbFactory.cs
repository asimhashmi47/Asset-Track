using AssetTrack.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetTrack.Tests;

/// <summary>Hands out fresh contexts backed by the same named InMemory database, mirroring
/// how the app's IDbContextFactory-based services work in production.</summary>
public class TestDbContextFactory(string databaseName) : IDbContextFactory<AssetTrackDbContext>
{
    public AssetTrackDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AssetTrackDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return new AssetTrackDbContext(options);
    }

    public Task<AssetTrackDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(CreateDbContext());
}

public static class TestDbFactory
{
    /// <summary>A single throwaway context — for tests that only need direct DbSet access (seeding, assertions).</summary>
    public static AssetTrackDbContext Create() => CreateFactory().CreateDbContext();

    /// <summary>An IDbContextFactory, for constructing the Ef*Service classes under test.</summary>
    public static TestDbContextFactory CreateFactory() => new(Guid.NewGuid().ToString());
}
