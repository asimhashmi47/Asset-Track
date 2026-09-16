using System.Collections.ObjectModel;
using AssetTrack.App.Navigation;
using AssetTrack.Core.Abstractions;
using AssetTrack.Core.Entities;
using AssetTrack.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AssetTrack.App.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly IAssetService _assetService;
    private readonly IUserService _userService;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigation;
    private readonly ISessionContext _session;

    public DashboardViewModel(IAssetService assetService, IUserService userService, IDialogService dialogService,
        INavigationService navigation, ISessionContext session)
    {
        _assetService = assetService;
        _userService = userService;
        _dialogService = dialogService;
        _navigation = navigation;
        _session = session;
        FireAndForget(LoadAsync);
    }

    public bool IsAdmin => _session.IsAdmin;

    // Admin: org-wide numbers.
    [ObservableProperty] private DashboardSummary? _summary;
    public ObservableCollection<CategoryBar> CategoryBreakdown { get; } = [];
    public ObservableCollection<ActivityLog> RecentActivity { get; } = [];

    // Staff: only their own data — never the org-wide summary above.
    [ObservableProperty] private int _myCurrentAssetCount;
    [ObservableProperty] private int _myEverHeldCount;

    private async Task LoadAsync()
    {
        if (IsAdmin)
        {
            Summary = await _assetService.GetDashboardSummaryAsync();

            CategoryBreakdown.Clear();
            var max = Summary.ByCategory.Count == 0 ? 1 : Summary.ByCategory.Max(c => c.Count);
            foreach (var (category, count) in Summary.ByCategory)
                CategoryBreakdown.Add(new CategoryBar(category, count, max == 0 ? 0 : count / (double)max));

            var recent = await _assetService.GetRecentActivityAsync(8);
            RecentActivity.Clear();
            foreach (var r in recent) RecentActivity.Add(r);
        }
        else
        {
            var userId = _session.CurrentUser!.Id;
            var current = await _userService.GetCurrentlyAssignedAsync(userId);
            var history = await _userService.GetFullHistoryAsync(userId);

            MyCurrentAssetCount = current.Count;
            MyEverHeldCount = history.Select(h => h.AssetId).Distinct().Count();

            RecentActivity.Clear();
            foreach (var r in history.Take(8)) RecentActivity.Add(r);
        }
    }

    [RelayCommand]
    private async Task AllocateAssetAsync()
    {
        if (_dialogService.ShowAllocateAsset() == true)
            await LoadAsync();
    }

    [RelayCommand]
    private async Task RecordReturnAsync()
    {
        if (_dialogService.ShowReturnAsset() == true)
            await LoadAsync();
    }

    [RelayCommand]
    private void OpenAssetsFiltered(string? statusText)
    {
        if (!IsAdmin) return;
        var status = Enum.TryParse<AssetStatus>(statusText, out var s) ? (AssetStatus?)s : null;
        _navigation.NavigateTo<AssetsListViewModel>(vm => vm.SetInitialFilter(status));
    }

    [RelayCommand]
    private void OpenReturnsActivity()
    {
        if (!IsAdmin) return;
        _navigation.NavigateTo<ActivityLogViewModel>(vm => vm.SetInitialFilter(ActivityEventType.Returned));
    }
}
