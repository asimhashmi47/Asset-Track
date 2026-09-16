using System.Collections.Concurrent;
using AssetTrack.Core.Abstractions;
using AssetTrack.Core.Entities;
using AssetTrack.Core.Security;
using Microsoft.EntityFrameworkCore;

namespace AssetTrack.Data.Services;

public class EfAuthService(IDbContextFactory<AssetTrackDbContext> dbFactory) : IAuthService
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan LockoutWindow = TimeSpan.FromSeconds(30);

    // Fixed dummy hash so a missing/inactive username costs the same PBKDF2 work as a real
    // one, instead of returning near-instantly and leaking which usernames exist via timing.
    private static readonly (string Hash, string Salt) DummyCredential = PasswordHasher.Hash(Guid.NewGuid().ToString());

    // Process-lifetime brute-force throttle, keyed by normalized username. A small demo
    // app has no shared cache/Redis to lean on — this is the smallest thing that works.
    private static readonly ConcurrentDictionary<string, (int Attempts, DateTime LockedUntilUtc)> FailedAttempts = new();

    public async Task<ServiceResult<User>> SignInAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return ServiceResult<User>.Fail("Username and password are required.");

        var normalized = username.Trim().ToLowerInvariant();

        if (FailedAttempts.TryGetValue(normalized, out var throttle) && throttle.LockedUntilUtc > DateTime.UtcNow)
        {
            var secondsLeft = (int)Math.Ceiling((throttle.LockedUntilUtc - DateTime.UtcNow).TotalSeconds);
            return ServiceResult<User>.Fail($"Too many failed attempts. Try again in {secondsLeft}s.");
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == normalized);

        bool passwordOk;
        if (user is not null && user.IsActive)
        {
            passwordOk = PasswordHasher.Verify(password, user.PasswordHash, user.PasswordSalt);
        }
        else
        {
            // No real account to check against — verify against a dummy hash anyway so a
            // missing/inactive username costs the same PBKDF2 work as a real failed login.
            PasswordHasher.Verify(password, DummyCredential.Hash, DummyCredential.Salt);
            passwordOk = false;
        }

        // Deliberately identical failure message whether the user is missing, inactive, or
        // the password is wrong — avoids leaking which accounts exist (username enumeration).
        if (!passwordOk)
        {
            RecordFailure(normalized);
            return ServiceResult<User>.Fail("Invalid username or password.");
        }

        FailedAttempts.TryRemove(normalized, out _);
        return ServiceResult<User>.Ok(user!);
    }

    private static void RecordFailure(string normalizedUsername)
    {
        FailedAttempts.AddOrUpdate(
            normalizedUsername,
            _ => (1, DateTime.MinValue),
            (_, current) =>
            {
                // A lockout that has already expired starts a fresh count instead of
                // re-locking on the very next mistake.
                var previousAttempts = current.LockedUntilUtc != DateTime.MinValue && current.LockedUntilUtc <= DateTime.UtcNow
                    ? 0
                    : current.Attempts;

                var attempts = previousAttempts + 1;
                var lockedUntil = attempts >= MaxAttempts ? DateTime.UtcNow.Add(LockoutWindow) : DateTime.MinValue;
                return (attempts, lockedUntil);
            });
    }
}
