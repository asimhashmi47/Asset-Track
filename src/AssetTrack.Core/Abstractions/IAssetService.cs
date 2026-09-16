using AssetTrack.Core.Entities;
using AssetTrack.Core.Enums;

namespace AssetTrack.Core.Abstractions;

public interface IAssetService
{
    Task<PagedResult<Asset>> SearchAsync(string? searchText, AssetStatus? statusFilter, string? categoryFilter, int page, int pageSize,
        string? sortBy = null, bool sortAscending = true);

    Task<Asset?> GetByIdAsync(int assetId);

    Task<ServiceResult<Asset>> CreateAsync(string name, string category, string serialNumber, string company, DateTime addedDate,
        int actingUserId, string? make = null, string? model = null, int? year = null);

    Task<ServiceResult> UpdateAsync(int assetId, string name, string category, string serialNumber, string company,
        int actingUserId, string? make = null, string? model = null, int? year = null);

    Task<ServiceResult> AllocateAsync(int assetId, int targetUserId, DateTime allocationDate, string? notes, int actingUserId);

    Task<ServiceResult> ReturnAsync(int assetId, DateTime returnDate, ReturnCondition condition, string? notes, int actingUserId);

    Task<ServiceResult> MarkScrapAsync(int assetId, string? notes, int actingUserId);

    Task<IReadOnlyList<ActivityLog>> GetHistoryAsync(int assetId);

    Task<DashboardSummary> GetDashboardSummaryAsync();

    Task<IReadOnlyList<ActivityLog>> GetRecentActivityAsync(int count);

    Task<IReadOnlyList<Asset>> GetAvailableAssetsAsync();

    Task<IReadOnlyList<Asset>> GetAssignedAssetsAsync();
}
