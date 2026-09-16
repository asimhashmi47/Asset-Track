using AssetTrack.Core.Abstractions;
using AssetTrack.Core.Entities;
using AssetTrack.Core.Enums;
using AssetTrack.Core.Security;
using Microsoft.EntityFrameworkCore;

namespace AssetTrack.Data.Services;

public class EfUserService(IDbContextFactory<AssetTrackDbContext> dbFactory) : IUserService
{
    public async Task<PagedResult<UserListItem>> SearchAsync(string? searchText, int page, int pageSize,
        UserRole? roleFilter = null, bool? activeFilter = null, string? sortBy = null, bool sortAscending = true)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        IQueryable<User> query = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var term = searchText.Trim();
            query = query.Where(u =>
                EF.Functions.Like(u.DisplayName, $"%{term}%") ||
                EF.Functions.Like(u.Username, $"%{term}%") ||
                EF.Functions.Like(u.Company, $"%{term}%") ||
                EF.Functions.Like(u.Division, $"%{term}%") ||
                EF.Functions.Like(u.City, $"%{term}%"));
        }

        if (roleFilter.HasValue)
            query = query.Where(u => u.Role == roleFilter.Value);

        if (activeFilter.HasValue)
            query = query.Where(u => u.IsActive == activeFilter.Value);

        query = sortBy switch
        {
            "Username" => sortAscending ? query.OrderBy(u => u.Username) : query.OrderByDescending(u => u.Username),
            "Company" => sortAscending ? query.OrderBy(u => u.Company) : query.OrderByDescending(u => u.Company),
            "Division" => sortAscending ? query.OrderBy(u => u.Division) : query.OrderByDescending(u => u.Division),
            "City" => sortAscending ? query.OrderBy(u => u.City) : query.OrderByDescending(u => u.City),
            _ => sortAscending ? query.OrderBy(u => u.DisplayName) : query.OrderByDescending(u => u.DisplayName)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserListItem(u, db.Assets.Count(a => a.CurrentUserId == u.Id)))
            .ToListAsync();

        return new PagedResult<UserListItem> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<User?> GetByIdAsync(int userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<ServiceResult<User>> CreateAsync(string username, string password, string displayName, UserRole role,
        string company, string division, string city, int actingUserId, string? designation = null, string? contact = null)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        if (!await AuthorizationGuard.IsAdminAsync(db, actingUserId))
            return ServiceResult<User>.Fail("Not authorized.");

        username = username.Trim().ToLowerInvariant();
        displayName = displayName.Trim();
        // SearchableDropdown-backed fields (Company/Division) resolve an emptied field to null,
        // not "" — trim defensively so clearing one can't NullReferenceException instead of
        // failing the validation check below.
        company = (company ?? string.Empty).Trim();
        division = (division ?? string.Empty).Trim();
        city = (city ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(displayName))
            return ServiceResult<User>.Fail("Username, password and display name are required.");

        if (string.IsNullOrWhiteSpace(company))
            return ServiceResult<User>.Fail("Company is required.");

        if (!Enum.IsDefined(role))
            return ServiceResult<User>.Fail("Invalid role.");

        if (password.Length < 6)
            return ServiceResult<User>.Fail("Password must be at least 6 characters.");

        if (await db.Users.AnyAsync(u => u.Username == username))
            return ServiceResult<User>.Fail($"Username '{username}' is already taken.");

        var (hash, salt) = PasswordHasher.Hash(password);

        var user = new User
        {
            Username = username,
            PasswordHash = hash,
            PasswordSalt = salt,
            DisplayName = displayName,
            Role = role,
            Company = company,
            Division = division,
            City = city,
            Designation = string.IsNullOrWhiteSpace(designation) ? null : designation.Trim(),
            Contact = string.IsNullOrWhiteSpace(contact) ? null : contact.Trim()
        };

        db.Users.Add(user);
        db.ActivityLogs.Add(new ActivityLog
        {
            TimestampUtc = DateTime.UtcNow,
            ActorUserId = actingUserId,
            EventType = ActivityEventType.UserCreated,
            TargetUser = user,
            Description = $"User '{displayName}' created"
        });

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return ServiceResult<User>.Fail($"Username '{username}' is already taken.");
        }

        return ServiceResult<User>.Ok(user);
    }

    public async Task<ServiceResult> UpdateAsync(int userId, string displayName, UserRole role, string company, string division, string city,
        int actingUserId, string? designation = null, string? contact = null)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        if (!await AuthorizationGuard.IsAdminAsync(db, actingUserId))
            return ServiceResult.Fail("Not authorized.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return ServiceResult.Fail("User not found.");

        displayName = displayName.Trim();
        company = (company ?? string.Empty).Trim();
        division = (division ?? string.Empty).Trim();
        city = (city ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(displayName))
            return ServiceResult.Fail("Display name is required.");

        if (string.IsNullOrWhiteSpace(company))
            return ServiceResult.Fail("Company is required.");

        if (!Enum.IsDefined(role))
            return ServiceResult.Fail("Invalid role.");

        // The app has no recovery path once no active Admin remains: allocate/return/scrap,
        // and even undoing this very change, all require an admin actor. Block the edit that
        // would leave zero, rather than fail every subsequent action afterwards.
        if (user.Role == UserRole.Admin && role != UserRole.Admin)
        {
            var otherActiveAdmins = await db.Users.CountAsync(u => u.Id != userId && u.Role == UserRole.Admin && u.IsActive);
            if (otherActiveAdmins == 0)
                return ServiceResult.Fail("Can't remove the last administrator.");
        }

        user.DisplayName = displayName;
        user.Role = role;
        user.Company = company;
        user.Division = division;
        user.City = city;
        user.Designation = string.IsNullOrWhiteSpace(designation) ? null : designation.Trim();
        user.Contact = string.IsNullOrWhiteSpace(contact) ? null : contact.Trim();

        db.ActivityLogs.Add(new ActivityLog
        {
            TimestampUtc = DateTime.UtcNow,
            ActorUserId = actingUserId,
            EventType = ActivityEventType.UserUpdated,
            TargetUserId = user.Id,
            Description = $"User '{displayName}' details updated"
        });

        await db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<IReadOnlyList<Asset>> GetCurrentlyAssignedAsync(int userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Assets.Where(a => a.CurrentUserId == userId).OrderBy(a => a.Name).AsNoTracking().ToListAsync();
    }

    public async Task<IReadOnlyList<ActivityLog>> GetFullHistoryAsync(int userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.ActivityLogs
            .Include(a => a.Asset)
            .Where(a => a.TargetUserId == userId)
            .OrderByDescending(a => a.TimestampUtc)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IReadOnlyList<User>> GetActiveUsersAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Users.Where(u => u.IsActive).OrderBy(u => u.DisplayName).AsNoTracking().ToListAsync();
    }
}
