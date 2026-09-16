using AssetTrack.Core.Entities;

namespace AssetTrack.App.Navigation;

/// <summary>Opens modal dialogs, pre-configuring their ViewModel. Returns true if the user saved.</summary>
public interface IDialogService
{
    bool? ShowAllocateAsset(int? preselectedUserId = null);
    bool? ShowReturnAsset(int? preselectedAssetId = null);
    bool? ShowAddAsset();
    bool? ShowAddUser();
    bool? ShowEditAsset(Asset asset);
    bool? ShowEditUser(User user);
    bool? ShowMarkScrap(int assetId, string assetName, string? holderName);
}
