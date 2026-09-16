using System.Collections.ObjectModel;
using AssetTrack.App.Infrastructure;
using AssetTrack.App.Navigation;
using AssetTrack.Core.Abstractions;
using AssetTrack.Core.Entities;
using AssetTrack.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AssetTrack.App.ViewModels;

public partial class AssetsListViewModel : ViewModelBase
{
    private const int PageSize = 8;

    private readonly IAssetService _assetService;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigation;
    private readonly ISessionContext _session;

    public AssetsListViewModel(IAssetService assetService, IDialogService dialogService, INavigationService navigation, ISessionContext session)
    {
        _assetService = assetService;
        _dialogService = dialogService;
        _navigation = navigation;
        _session = session;
        FireAndForgetAfterConstruction(LoadAsync);
    }

    public bool IsAdmin => _session.IsAdmin;

    public ObservableCollection<Asset> Assets { get; } = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private AssetStatus? _statusFilter;
    [ObservableProperty] private int _page = 1;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private string _sortBy = "AssetTag";
    [ObservableProperty] private bool _sortAscending = true;

    /// <summary>Pre-set the status filter before the first load — used when the Dashboard
    /// navigates here from a stat card (e.g. "Currently assigned").</summary>
    public void SetInitialFilter(AssetStatus? status) => _statusFilter = status;

    partial void OnSearchTextChanged(string value) => FireAndForget(ReloadFromFirstPageAsync);
    partial void OnStatusFilterChanged(AssetStatus? value) => FireAndForget(ReloadFromFirstPageAsync);

    private async Task ReloadFromFirstPageAsync()
    {
        Page = 1;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var result = await _assetService.SearchAsync(SearchText, StatusFilter, null, Page, PageSize, SortBy, SortAscending);
        Assets.Clear();
        foreach (var a in result.Items) Assets.Add(a);
        TotalCount = result.TotalCount;
        TotalPages = Math.Max(1, result.TotalPages);
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (Page >= TotalPages) return;
        Page++;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (Page <= 1) return;
        Page--;
        await LoadAsync();
    }

    [RelayCommand]
    private void ClearStatusFilter() => StatusFilter = null;

    [RelayCommand]
    private void SetStatusFilter(string status) =>
        StatusFilter = Enum.TryParse<AssetStatus>(status, out var s) ? s : null;

    [RelayCommand]
    private void OpenAsset(Asset asset) =>
        _navigation.NavigateTo<AssetDetailViewModel>(vm => vm.Load(asset.Id));

    [RelayCommand]
    private async Task AddAssetAsync()
    {
        if (_dialogService.ShowAddAsset() == true)
            await LoadAsync();
    }

    [RelayCommand]
    private async Task SortByColumnAsync(string column)
    {
        if (SortBy == column) SortAscending = !SortAscending;
        else { SortBy = column; SortAscending = true; }
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ExportToExcelAsync()
    {
        var all = await _assetService.SearchAsync(SearchText, StatusFilter, null, 1, int.MaxValue, SortBy, SortAscending);
        ExcelExporter.Export("Assets.xlsx",
            ["Asset ID", "Name", "Category", "Serial", "Make", "Model", "Year", "Status", "Current User", "Company", "Department", "City"],
            all.Items.Select(a => (IReadOnlyList<string>)
            [
                a.AssetTag, a.Name, a.Category, a.SerialNumber, a.Make ?? "", a.Model ?? "", a.Year?.ToString() ?? "",
                a.Status.ToString(), a.CurrentUser?.DisplayName ?? "", a.Company, a.Division ?? "", a.City ?? ""
            ]));
    }
}
