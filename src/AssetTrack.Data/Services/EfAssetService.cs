using AssetTrack.Core.Abstractions;
using AssetTrack.Core.Entities;
using AssetTrack.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace AssetTrack.Data.Services;

public class EfAssetService(IDbContextFactory<AssetTrackDbContext> dbFactory) : IAssetService
{
    public async Task<PagedResult<Asset>> SearchAsync(string? searchText, AssetStatus? statusFilter, string? categoryFilter, int page, int pageSize,
        string? sortBy = null, bool sortAscending = true)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        IQueryable<Asset> query = db.Assets.Include(a => a.CurrentUser).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var term = EscapeLike(searchText.Trim());
            query = query.Where(a =>
                EF.Functions.Like(a.AssetTag, $"%{term}%", "\\") ||
                EF.Functions.Like(a.Name, $"%{term}%", "\\") ||
                EF.Functions.Like(a.SerialNumber, $"%{term}%", "\\") ||
                EF.Functions.Like(a.Category, $"%{term}%", "\\") ||
                (a.CurrentUser != null && EF.Functions.Like(a.CurrentUser.DisplayName, $"%{term}%", "\\")));
        }

        if (statusFilter.HasValue)
            query = query.Where(a => a.Status == statusFilter.Value);

        if (!string.IsNullOrWhiteSpace(categoryFilter) && categoryFilter != "All")
            query = query.Where(a => a.Category == categoryFilter);

        query = ApplySort(query, sortBy, sortAscending);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<Asset> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<Asset?> GetByIdAsync(int assetId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Assets.Include(a => a.CurrentUser).AsNoTracking().FirstOrDefaultAsync(a => a.Id == assetId);
    }

    public async Task<ServiceResult<Asset>> CreateAsync(string name, string category, string serialNumber, string company, DateTime addedDate,
        int actingUserId, string? make = null, string? model = null, int? year = null)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        if (!await AuthorizationGuard.IsAdminAsync(db, actingUserId))
            return ServiceResult<Asset>.Fail("Not authorized.");

        // SearchableDropdown-backed Category resolves an emptied field to null, not "" — trim
        // defensively so clearing it can't NullReferenceException instead of failing validation.
        name = (name ?? string.Empty).Trim();
        category = (category ?? string.Empty).Trim();
        serialNumber = (serialNumber ?? string.Empty).Trim();
        company = (company ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(category) ||
            string.IsNullOrWhiteSpace(serialNumber) || string.IsNullOrWhiteSpace(company))
            return ServiceResult<Asset>.Fail("All fields are required.");

        if (await db.Assets.AnyAsync(a => a.SerialNumber == serialNumber))
            return ServiceResult<Asset>.Fail($"Serial number '{serialNumber}' is already in use.");

        var assetTag = await GenerateNextAssetTagAsync(db);

        var asset = new Asset
        {
            AssetTag = assetTag,
            Name = name,
            Category = category,
            SerialNumber = serialNumber,
            Make = string.IsNullOrWhiteSpace(make) ? null : make.Trim(),
            Model = string.IsNullOrWhiteSpace(model) ? null : model.Trim(),
            Year = year,
            Company = company,
            Status = AssetStatus.Available,
            CreatedAtUtc = addedDate
        };

        db.Assets.Add(asset);
        db.ActivityLogs.Add(new ActivityLog
        {
            TimestampUtc = addedDate,
            ActorUserId = actingUserId,
            EventType = ActivityEventType.AssetCreated,
            Asset = asset,
            Description = $"{name} added to inventory"
        });

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return ServiceResult<Asset>.Fail($"Serial number '{serialNumber}' is already in use.");
        }

        return ServiceResult<Asset>.Ok(asset);
    }

    public async Task<ServiceResult> UpdateAsync(int assetId, string name, string category, string serialNumber, string company,
        int actingUserId, string? make = null, string? model = null, int? year = null)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        if (!await AuthorizationGuard.IsAdminAsync(db, actingUserId))
            return ServiceResult.Fail("Not authorized.");

        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == assetId);
        if (asset is null)
            return ServiceResult.Fail("Asset not found.");

        name = (name ?? string.Empty).Trim();
        category = (category ?? string.Empty).Trim();
        serialNumber = (serialNumber ?? string.Empty).Trim();
        company = (company ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(category) ||
            string.IsNullOrWhiteSpace(serialNumber) || string.IsNullOrWhiteSpace(company))
            return ServiceResult.Fail("All fields are required.");

        if (await db.Assets.AnyAsync(a => a.Id != assetId && a.SerialNumber == serialNumber))
            return ServiceResult.Fail($"Serial number '{serialNumber}' is already in use.");

        asset.Name = name;
        asset.Category = category;
        asset.SerialNumber = serialNumber;
        asset.Company = company;
        asset.Make = string.IsNullOrWhiteSpace(make) ? null : make.Trim();
        asset.Model = string.IsNullOrWhiteSpace(model) ? null : model.Trim();
        asset.Year = year;

        db.ActivityLogs.Add(new ActivityLog
        {
            TimestampUtc = DateTime.UtcNow,
            ActorUserId = actingUserId,
            EventType = ActivityEventType.AssetUpdated,
            AssetId = asset.Id,
            Description = $"{name} details updated"
        });

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return ServiceResult.Fail($"Serial number '{serialNumber}' is already in use.");
        }

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> AllocateAsync(int assetId, int targetUserId, DateTime allocationDate, string? notes, int actingUserId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        if (!await AuthorizationGuard.IsAdminAsync(db, actingUserId))
            return ServiceResult.Fail("Not authorized.");

        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == assetId);
        if (asset is null)
            return ServiceResult.Fail("Asset not found.");
        if (asset.Status != AssetStatus.Available)
            return ServiceResult.Fail("Only available assets can be allocated.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == targetUserId && u.IsActive);
        if (user is null)
            return ServiceResult.Fail("User not found or inactive.");

        asset.Status = AssetStatus.Assigned;
        asset.CurrentUserId = user.Id;
        asset.Company = user.Company;
        asset.Division = user.Division;
        asset.City = user.City;

        db.ActivityLogs.Add(new ActivityLog
        {
            TimestampUtc = allocationDate,
            ActorUserId = actingUserId,
            EventType = ActivityEventType.Allocated,
            AssetId = asset.Id,
            TargetUserId = user.Id,
            Notes = notes,
            Description = $"{asset.Name} → {user.DisplayName}"
        });

        await db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> ReturnAsync(int assetId, DateTime returnDate, ReturnCondition condition, string? notes, int actingUserId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        if (!await AuthorizationGuard.IsAdminAsync(db, actingUserId))
            return ServiceResult.Fail("Not authorized.");

        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == assetId);
        if (asset is null)
            return ServiceResult.Fail("Asset not found.");
        if (asset.Status != AssetStatus.Assigned || asset.CurrentUserId is null)
            return ServiceResult.Fail("Only assigned assets can be returned.");

        var previousUserId = asset.CurrentUserId.Value;
        var previousUser = await db.Users.FirstOrDefaultAsync(u => u.Id == previousUserId);

        asset.Status = condition switch
        {
            ReturnCondition.Good => AssetStatus.Available,
            ReturnCondition.Damaged => AssetStatus.Repair,
            ReturnCondition.NeedsRepair => AssetStatus.Repair,
            ReturnCondition.Scrap => AssetStatus.Scrap,
            _ => AssetStatus.Available
        };
        asset.CurrentUserId = null;
        // The asset no longer belongs to any location until it's next allocated.
        asset.Division = null;
        asset.City = null;

        db.ActivityLogs.Add(new ActivityLog
        {
            TimestampUtc = returnDate,
            ActorUserId = actingUserId,
            EventType = ActivityEventType.Returned,
            AssetId = asset.Id,
            TargetUserId = previousUserId,
            Condition = condition,
            Notes = notes,
            Description = $"{asset.Name} returned by {previousUser?.DisplayName ?? "former holder"} ({condition})"
        });

        await db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> MarkScrapAsync(int assetId, string? notes, int actingUserId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        if (!await AuthorizationGuard.IsAdminAsync(db, actingUserId))
            return ServiceResult.Fail("Not authorized.");

        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == assetId);
        if (asset is null)
            return ServiceResult.Fail("Asset not found.");
        if (asset.Status == AssetStatus.Scrap)
            return ServiceResult.Fail("Asset is already scrapped.");

        var previousUserId = asset.CurrentUserId;
        asset.Status = AssetStatus.Scrap;
        asset.CurrentUserId = null;
        asset.Division = null;
        asset.City = null;

        db.ActivityLogs.Add(new ActivityLog
        {
            TimestampUtc = DateTime.UtcNow,
            ActorUserId = actingUserId,
            EventType = ActivityEventType.MarkedScrap,
            AssetId = asset.Id,
            TargetUserId = previousUserId,
            Condition = ReturnCondition.Scrap,
            Notes = notes,
            Description = $"{asset.Name} marked as scrap"
        });

        await db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<IReadOnlyList<ActivityLog>> GetHistoryAsync(int assetId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.ActivityLogs
            .Include(a => a.ActorUser)
            .Include(a => a.TargetUser)
            .Where(a => a.AssetId == assetId)
            .OrderByDescending(a => a.TimestampUtc)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<DashboardSummary> GetDashboardSummaryAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var counts = await db.Assets
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var byCategory = await db.Assets
            .GroupBy(a => a.Category)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .ToListAsync();

        var totalUsers = await db.Users.CountAsync(u => u.IsActive);
        var returnsRecorded = await db.ActivityLogs.CountAsync(a => a.EventType == ActivityEventType.Returned);

        int Count(AssetStatus s) => counts.FirstOrDefault(c => c.Status == s)?.Count ?? 0;

        return new DashboardSummary
        {
            TotalAssets = counts.Sum(c => c.Count),
            Assigned = Count(AssetStatus.Assigned),
            Available = Count(AssetStatus.Available),
            Repair = Count(AssetStatus.Repair),
            Scrap = Count(AssetStatus.Scrap),
            TotalUsers = totalUsers,
            ReturnsRecorded = returnsRecorded,
            ByCategory = byCategory.Select(c => (c.Category, c.Count)).ToList()
        };
    }

    public async Task<IReadOnlyList<ActivityLog>> GetRecentActivityAsync(int count)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.ActivityLogs
            .Include(a => a.Asset)
            .Include(a => a.TargetUser)
            .Include(a => a.ActorUser)
            .OrderByDescending(a => a.TimestampUtc)
            .Take(count)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Asset>> GetAvailableAssetsAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Assets.Where(a => a.Status == AssetStatus.Available).OrderBy(a => a.Name).AsNoTracking().ToListAsync();
    }

    public async Task<IReadOnlyList<Asset>> GetAssignedAssetsAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Assets.Include(a => a.CurrentUser).Where(a => a.Status == AssetStatus.Assigned).OrderBy(a => a.Name).AsNoTracking().ToListAsync();
    }

    private static IQueryable<Asset> ApplySort(IQueryable<Asset> query, string? sortBy, bool ascending)
    {
        IOrderedQueryable<Asset> ordered = sortBy switch
        {
            "Name" => ascending ? query.OrderBy(a => a.Name) : query.OrderByDescending(a => a.Name),
            "Category" => ascending ? query.OrderBy(a => a.Category) : query.OrderByDescending(a => a.Category),
            "SerialNumber" => ascending ? query.OrderBy(a => a.SerialNumber) : query.OrderByDescending(a => a.SerialNumber),
            "Status" => ascending ? query.OrderBy(a => a.Status) : query.OrderByDescending(a => a.Status),
            "CurrentUser" => ascending ? query.OrderBy(a => a.CurrentUser!.DisplayName) : query.OrderByDescending(a => a.CurrentUser!.DisplayName),
            _ => ascending ? query.OrderBy(a => a.AssetTag) : query.OrderByDescending(a => a.AssetTag)
        };
        return ordered;
    }

    private static async Task<string> GenerateNextAssetTagAsync(AssetTrackDbContext db)
    {
        var tags = await db.Assets.Select(a => a.AssetTag).ToListAsync();

        var maxNumber = tags
            .Select(t => t.StartsWith("AT-") && int.TryParse(t.AsSpan(3), out var n) ? n : (int?)null)
            .Where(n => n.HasValue)
            .Select(n => n!.Value)
            .DefaultIfEmpty(1000)
            .Max();

        return $"AT-{maxNumber + 1}";
    }

    private static string EscapeLike(string term) =>
        term.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
