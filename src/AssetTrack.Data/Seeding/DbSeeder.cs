using AssetTrack.Core.Entities;
using AssetTrack.Core.Enums;
using AssetTrack.Core.Security;
using Microsoft.EntityFrameworkCore;

namespace AssetTrack.Data.Seeding;

/// <summary>Seeds the 6 demo accounts and a small realistic asset set on first run only.</summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AssetTrackDbContext db)
    {
        if (await db.Users.AnyAsync())
            return; // already seeded

        var admin = MakeUser("admin", "admin123", "Priya Nair", UserRole.Admin, "Northwind Labs", "IT", "Pune", "IT Manager", "+91 98220 11223");
        var arjun = MakeUser("arjun", "user123", "Arjun Mehta", UserRole.Staff, "Northwind Labs", "Engineering", "Pune", "Software Engineer", "+91 98221 44556");
        var sara = MakeUser("sara", "user123", "Sara Iqbal", UserRole.Staff, "Northwind Labs", "Design", "Bengaluru", "Product Designer", "+91 98222 77889");
        var daniel = MakeUser("daniel", "user123", "Daniel Cruz", UserRole.Staff, "Northwind Retail", "Operations", "Austin", "Operations Analyst", "+1 512 555 0142");
        var lena = MakeUser("lena", "user123", "Lena Fischer", UserRole.Staff, "Northwind Retail", "Finance", "Berlin", "Finance Analyst", "+49 30 1234 5678");
        var rohit = MakeUser("rohit", "user123", "Rohit Verma", UserRole.Staff, "Northwind Labs", "Support", "Pune", "Support Engineer", null);
        rohit.IsActive = false;

        db.Users.AddRange(admin, arjun, sara, daniel, lena, rohit);
        await db.SaveChangesAsync();

        var created = new DateTime(2026, 1, 18, 9, 15, 0, DateTimeKind.Utc);
        var allocated = new DateTime(2026, 3, 12, 10, 30, 0, DateTimeKind.Utc);

        (Asset asset, User? holder, AssetStatus status)[] assets =
        [
            (Make("AT-1001", "MacBook Pro 14\"", "Laptop", "C02FK9LMQ6", arjun, "Apple", "MacBook Pro 14\" M3", 2024), arjun, AssetStatus.Assigned),
            (Make("AT-1002", "MacBook Air 13\"", "Laptop", "C02GH3PPL1", sara, "Apple", "MacBook Air 13\" M2", 2023), sara, AssetStatus.Assigned),
            (Make("AT-1003", "ThinkPad T14 Gen 4", "Laptop", "PF3NBQ7X", daniel, "Lenovo", "ThinkPad T14 Gen 4", 2023), daniel, AssetStatus.Assigned),
            (Make("AT-1004", "ThinkPad X1 Carbon", "Laptop", "PF2MK81C", null), null, AssetStatus.Available),
            (Make("AT-1005", "Dell U2723QE 27\"", "Monitor", "CN0H8P2M", arjun), arjun, AssetStatus.Assigned),
            (Make("AT-1006", "LG 24MP60G 24\"", "Monitor", "KR1188DQ", null), null, AssetStatus.Repair),
            (Make("AT-1007", "Samsung S24C 24\"", "Monitor", "SM4402KL", null), null, AssetStatus.Available),
            (Make("AT-1008", "Corsair 16GB DDR5", "RAM", "CMK16G5", daniel), daniel, AssetStatus.Assigned),
            (Make("AT-1009", "Crucial 16GB DDR4", "RAM", "CT16G4D", null), null, AssetStatus.Available),
            (Make("AT-1010", "Logitech MX Master 3S", "Mouse", "LM3S0091", sara), sara, AssetStatus.Assigned),
            (Make("AT-1011", "Logitech B100", "Mouse", "LB100221", null), null, AssetStatus.Available),
            (Make("AT-1012", "Keychron K3 Pro", "Keyboard", "KK3P0044", lena), lena, AssetStatus.Assigned),
            (Make("AT-1013", "Dell KB216", "Keyboard", "DKB216X9", null), null, AssetStatus.Available),
            (Make("AT-1014", "Belkin 6-Outlet Board", "Extension Board", "BK6OB771", null), null, AssetStatus.Available),
            (Make("AT-1015", "Anker 3m Extension Wire", "Extension Wire", "AN3M0092", arjun), arjun, AssetStatus.Assigned),
            (Make("AT-1016", "Amazon Basics HDMI Cable", "Cable", "AB2HDM01", null), null, AssetStatus.Available),
            (Make("AT-1017", "CalDigit TS4 Dock", "Docking Station", "CD-TS4-01", sara), sara, AssetStatus.Assigned),
            (Make("AT-1018", "Dell WD19S Dock", "Docking Station", "DL-WD19S2", null), null, AssetStatus.Scrap),
        ];

        foreach (var (asset, holder, status) in assets)
        {
            asset.Status = status;
            if (holder is not null)
            {
                asset.CurrentUserId = holder.Id;
                asset.Company = holder.Company;
                asset.Division = holder.Division;
                asset.City = holder.City;
            }
        }

        db.Assets.AddRange(assets.Select(a => a.asset));
        await db.SaveChangesAsync();

        foreach (var (asset, holder, status) in assets)
        {
            db.ActivityLogs.Add(new ActivityLog
            {
                TimestampUtc = created,
                ActorUserId = admin.Id,
                EventType = ActivityEventType.AssetCreated,
                AssetId = asset.Id,
                Description = $"{asset.Name} added to inventory"
            });

            if (holder is not null)
            {
                db.ActivityLogs.Add(new ActivityLog
                {
                    TimestampUtc = allocated,
                    ActorUserId = admin.Id,
                    EventType = ActivityEventType.Allocated,
                    AssetId = asset.Id,
                    TargetUserId = holder.Id,
                    Description = $"{asset.Name} → {holder.DisplayName}"
                });
            }

            if (status == AssetStatus.Scrap && holder is null)
            {
                db.ActivityLogs.Add(new ActivityLog
                {
                    TimestampUtc = allocated.AddDays(30),
                    ActorUserId = admin.Id,
                    EventType = ActivityEventType.MarkedScrap,
                    AssetId = asset.Id,
                    Condition = ReturnCondition.Scrap,
                    Description = $"{asset.Name} marked as scrap"
                });
            }
        }

        // AT-1011 has a real return in its past, matching the source design mockup.
        var b100 = assets.First(a => a.asset.AssetTag == "AT-1011").asset;
        db.ActivityLogs.Add(new ActivityLog
        {
            TimestampUtc = allocated.AddDays(-14),
            ActorUserId = admin.Id,
            EventType = ActivityEventType.Allocated,
            AssetId = b100.Id,
            TargetUserId = rohit.Id,
            Description = $"{b100.Name} → {rohit.DisplayName}"
        });
        db.ActivityLogs.Add(new ActivityLog
        {
            TimestampUtc = new DateTime(2026, 2, 26, 16, 40, 0, DateTimeKind.Utc),
            ActorUserId = admin.Id,
            EventType = ActivityEventType.Returned,
            AssetId = b100.Id,
            TargetUserId = rohit.Id,
            Condition = ReturnCondition.Good,
            Description = $"{b100.Name} returned by {rohit.DisplayName} (Good)"
        });

        await db.SaveChangesAsync();
        await SeedLookupsAsync(db);
    }

    private static User MakeUser(string username, string password, string displayName, UserRole role, string company, string division, string city,
        string? designation, string? contact)
    {
        var (hash, salt) = PasswordHasher.Hash(password);
        return new User
        {
            Username = username,
            PasswordHash = hash,
            PasswordSalt = salt,
            DisplayName = displayName,
            Role = role,
            Company = company,
            Division = division,
            City = city,
            Designation = designation,
            Contact = contact
        };
    }

    private static Asset Make(string tag, string name, string category, string serial, User? holder,
        string? make = null, string? model = null, int? year = null) => new()
    {
        AssetTag = tag,
        Name = name,
        Category = category,
        SerialNumber = serial,
        Make = make,
        Model = model,
        Year = year,
        Company = holder?.Company ?? "Northwind Labs",
        Division = holder?.Division,
        City = holder?.City,
        CreatedAtUtc = new DateTime(2026, 1, 18, 9, 15, 0, DateTimeKind.Utc)
    };

    private static async Task SeedLookupsAsync(AssetTrackDbContext db)
    {
        (LookupKind Kind, string[] Values)[] lookups =
        [
            (LookupKind.Category, ["Laptop", "Monitor", "RAM", "Mouse", "Keyboard", "Extension Wire", "Extension Board", "Cable", "Docking Station", "Other"]),
            (LookupKind.Company, ["Northwind Labs", "Northwind Retail"]),
            (LookupKind.Department, ["IT", "Engineering", "Design", "Operations", "Finance", "Support"]),
            (LookupKind.Designation, ["IT Manager", "Software Engineer", "Product Designer", "Operations Analyst", "Finance Analyst", "Support Engineer"]),
        ];

        foreach (var (kind, values) in lookups)
            foreach (var value in values)
                db.LookupItems.Add(new LookupItem { Kind = kind, Name = value });

        await db.SaveChangesAsync();
    }
}
