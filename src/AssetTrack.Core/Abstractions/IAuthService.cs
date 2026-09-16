using AssetTrack.Core.Entities;

namespace AssetTrack.Core.Abstractions;

public interface IAuthService
{
    Task<ServiceResult<User>> SignInAsync(string username, string password);
}
