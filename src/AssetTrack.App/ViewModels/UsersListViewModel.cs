using System.Collections.ObjectModel;
using AssetTrack.App.Infrastructure;
using AssetTrack.App.Navigation;
using AssetTrack.Core.Abstractions;
using AssetTrack.Core.Entities;
using AssetTrack.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AssetTrack.App.ViewModels;

public partial class UsersListViewModel : ViewModelBase
{
    private const int PageSize = 10;

    private readonly IUserService _userService;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigation;

    public UsersListViewModel(IUserService userService, IDialogService dialogService, INavigationService navigation)
    {
        _userService = userService;
        _dialogService = dialogService;
        _navigation = navigation;
        FireAndForget(LoadAsync);
    }

    public ObservableCollection<UserListItem> Users { get; } = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private UserRole? _roleFilter;
    [ObservableProperty] private bool? _activeFilter;
    [ObservableProperty] private int _page = 1;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private string _sortBy = "DisplayName";
    [ObservableProperty] private bool _sortAscending = true;

    partial void OnSearchTextChanged(string value) => FireAndForget(ReloadFromFirstPageAsync);
    partial void OnRoleFilterChanged(UserRole? value) => FireAndForget(ReloadFromFirstPageAsync);
    partial void OnActiveFilterChanged(bool? value) => FireAndForget(ReloadFromFirstPageAsync);

    private async Task ReloadFromFirstPageAsync()
    {
        Page = 1;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var result = await _userService.SearchAsync(SearchText, Page, PageSize, RoleFilter, ActiveFilter, SortBy, SortAscending);
        Users.Clear();
        foreach (var u in result.Items) Users.Add(u);
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
    private void OpenUser(UserListItem item) =>
        _navigation.NavigateTo<UserDetailViewModel>(vm => vm.Load(item.User.Id));

    [RelayCommand]
    private async Task AddUserAsync()
    {
        if (_dialogService.ShowAddUser() == true)
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
        var all = await _userService.SearchAsync(SearchText, 1, int.MaxValue, RoleFilter, ActiveFilter, SortBy, SortAscending);
        ExcelExporter.Export("Users.xlsx",
            ["Name", "Username", "Role", "Designation", "Company", "Department", "City", "Contact", "Assets", "Status"],
            all.Items.Select(i => (IReadOnlyList<string>)
            [
                i.User.DisplayName, i.User.Username, i.User.Role == UserRole.Admin ? "Admin" : "Standard",
                i.User.Designation ?? "", i.User.Company, i.User.Division, i.User.City, i.User.Contact ?? "",
                i.AssetCount.ToString(), i.User.IsActive ? "Active" : "Inactive"
            ]));
    }
}
