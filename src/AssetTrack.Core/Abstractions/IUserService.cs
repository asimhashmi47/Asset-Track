using AssetTrack.Core.Entities;
using AssetTrack.Core.Enums;

namespace AssetTrack.Core.Abstractions;

public interface IUserService
{
    Task<PagedResult<UserListItem>> SearchAsync(string? searchText, int page, int pageSize,
        UserRole? roleFilter = null, bool? activeFilter = null, string? sortBy = null, bool sortAscending = true);

    Task<User?> GetByIdAsync(int userId);

    Task<ServiceResult<User>> CreateAsync(string username, string password, string displayName, UserRole role,
        string company, string division, string city, int actingUserId, string? designation = null, string? contact = null);

    Task<ServiceResult> UpdateAsync(int userId, string displayName, UserRole role, string company, string division, string city,
        int actingUserId, string? designation = null, string? contact = null);

    Task<IReadOnlyList<Asset>> GetCurrentlyAssignedAsync(int userId);

    Task<IReadOnlyList<ActivityLog>> GetFullHistoryAsync(int userId);

    Task<IReadOnlyList<User>> GetActiveUsersAsync();
}
