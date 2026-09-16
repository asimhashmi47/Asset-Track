using AssetTrack.Core.Entities;
using AssetTrack.Core.Enums;
using AssetTrack.Data.Services;
using Xunit;

namespace AssetTrack.Tests;

public class UserServiceTests
{
    private static async Task<(EfUserService Service, User Admin)> SetupAsync()
    {
        var factory = TestDbFactory.CreateFactory();
        await using var db = factory.CreateDbContext();
        var admin = new User { Username = "admin", PasswordHash = "x", PasswordSalt = "x", DisplayName = "Admin", Role = UserRole.Admin, Company = "Acme", Division = "IT", City = "Pune" };
        db.Users.Add(admin);
        await db.SaveChangesAsync();
        return (new EfUserService(factory), admin);
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicateUsername()
    {
        var (service, admin) = await SetupAsync();

        var first = await service.CreateAsync("jdoe", "password1", "Jane Doe", UserRole.Staff, "Acme", "Eng", "Pune", admin.Id);
        Assert.True(first.Success);

        var duplicate = await service.CreateAsync("jdoe", "password2", "John Doe", UserRole.Staff, "Acme", "Eng", "Pune", admin.Id);

        Assert.False(duplicate.Success);
        Assert.Contains("already taken", duplicate.Error);
    }

    [Fact]
    public async Task CreateAsync_RejectsShortPassword()
    {
        var (service, admin) = await SetupAsync();

        var result = await service.CreateAsync("shortpw", "abc", "Short Pw", UserRole.Staff, "Acme", "Eng", "Pune", admin.Id);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task CreateAsync_NeverStoresPlaintextPassword()
    {
        var (service, admin) = await SetupAsync();

        var result = await service.CreateAsync("secure1", "correcthorsebattery", "Secure User", UserRole.Staff, "Acme", "Eng", "Pune", admin.Id);

        Assert.True(result.Success);
        Assert.DoesNotContain("correcthorsebattery", result.Value!.PasswordHash);
    }

    [Fact]
    public async Task UpdateAsync_ChangesEditableFieldsAndKeepsUsernameAndPassword()
    {
        var (service, admin) = await SetupAsync();
        var created = await service.CreateAsync("jsmith", "password1", "Jane Smith", UserRole.Staff, "Acme", "Eng", "Pune", admin.Id,
            "Engineer", "+1 555 0100");
        var userId = created.Value!.Id;
        var originalUsername = created.Value!.Username;
        var originalHash = created.Value!.PasswordHash;

        var result = await service.UpdateAsync(userId, "Jane S. Smith", UserRole.Admin, "Globex", "Ops", "Berlin",
            admin.Id, "Senior Engineer", "+1 555 0199");

        Assert.True(result.Success);

        var user = await service.GetByIdAsync(userId);
        Assert.Equal("Jane S. Smith", user!.DisplayName);
        Assert.Equal(UserRole.Admin, user.Role);
        Assert.Equal("Globex", user.Company);
        Assert.Equal("Ops", user.Division);
        Assert.Equal("Berlin", user.City);
        Assert.Equal("Senior Engineer", user.Designation);
        Assert.Equal("+1 555 0199", user.Contact);
        Assert.Equal(originalUsername, user.Username);
        Assert.Equal(originalHash, user.PasswordHash);
    }

    [Fact]
    public async Task UpdateAsync_RejectsBlankDisplayName()
    {
        var (service, admin) = await SetupAsync();
        var created = await service.CreateAsync("blankname", "password1", "Blank Name", UserRole.Staff, "Acme", "Eng", "Pune", admin.Id);

        var result = await service.UpdateAsync(created.Value!.Id, "   ", UserRole.Staff, "Acme", "Eng", "Pune", admin.Id);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task UpdateAsync_RecordsUserUpdatedActivityLog()
    {
        var (service, admin) = await SetupAsync();
        var created = await service.CreateAsync("logtest", "password1", "Log Test", UserRole.Staff, "Acme", "Eng", "Pune", admin.Id);

        await service.UpdateAsync(created.Value!.Id, "Log Test 2", UserRole.Staff, "Acme", "Eng", "Pune", admin.Id);

        var history = await service.GetFullHistoryAsync(created.Value!.Id);
        Assert.Contains(history, h => h.EventType == ActivityEventType.UserUpdated);
    }

    [Fact]
    public async Task UpdateAsync_RejectsNullCompanyInsteadOfThrowing()
    {
        var (service, admin) = await SetupAsync();
        var created = await service.CreateAsync("nullco", "password1", "Null Co", UserRole.Staff, "Acme", "Eng", "Pune", admin.Id);

        var result = await service.UpdateAsync(created.Value!.Id, "Null Co", UserRole.Staff, null!, "Eng", "Pune", admin.Id);

        Assert.False(result.Success);
        Assert.Equal("Company is required.", result.Error);
    }

    [Fact]
    public async Task UpdateAsync_RejectsDemotingTheLastAdmin()
    {
        var (service, admin) = await SetupAsync();

        var result = await service.UpdateAsync(admin.Id, admin.DisplayName, UserRole.Staff, admin.Company, admin.Division, admin.City, admin.Id);

        Assert.False(result.Success);
        Assert.Equal("Can't remove the last administrator.", result.Error);

        var stillAdmin = await service.GetByIdAsync(admin.Id);
        Assert.Equal(UserRole.Admin, stillAdmin!.Role);
    }

    [Fact]
    public async Task UpdateAsync_AllowsDemotingAnAdminWhenAnotherActiveAdminRemains()
    {
        var (service, admin) = await SetupAsync();
        var secondAdmin = await service.CreateAsync("admin2", "password1", "Second Admin", UserRole.Admin, "Acme", "IT", "Pune", admin.Id);

        var result = await service.UpdateAsync(secondAdmin.Value!.Id, "Second Admin", UserRole.Staff, "Acme", "IT", "Pune", admin.Id);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task UpdateAsync_RejectsUndefinedRole()
    {
        var (service, admin) = await SetupAsync();
        var created = await service.CreateAsync("badrole", "password1", "Bad Role", UserRole.Staff, "Acme", "Eng", "Pune", admin.Id);

        var result = await service.UpdateAsync(created.Value!.Id, "Bad Role", (UserRole)99, "Acme", "Eng", "Pune", admin.Id);

        Assert.False(result.Success);
    }
}
