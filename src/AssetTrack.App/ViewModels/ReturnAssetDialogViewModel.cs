using System.Collections.ObjectModel;
using AssetTrack.Core.Abstractions;
using AssetTrack.Core.Entities;
using AssetTrack.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AssetTrack.App.ViewModels;

public partial class ReturnAssetDialogViewModel(IAssetService assetService, ISessionContext session) : ViewModelBase
{
    public ObservableCollection<Asset> AssignedAssets { get; } = [];

    public static IReadOnlyList<ReturnCondition> Conditions { get; } =
        [ReturnCondition.Good, ReturnCondition.Damaged, ReturnCondition.NeedsRepair, ReturnCondition.Scrap];

    [ObservableProperty] private Asset? _selectedAsset;
    [ObservableProperty] private DateTime _returnDate = DateTime.Today;
    [ObservableProperty] private ReturnCondition _condition = ReturnCondition.Good;
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isBusy;

    public event Action<bool>? RequestClose;

    public async Task LoadAsync(int? preselectedAssetId)
    {
        var assets = await assetService.GetAssignedAssetsAsync();
        AssignedAssets.Clear();
        foreach (var a in assets) AssignedAssets.Add(a);

        if (preselectedAssetId.HasValue)
            SelectedAsset = AssignedAssets.FirstOrDefault(a => a.Id == preselectedAssetId.Value);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedAsset is null)
        {
            ErrorMessage = "Select an assigned asset.";
            return;
        }

        IsBusy = true;
        try
        {
            var result = await assetService.ReturnAsync(SelectedAsset.Id, ReturnDate, Condition, Notes, session.CurrentUser!.Id);
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
