using AssetTrack.Core.Abstractions;
using AssetTrack.Core.Entities;
using AssetTrack.Core.Enums;

namespace AssetTrack.Core.Services;

public class SessionContext : ISessionContext
{
    public User? CurrentUser { get; private set; }
    public bool IsAdmin => CurrentUser?.Role == UserRole.Admin;

    public void SignIn(User user) => CurrentUser = user;
    public void SignOut() => CurrentUser = null;
}
