using AssetTrack.Core.Entities;
using AssetTrack.Core.Enums;
using AssetTrack.Data.Services;
using Xunit;

namespace AssetTrack.Tests;

/// <summary>
/// Verifies the data-layer authorization check exists independently of the UI: a Staff
/// actingUserId must be rejected by every mutating service method, not just hidden from
/// the UI. This is the control the review gates flagged as missing.
/// </summary>
public class AuthorizationTests
{
    private static async Task<(TestDbContextFactory Factory, EfAssetService Assets, EfUserService Users, User Admin, User Staff)> SetupAsync()
    {
        var factory = TestDbFactory.CreateFactory();
        await using var db = factory.CreateDbContext();
        var admin = new User { Username = "admin", PasswordHash = "x", PasswordSalt = "x", DisplayName = "Admin", Role = UserRole.Admin, Company = "Acme", Division = "IT", City = "Pune" };
        var staff = new User { Username = "staff", PasswordHash = "x", PasswordSalt = "x", DisplayName = "Staff One", Role = UserRole.Staff, Company = "Acme", Division = "Eng", City = "Pune" };
        db.Users.AddRange(admin, staff);
        await db.SaveChangesAsync();
        return (factory, new EfAssetService(factory), new EfUserService(factory), admin, staff);
    }

    [Fact]
    public async Task CreateAssetAsync_RejectsNonAdminActor()
    {
        var (_, assets, _, _, staff) = await SetupAsync();

        var result = await assets.CreateAsync("Laptop", "Laptop", "SN-AUTH-1", "Acme", DateTime.UtcNow, staff.Id);

        Assert.False(result.Success);
        Assert.Equal("Not authorized.", result.Error);
    }

    [Fact]
    public async Task AllocateAsync_RejectsNonAdminActor()
    {
        var (_, assets, _, admin, staff) = await SetupAsync();
        var created = await assets.CreateAsync("Laptop", "Laptop", "SN-AUTH-2", "Acme", DateTime.UtcNow, admin.Id);

        var result = await assets.AllocateAsync(created.Value!.Id, staff.Id, DateTime.UtcNow, null, staff.Id);

        Assert.False(result.Success);
        Assert.Equal("Not authorized.", result.Error);
    }

    [Fact]
    public async Task ReturnAsync_RejectsNonAdminActor()
    {
        var (_, assets, _, admin, staff) = await SetupAsync();
        var created = await assets.CreateAsync("Laptop", "Laptop", "SN-AUTH-3", "Acme", DateTime.UtcNow, admin.Id);
        await assets.AllocateAsync(created.Value!.Id, staff.Id, DateTime.UtcNow, null, admin.Id);

        var result = await assets.ReturnAsync(created.Value!.Id, DateTime.UtcNow, ReturnCondition.Good, null, staff.Id);

        Assert.False(result.Success);
        Assert.Equal("Not authorized.", result.Error);
    }

    [Fact]
    public async Task MarkScrapAsync_RejectsNonAdminActor()
    {
        var (_, assets, _, admin, staff) = await SetupAsync();
        var created = await assets.CreateAsync("Laptop", "Laptop", "SN-AUTH-4", "Acme", DateTime.UtcNow, admin.Id);

        var result = await assets.MarkScrapAsync(created.Value!.Id, null, staff.Id);

        Assert.False(result.Success);
        Assert.Equal("Not authorized.", result.Error);
    }

    [Fact]
    public async Task UpdateAssetAsync_RejectsNonAdminActor()
    {
        var (_, assets, _, admin, staff) = await SetupAsync();
        var created = await assets.CreateAsync("Laptop", "Laptop", "SN-AUTH-5", "Acme", DateTime.UtcNow, admin.Id);

        var result = await assets.UpdateAsync(created.Value!.Id, "Renamed", "Laptop", "SN-AUTH-5", "Acme", staff.Id);

        Assert.False(result.Success);
        Assert.Equal("Not authorized.", result.Error);
    }

    [Fact]
    public async Task UpdateUserAsync_RejectsNonAdminActor()
    {
        var (_, _, users, admin, staff) = await SetupAsync();

        var result = await users.UpdateAsync(staff.Id, "Renamed Staff", UserRole.Staff, "Acme", "Eng", "Pune", staff.Id);

        Assert.False(result.Success);
        Assert.Equal("Not authorized.", result.Error);
    }

    [Fact]
    public async Task CreateUserAsync_RejectsNonAdminActor()
    {
        var (_, _, users, _, staff) = await SetupAsync();

        var result = await users.CreateAsync("newperson", "password1", "New Person", UserRole.Staff, "Acme", "Eng", "Pune", staff.Id);

        Assert.False(result.Success);
        Assert.Equal("Not authorized.", result.Error);
    }

    [Fact]
    public async Task CreateUserAsync_StaffCannotEscalateSelfToAdmin()
    {
        var (_, _, users, _, staff) = await SetupAsync();

        var result = await users.CreateAsync("wannabe-admin", "password1", "Wannabe Admin", UserRole.Admin, "Acme", "Eng", "Pune", staff.Id);

        Assert.False(result.Success);
    }
}
