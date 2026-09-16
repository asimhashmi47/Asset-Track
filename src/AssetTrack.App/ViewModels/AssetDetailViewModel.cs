using System.Collections.ObjectModel;
using AssetTrack.App.Navigation;
using AssetTrack.Core.Abstractions;
using AssetTrack.Core.Entities;
using AssetTrack.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AssetTrack.App.ViewModels;

public partial class AssetDetailViewModel(IAssetService assetService, IDialogService dialogService, INavigationService navigation, ISessionContext session)
    : ViewModelBase
{
    [ObservableProperty] private Asset? _asset;
    [ObservableProperty] private DateTime? _currentOwnershipSince;

    public ObservableCollection<ActivityLog> History { get; } = [];

    public bool IsAdmin => session.IsAdmin;
    public bool CanReturn => Asset?.Status == AssetStatus.Assigned && IsAdmin;
    public bool CanScrap => Asset?.Status != AssetStatus.Scrap && IsAdmin;
    public bool CanEdit => IsAdmin;

    public void Load(int assetId) => FireAndForget(() => LoadAsync(assetId));

    private int _assetId;

    private async Task LoadAsync(int assetId)
    {
        _assetId = assetId;
        Asset = await assetService.GetByIdAsync(assetId);
        OnPropertyChanged(nameof(CanReturn));
        OnPropertyChanged(nameof(CanScrap));

        var history = await assetService.GetHistoryAsync(assetId);
        History.Clear();
        foreach (var h in history) History.Add(h);

        // History is newest-first, so the first Allocated row is when the *current*
        // holder took the asset (not when the asset itself was created).
        CurrentOwnershipSince = Asset?.CurrentUserId is not null
            ? History.FirstOrDefault(h => h.EventType == ActivityEventType.Allocated)?.TimestampUtc
            : null;
    }

    [RelayCommand]
    private async Task RecordReturnAsync()
    {
        if (Asset is null) return;
        if (dialogService.ShowReturnAsset(Asset.Id) == true)
            await LoadAsync(_assetId);
    }

    [RelayCommand]
    private async Task EditAsync()
    {
        if (Asset is null) return;
        if (dialogService.ShowEditAsset(Asset) == true)
            await LoadAsync(_assetId);
    }

    [RelayCommand]
    private async Task MarkScrapAsync()
    {
        if (Asset is null) return;
        if (dialogService.ShowMarkScrap(Asset.Id, Asset.Name, Asset.CurrentUser?.DisplayName) == true)
            await LoadAsync(_assetId);
    }

    [RelayCommand]
    private void Back() => navigation.NavigateTo<AssetsListViewModel>();
}
