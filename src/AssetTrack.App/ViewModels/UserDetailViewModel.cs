using System.Collections.ObjectModel;
using AssetTrack.App.Navigation;
using AssetTrack.Core.Abstractions;
using AssetTrack.Core.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AssetTrack.App.ViewModels;

public partial class UserDetailViewModel(IUserService userService, IDialogService dialogService, INavigationService navigation, ISessionContext session)
    : ViewModelBase
{
    [ObservableProperty] private User? _user;
    [ObservableProperty] private bool _showFullHistory;

    public bool IsAdmin => session.IsAdmin;
    public string BackLabel => IsAdmin ? "← All users" : "← Dashboard";

    public ObservableCollection<Asset> CurrentlyAssigned { get; } = [];
    public ObservableCollection<ActivityLog> FullHistory { get; } = [];

    private int _userId;

    public void Load(int userId) => FireAndForget(() => LoadAsync(userId));

    private async Task LoadAsync(int userId)
    {
        _userId = userId;
        User = await userService.GetByIdAsync(userId);

        var assigned = await userService.GetCurrentlyAssignedAsync(userId);
        CurrentlyAssigned.Clear();
        foreach (var a in assigned) CurrentlyAssigned.Add(a);

        var history = await userService.GetFullHistoryAsync(userId);
        FullHistory.Clear();
        foreach (var h in history) FullHistory.Add(h);
    }

    [RelayCommand]
    private void ShowCurrentTab() => ShowFullHistory = false;

    [RelayCommand]
    private void ShowHistoryTab() => ShowFullHistory = true;

    [RelayCommand]
    private async Task AllocateToUserAsync()
    {
        if (dialogService.ShowAllocateAsset(_userId) == true)
            await LoadAsync(_userId);
    }

    [RelayCommand]
    private async Task EditAsync()
    {
        if (User is null) return;
        if (dialogService.ShowEditUser(User) == true)
            await LoadAsync(_userId);
    }

    [RelayCommand]
    private async Task RecordReturnAsync(Asset asset)
    {
        if (dialogService.ShowReturnAsset(asset.Id) == true)
            await LoadAsync(_userId);
    }

    [RelayCommand]
    private void Back()
    {
        // Staff reach this page via "My Assets", not the Users list — send them home.
        if (IsAdmin)
            navigation.NavigateTo<UsersListViewModel>();
        else
            navigation.NavigateTo<DashboardViewModel>();
    }
}
