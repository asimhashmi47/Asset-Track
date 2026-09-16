using System.Collections.ObjectModel;
using AssetTrack.Core.Abstractions;
using AssetTrack.Core.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AssetTrack.App.ViewModels;

public partial class AllocateAssetDialogViewModel(IAssetService assetService, IUserService userService, ISessionContext session) : ViewModelBase
{
    public ObservableCollection<Asset> AvailableAssets { get; } = [];
    public ObservableCollection<User> Users { get; } = [];

    [ObservableProperty] private Asset? _selectedAsset;
    [ObservableProperty] private User? _selectedUser;
    [ObservableProperty] private DateTime _allocationDate = DateTime.Today;
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isBusy;

    public event Action<bool>? RequestClose;

    public async Task LoadAsync(int? preselectedUserId)
    {
        var assets = await assetService.GetAvailableAssetsAsync();
        AvailableAssets.Clear();
        foreach (var a in assets) AvailableAssets.Add(a);

        var users = await userService.GetActiveUsersAsync();
        Users.Clear();
        foreach (var u in users) Users.Add(u);

        if (preselectedUserId.HasValue)
            SelectedUser = Users.FirstOrDefault(u => u.Id == preselectedUserId.Value);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedAsset is null || SelectedUser is null)
        {
            ErrorMessage = "Select an asset and a user.";
            return;
        }

        IsBusy = true;
        try
        {
            var result = await assetService.AllocateAsync(SelectedAsset.Id, SelectedUser.Id, AllocationDate, Notes, session.CurrentUser!.Id);
            if (!result.Success)
            {
                ErrorMessage = result.Error;
                return;
            }

            RequestClose?.Invoke(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(false);
}
