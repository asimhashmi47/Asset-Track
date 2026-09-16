using AssetTrack.Core.Abstractions;
using AssetTrack.Core.Entities;
using AssetTrack.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace AssetTrack.Data.Services;

public class EfLookupService(IDbContextFactory<AssetTrackDbContext> dbFactory) : ILookupService
{
    public async Task<IReadOnlyList<string>> GetValuesAsync(LookupKind kind)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.LookupItems
            .Where(l => l.Kind == kind)
            .OrderBy(l => l.Name)
            .Select(l => l.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<ServiceResult<string>> AddValueAsync(LookupKind kind, string value)
    {
        value = value.Trim();
        if (string.IsNullOrWhiteSpace(value))
            return ServiceResult<string>.Fail("Value cannot be empty.");
        if (value.Length > 100)
            return ServiceResult<string>.Fail("Value is too long.");

        await using var db = await dbFactory.CreateDbContextAsync();

        var existing = await db.LookupItems
            .FirstOrDefaultAsync(l => l.Kind == kind && l.Name.ToLower() == value.ToLower());
        if (existing is not null)
            return ServiceResult<string>.Ok(existing.Name);

        db.LookupItems.Add(new LookupItem { Kind = kind, Name = value });

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return ServiceResult<string>.Fail($"'{value}' already exists.");
        }

        return ServiceResult<string>.Ok(value);
    }
}
