using AssetTrack.Core.Entities;
using AssetTrack.Core.Enums;
using AssetTrack.Data.Services;
using Xunit;

namespace AssetTrack.Tests;

public class AssetServiceTests
{
    private static async Task<(EfAssetService Service, User Admin, User Staff)> SetupAsync()
    {
        var factory = TestDbFactory.CreateFactory();
        await using var db = factory.CreateDbContext();
        var admin = new User { Username = "admin", PasswordHash = "x", PasswordSalt = "x", DisplayName = "Admin", Role = UserRole.Admin, Company = "Acme", Division = "IT", City = "Pune" };
        var staff = new User { Username = "staff", PasswordHash = "x", PasswordSalt = "x", DisplayName = "Staff One", Role = UserRole.Staff, Company = "Acme", Division = "Eng", City = "Pune" };
        db.Users.AddRange(admin, staff);
        await db.SaveChangesAsync();
        return (new EfAssetService(factory), admin, staff);
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicateSerialNumber()
    {
        var (service, admin, _) = await SetupAsync();

        var first = await service.CreateAsync("Laptop A", "Laptop", "SN-001", "Acme", DateTime.UtcNow, admin.Id);
        Assert.True(first.Success);

        var duplicate = await service.CreateAsync("Laptop B", "Laptop", "SN-001", "Acme", DateTime.UtcNow, admin.Id);

        Assert.False(duplicate.Success);
        Assert.Contains("already in use", duplicate.Error);
    }

    [Fact]
    public async Task AllocateAsync_CannotAllocateAnAlreadyAssignedAsset()
    {
        var (service, admin, staff) = await SetupAsync();
        var created = await service.CreateAsync("Laptop A", "Laptop", "SN-100", "Acme", DateTime.UtcNow, admin.Id);
        var assetId = created.Value!.Id;

        var firstAllocation = await service.AllocateAsync(assetId, staff.Id, DateTime.UtcNow, null, admin.Id);
        Assert.True(firstAllocation.Success);

        var secondAllocation = await service.AllocateAsync(assetId, staff.Id, DateTime.UtcNow, null, admin.Id);

        Assert.False(secondAllocation.Success);
        Assert.Contains("available", secondAllocation.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(ReturnCondition.Good, AssetStatus.Available)]
    [InlineData(ReturnCondition.Damaged, AssetStatus.Repair)]
    [InlineData(ReturnCondition.NeedsRepair, AssetStatus.Repair)]
    [InlineData(ReturnCondition.Scrap, AssetStatus.Scrap)]
    public async Task ReturnAsync_SetsNextStatusFromCondition(ReturnCondition condition, AssetStatus expectedStatus)
    {
        var (service, admin, staff) = await SetupAsync();
        var created = await service.CreateAsync("Monitor", "Monitor", $"SN-{condition}", "Acme", DateTime.UtcNow, admin.Id);
        var assetId = created.Value!.Id;
        await service.AllocateAsync(assetId, staff.Id, DateTime.UtcNow, null, admin.Id);

        var result = await service.ReturnAsync(assetId, DateTime.UtcNow, condition, null, admin.Id);
        Assert.True(result.Success);

        var asset = await service.GetByIdAsync(assetId);
        Assert.Equal(expectedStatus, asset!.Status);
        Assert.Null(asset.CurrentUserId);
    }

    [Fact]
    public async Task ReturnAsync_RejectsReturningAnAssetThatIsNotAssigned()
    {
        var (service, admin, _) = await SetupAsync();
        var created = await service.CreateAsync("Keyboard", "Keyboard", "SN-200", "Acme", DateTime.UtcNow, admin.Id);

        var result = await service.ReturnAsync(created.Value!.Id, DateTime.UtcNow, ReturnCondition.Good, null, admin.Id);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task MarkScrapAsync_PreservesPreviousOwnerInHistoryAndClearsCurrentUser()
    {
        var (service, admin, staff) = await SetupAsync();
        var created = await service.CreateAsync("Dock", "Docking Station", "SN-300", "Acme", DateTime.UtcNow, admin.Id);
        var assetId = created.Value!.Id;
        await service.AllocateAsync(assetId, staff.Id, DateTime.UtcNow, null, admin.Id);

        var result = await service.MarkScrapAsync(assetId, "Damaged beyond repair", admin.Id);
        Assert.True(result.Success);

        var asset = await service.GetByIdAsync(assetId);
        Assert.Equal(AssetStatus.Scrap, asset!.Status);
        Assert.Null(asset.CurrentUserId);

        var history = await service.GetHistoryAsync(assetId);
        Assert.Contains(history, h => h.EventType == ActivityEventType.MarkedScrap && h.TargetUserId == staff.Id);
        Assert.Contains(history, h => h.EventType == ActivityEventType.Allocated && h.TargetUserId == staff.Id);
    }

    [Fact]
    public async Task MarkScrapAsync_RejectsAssetAlreadyScrapped()
    {
        var (service, admin, _) = await SetupAsync();
        var created = await service.CreateAsync("Cable", "Cable", "SN-400", "Acme", DateTime.UtcNow, admin.Id);
        await service.MarkScrapAsync(created.Value!.Id, null, admin.Id);

        var result = await service.MarkScrapAsync(created.Value!.Id, null, admin.Id);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task UpdateAsync_ChangesFieldsAndKeepsAssetTagAndStatus()
    {
        var (service, admin, staff) = await SetupAsync();
        var created = await service.CreateAsync("Laptop A", "Laptop", "SN-500", "Acme", DateTime.UtcNow, admin.Id);
        var assetId = created.Value!.Id;
        await service.AllocateAsync(assetId, staff.Id, DateTime.UtcNow, null, admin.Id);
        var originalTag = created.Value!.AssetTag;

        var result = await service.UpdateAsync(assetId, "Laptop A (renamed)", "Ultrabook", "SN-500-B", "Globex",
            admin.Id, "Dell", "XPS 13", 2024);

        Assert.True(result.Success);

        var asset = await service.GetByIdAsync(assetId);
        Assert.Equal("Laptop A (renamed)", asset!.Name);
        Assert.Equal("Ultrabook", asset.Category);
        Assert.Equal("SN-500-B", asset.SerialNumber);
        Assert.Equal("Globex", asset.Company);
        Assert.Equal("Dell", asset.Make);
        Assert.Equal("XPS 13", asset.Model);
        Assert.Equal(2024, asset.Year);
        Assert.Equal(originalTag, asset.AssetTag);
        Assert.Equal(AssetStatus.Assigned, asset.Status);
        Assert.Equal(staff.Id, asset.CurrentUserId);
    }

    [Fact]
    public async Task UpdateAsync_RejectsSerialNumberAlreadyUsedByAnotherAsset()
    {
        var (service, admin, _) = await SetupAsync();
        var first = await service.CreateAsync("Laptop A", "Laptop", "SN-600", "Acme", DateTime.UtcNow, admin.Id);
        var second = await service.CreateAsync("Laptop B", "Laptop", "SN-601", "Acme", DateTime.UtcNow, admin.Id);

        var result = await service.UpdateAsync(second.Value!.Id, "Laptop B", "Laptop", "SN-600", "Acme", admin.Id);

        Assert.False(result.Success);
        Assert.Contains("already in use", result.Error);
    }

    [Fact]
    public async Task UpdateAsync_AllowsKeepingTheAssetsOwnSerialNumber()
    {
        var (service, admin, _) = await SetupAsync();
        var created = await service.CreateAsync("Laptop A", "Laptop", "SN-700", "Acme", DateTime.UtcNow, admin.Id);

        var result = await service.UpdateAsync(created.Value!.Id, "Laptop A renamed", "Laptop", "SN-700", "Acme", admin.Id);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task UpdateAsync_RejectsNullCategoryInsteadOfThrowing()
    {
        var (service, admin, _) = await SetupAsync();
        var created = await service.CreateAsync("Laptop A", "Laptop", "SN-900", "Acme", DateTime.UtcNow, admin.Id);

        var result = await service.UpdateAsync(created.Value!.Id, "Laptop A", null!, "SN-900", "Acme", admin.Id);

        Assert.False(result.Success);
        Assert.Equal("All fields are required.", result.Error);
    }

    [Fact]
    public async Task UpdateAsync_RecordsAssetUpdatedActivityLog()
    {
        var (service, admin, _) = await SetupAsync();
        var created = await service.CreateAsync("Laptop A", "Laptop", "SN-800", "Acme", DateTime.UtcNow, admin.Id);

        await service.UpdateAsync(created.Value!.Id, "Laptop A v2", "Laptop", "SN-800", "Acme", admin.Id);

        var history = await service.GetHistoryAsync(created.Value!.Id);
        Assert.Contains(history, h => h.EventType == ActivityEventType.AssetUpdated);
    }

    [Fact]
    public async Task CreateAsync_GeneratesSequentialUniqueAssetTags()
    {
        var (service, admin, _) = await SetupAsync();

        var first = await service.CreateAsync("Item A", "Cable", "SN-A", "Acme", DateTime.UtcNow, admin.Id);
        var second = await service.CreateAsync("Item B", "Cable", "SN-B", "Acme", DateTime.UtcNow, admin.Id);

        Assert.NotEqual(first.Value!.AssetTag, second.Value!.AssetTag);
    }
}
