using AssetTrack.Core.Entities;
using AssetTrack.Core.Enums;
using AssetTrack.Core.Security;
using AssetTrack.Data.Services;
using Xunit;

namespace AssetTrack.Tests;

public class AuthServiceTests
{
    private static async Task<TestDbContextFactory> SetupAsync(string username, string password)
    {
        var factory = TestDbFactory.CreateFactory();
        await using var db = factory.CreateDbContext();
        var (hash, salt) = PasswordHasher.Hash(password);
        db.Users.Add(new User
        {
            Username = username,
            PasswordHash = hash,
            PasswordSalt = salt,
            DisplayName = "Test User",
            Role = UserRole.Staff,
            Company = "Acme",
            Division = "Eng",
            City = "Pune"
        });
        await db.SaveChangesAsync();
        return factory;
    }

    [Fact]
    public async Task SignInAsync_SucceedsWithCorrectCredentials()
    {
        var factory = await SetupAsync("auth_ok_user", "correct-password");
        var service = new EfAuthService(factory);

        var result = await service.SignInAsync("auth_ok_user", "correct-password");

        Assert.True(result.Success);
        Assert.Equal("auth_ok_user", result.Value!.Username);
    }

    [Fact]
    public async Task SignInAsync_FailsWithWrongPassword_WithoutRevealingWhichPartWasWrong()
    {
        var factory = await SetupAsync("auth_bad_user", "correct-password");
        var service = new EfAuthService(factory);

        var wrongPassword = await service.SignInAsync("auth_bad_user", "wrong-password");
        var unknownUser = await service.SignInAsync("no-such-user", "whatever");

        Assert.False(wrongPassword.Success);
        Assert.False(unknownUser.Success);
        Assert.Equal(wrongPassword.Error, unknownUser.Error);
    }

    [Fact]
    public async Task SignInAsync_LocksOutAfterRepeatedFailures()
    {
        var factory = await SetupAsync("auth_lockout_user", "correct-password");
        var service = new EfAuthService(factory);

        for (var i = 0; i < 5; i++)
            await service.SignInAsync("auth_lockout_user", "wrong-password");

        var result = await service.SignInAsync("auth_lockout_user", "correct-password");

        Assert.False(result.Success);
        Assert.Contains("Too many failed attempts", result.Error);
    }
}
