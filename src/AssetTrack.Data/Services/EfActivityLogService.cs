using AssetTrack.Core.Abstractions;
using AssetTrack.Core.Entities;
using AssetTrack.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace AssetTrack.Data.Services;

public class EfActivityLogService(IDbContextFactory<AssetTrackDbContext> dbFactory) : IActivityLogService
{
    public async Task<PagedResult<ActivityLog>> SearchAsync(int page, int pageSize, ActivityEventType? eventTypeFilter = null,
        string? sortBy = null, bool sortAscending = false)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        IQueryable<ActivityLog> query = db.ActivityLogs
            .Include(a => a.ActorUser)
            .Include(a => a.Asset)
            .Include(a => a.TargetUser)
            .AsNoTracking();

        if (eventTypeFilter.HasValue)
            query = query.Where(a => a.EventType == eventTypeFilter.Value);

        query = sortBy switch
        {
            "Actor" => sortAscending ? query.OrderBy(a => a.ActorUser!.DisplayName) : query.OrderByDescending(a => a.ActorUser!.DisplayName),
            "EventType" => sortAscending ? query.OrderBy(a => a.EventType) : query.OrderByDescending(a => a.EventType),
            "Asset" => sortAscending ? query.OrderBy(a => a.Asset!.AssetTag) : query.OrderByDescending(a => a.Asset!.AssetTag),
            "Description" => sortAscending ? query.OrderBy(a => a.Description) : query.OrderByDescending(a => a.Description),
            _ => sortAscending ? query.OrderBy(a => a.TimestampUtc) : query.OrderByDescending(a => a.TimestampUtc)
        };

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<ActivityLog> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }
}
