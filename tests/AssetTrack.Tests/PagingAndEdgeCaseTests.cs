using AssetTrack.Core.Entities;
using AssetTrack.Core.Enums;
using AssetTrack.Data.Services;
using Xunit;

namespace AssetTrack.Tests;

/// <summary>
/// Risk-based coverage for behavior the original 16 tests did not touch:
/// pagination math/boundaries, search filtering semantics, asset-tag generation
/// edge cases, MarkScrap from each valid prior status, activity-log paging.
/// </summary>
public class PagingAndEdgeCaseTests
{
    private static async Task<(TestDbContextFactory Factory, EfAssetService Service, User Admin, User Staff)> SetupAsync()
    {
        var factory = TestDbFactory.CreateFactory();
        await using var db = factory.CreateDbContext();
        var admin = new User { Username = "admin", PasswordHash = "x", PasswordSalt = "x", DisplayName = "Admin", Role = UserRole.Admin, Company = "Acme", Division = "IT", City = "Pune" };
        var staff = new User { Username = "staff", PasswordHash = "x", PasswordSalt = "x", DisplayName = "Staff One", Role = UserRole.Staff, Company = "Acme", Division = "Eng", City = "Pune" };
        db.Users.AddRange(admin, staff);
        await db.SaveChangesAsync();
        return (factory, new EfAssetService(factory), admin, staff);
    }

    private static Asset MakeAsset(string tag, string name, string category = "Laptop", AssetStatus status = AssetStatus.Available) =>
        new() { AssetTag = tag, Name = name, Category = category, SerialNumber = $"SN-{tag}", Company = "Acme", Status = status };

    // ---------- Pagination ----------

    [Fact]
    public async Task SearchAsync_LastPartialPage_ReturnsRemainderAndCorrectTotalPages()
    {
        var (factory, service, _, _) = await SetupAsync();
        await using (var db = factory.CreateDbContext())
        {
            for (var i = 1; i <= 25; i++)
                db.Assets.Add(MakeAsset($"AT-{1000 + i}", $"Asset {i:00}"));
            await db.SaveChangesAsync();
        }

        var page3 = await service.SearchAsync(null, null, null, page: 3, pageSize: 10);

        Assert.Equal(25, page3.TotalCount);
        Assert.Equal(3, page3.TotalPages);       // ceil(25/10)
        Assert.Equal(5, page3.Items.Count);      // remainder, not a full page
        Assert.Equal("AT-1021", page3.Items[0].AssetTag);
        Assert.Equal("AT-1025", page3.Items[4].AssetTag);
    }

    [Fact]
    public async Task SearchAsync_PagesDoNotOverlapOrSkipRows()
    {
        var (factory, service, _, _) = await SetupAsync();
        await using (var db = factory.CreateDbContext())
        {
            for (var i = 1; i <= 25; i++)
                db.Assets.Add(MakeAsset($"AT-{1000 + i}", $"Asset {i:00}"));
            await db.SaveChangesAsync();
        }

        var seen = new List<string>();
        for (var p = 1; p <= 3; p++)
        {
            var page = await service.SearchAsync(null, null, null, p, 10);
            seen.AddRange(page.Items.Select(a => a.AssetTag));
        }

        Assert.Equal(25, seen.Count);
        Assert.Equal(25, seen.Distinct().Count());
    }

    [Fact]
    public async Task SearchAsync_PageBeyondLast_ReturnsEmptyItemsButRealTotalCount()
    {
        var (factory, service, _, _) = await SetupAsync();
        await using (var db = factory.CreateDbContext())
        {
            for (var i = 1; i <= 5; i++)
                db.Assets.Add(MakeAsset($"AT-{1000 + i}", $"Asset {i}"));
            await db.SaveChangesAsync();
        }

        var result = await service.SearchAsync(null, null, null, page: 9, pageSize: 10);

        Assert.Empty(result.Items);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
    }

    // ---------- Search filtering ----------

    [Fact]
    public async Task SearchAsync_WhitespaceOnlySearchText_IsTreatedAsNoFilter()
    {
        var (factory, service, _, _) = await SetupAsync();
        await using (var db = factory.CreateDbContext())
        {
            db.Assets.Add(MakeAsset("AT-1001", "MacBook Pro"));
            db.Assets.Add(MakeAsset("AT-1002", "ThinkPad T14"));
            await db.SaveChangesAsync();
        }

        var result = await service.SearchAsync("   ", null, null, 1, 10);

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task SearchAsync_MatchesPartialTermCaseInsensitivelyAndCombinesWithStatusFilter()
    {
        var (factory, service, _, _) = await SetupAsync();
        await using (var db = factory.CreateDbContext())
        {
            db.Assets.Add(MakeAsset("AT-1001", "MacBook Pro 14", status: AssetStatus.Available));
            db.Assets.Add(MakeAsset("AT-1002", "MacBook Air 13", status: AssetStatus.Repair));
            db.Assets.Add(MakeAsset("AT-1003", "ThinkPad T14", status: AssetStatus.Available));
            await db.SaveChangesAsync();
        }

        var partial = await service.SearchAsync("macbook", null, null, 1, 10);
        Assert.Equal(2, partial.TotalCount);

        var withStatus = await service.SearchAsync("MACBOOK", AssetStatus.Repair, null, 1, 10);
        Assert.Equal(1, withStatus.TotalCount);
        Assert.Equal("AT-1002", withStatus.Items[0].AssetTag);

        var padded = await service.SearchAsync("  ThinkPad  ", null, null, 1, 10);
        Assert.Equal(1, padded.TotalCount);
    }

    [Fact]
    public async Task SearchAsync_CategoryFilterAllIsTreatedAsNoFilter()
    {
        var (factory, service, _, _) = await SetupAsync();
        await using (var db = factory.CreateDbContext())
        {
            db.Assets.Add(MakeAsset("AT-1001", "MacBook Pro", "Laptop"));
            db.Assets.Add(MakeAsset("AT-1002", "Dell U2723", "Monitor"));
            await db.SaveChangesAsync();
        }

        Assert.Equal(2, (await service.SearchAsync(null, null, "All", 1, 10)).TotalCount);
        Assert.Equal(1, (await service.SearchAsync(null, null, "Monitor", 1, 10)).TotalCount);
    }

    // ---------- Asset tag generation ----------

    [Fact]
    public async Task CreateAsync_OnEmptyInventory_StartsAtAT1001()
    {
        var (_, service, admin, _) = await SetupAsync();

        var created = await service.CreateAsync("First Laptop", "Laptop", "SN-X1", "Acme", DateTime.UtcNow, admin.Id);

        Assert.True(created.Success);
        Assert.Equal("AT-1001", created.Value!.AssetTag);
    }

    [Fact]
    public async Task CreateAsync_UsesHighestExistingNumberNotRowCount()
    {
        var (factory, service, admin, _) = await SetupAsync();
        await using (var db = factory.CreateDbContext())
        {
            db.Assets.Add(MakeAsset("AT-1005", "Existing"));   // gap: only one row, but high number
            await db.SaveChangesAsync();
        }

        var created = await service.CreateAsync("Next", "Laptop", "SN-X2", "Acme", DateTime.UtcNow, admin.Id);

        Assert.Equal("AT-1006", created.Value!.AssetTag);
    }

    [Fact]
    public async Task CreateAsync_WithNonNumericLegacyTagPresent_StillProducesAUniqueTag()
    {
        var (factory, service, admin, _) = await SetupAsync();
        await using (var db = factory.CreateDbContext())
        {
            db.Assets.Add(MakeAsset("LEGACY-A", "Imported asset"));
            db.Assets.Add(MakeAsset("AT-1003", "Normal asset"));
            await db.SaveChangesAsync();
        }

        var created = await service.CreateAsync("New", "Laptop", "SN-X3", "Acme", DateTime.UtcNow, admin.Id);

        Assert.True(created.Success);
        await using var verifyDb = factory.CreateDbContext();
        var allTags = verifyDb.Assets.Select(a => a.AssetTag).ToList();
        Assert.Equal(allTags.Count, allTags.Distinct().Count());
    }

    // ---------- MarkScrap from every valid prior status ----------

    [Theory]
    [InlineData(AssetStatus.Available)]
    [InlineData(AssetStatus.Repair)]
    [InlineData(AssetStatus.Assigned)]
    public async Task MarkScrapAsync_SucceedsFromEveryNonScrapStatusAndWritesLog(AssetStatus startStatus)
    {
        var (factory, service, admin, staff) = await SetupAsync();
        int assetId;
        await using (var db = factory.CreateDbContext())
        {
            var asset = MakeAsset("AT-1001", "Dock", "Docking Station", startStatus);
            if (startStatus == AssetStatus.Assigned) asset.CurrentUserId = staff.Id;
            db.Assets.Add(asset);
            await db.SaveChangesAsync();
            assetId = asset.Id;
        }

        var result = await service.MarkScrapAsync(assetId, "end of life", admin.Id);

        Assert.True(result.Success);
        var reloaded = await service.GetByIdAsync(assetId);
        Assert.Equal(AssetStatus.Scrap, reloaded!.Status);
        Assert.Null(reloaded.CurrentUserId);

        var history = await service.GetHistoryAsync(assetId);
        var log = Assert.Single(history, h => h.EventType == ActivityEventType.MarkedScrap);
        Assert.Equal(startStatus == AssetStatus.Assigned ? staff.Id : (int?)null, log.TargetUserId);
    }

    // ---------- Activity log paging ----------

    [Fact]
    public async Task ActivityLogSearchAsync_PagesNewestFirstWithCorrectTotals()
    {
        var (factory, _, admin, _) = await SetupAsync();
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await using (var db = factory.CreateDbContext())
        {
            for (var i = 0; i < 12; i++)
            {
                db.ActivityLogs.Add(new ActivityLog
                {
                    TimestampUtc = baseTime.AddMinutes(i),
                    ActorUserId = admin.Id,
                    EventType = ActivityEventType.AssetCreated,
                    Description = $"event {i}"
                });
            }
            await db.SaveChangesAsync();
        }
        var service = new EfActivityLogService(factory);

        var page1 = await service.SearchAsync(1, 5);
        var page3 = await service.SearchAsync(3, 5);

        Assert.Equal(12, page1.TotalCount);
        Assert.Equal(3, page1.TotalPages);
        Assert.Equal("event 11", page1.Items[0].Description);   // newest first
        Assert.Equal("event 7", page1.Items[4].Description);
        Assert.Equal(2, page3.Items.Count);                      // 12 - 10
        Assert.Equal("event 1", page3.Items[0].Description);
    }

    // ---------- User search paging + live asset counts ----------

    [Fact]
    public async Task UserSearchAsync_PagedResultsCarryCorrectAssignedAssetCounts()
    {
        var (factory, _, admin, staff) = await SetupAsync();
        await using (var db = factory.CreateDbContext())
        {
            db.Assets.Add(MakeAsset("AT-1001", "Laptop A", status: AssetStatus.Assigned));
            db.Assets.Add(MakeAsset("AT-1002", "Laptop B", status: AssetStatus.Assigned));
            await db.SaveChangesAsync();
            foreach (var a in db.Assets) a.CurrentUserId = staff.Id;
            await db.SaveChangesAsync();
        }

        var service = new EfUserService(factory);
        var result = await service.SearchAsync("staff", 1, 10);

        var item = Assert.Single(result.Items);
        Assert.Equal(staff.Id, item.User.Id);
        Assert.Equal(2, item.AssetCount);
        Assert.Equal(1, result.TotalPages);
        Assert.NotEqual(admin.Id, item.User.Id);
    }
}
