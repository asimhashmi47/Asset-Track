using AssetTrack.Core.Entities;
using AssetTrack.Core.Enums;

namespace AssetTrack.Core.Abstractions;

public interface IActivityLogService
{
    Task<PagedResult<ActivityLog>> SearchAsync(int page, int pageSize, ActivityEventType? eventTypeFilter = null,
        string? sortBy = null, bool sortAscending = false);
}
