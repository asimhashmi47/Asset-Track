using AssetTrack.Core.Entities;

namespace AssetTrack.Core.Abstractions;

/// <summary>Holds the signed-in user for the lifetime of the desktop process.</summary>
public interface ISessionContext
{
    User? CurrentUser { get; }
    bool IsAdmin { get; }
    void SignIn(User user);
    void SignOut();
}
