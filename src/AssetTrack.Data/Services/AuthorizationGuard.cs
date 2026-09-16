using AssetTrack.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace AssetTrack.Data.Services;

/// <summary>
/// Data-layer authorization check (defense in depth). The UI already gates admin-only
/// actions, but mutating services must not trust a caller's claimed role — they load and
/// verify the acting user themselves.
/// </summary>
internal static class AuthorizationGuard
{
    public static async Task<bool> IsAdminAsync(AssetTrackDbContext db, int actingUserId) =>
        await db.Users.AnyAsync(u => u.Id == actingUserId && u.Role == UserRole.Admin && u.IsActive);
}
