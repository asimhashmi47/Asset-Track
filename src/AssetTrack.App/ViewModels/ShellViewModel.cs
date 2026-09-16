using AssetTrack.App.Navigation;
using AssetTrack.Core.Abstractions;
using CommunityToolkit.Mvvm.Input;

namespace AssetTrack.App.ViewModels;

public partial class ShellViewModel : ViewModelBase
{
    private readonly INavigationService _navigation;
    private readonly ISessionContext _session;

    public ShellViewModel(INavigationService navigation, ISessionContext session)
    {
        _navigation = navigation;
        _session = session;
        _navigation.CurrentPageChanged += () => OnPropertyChanged(nameof(CurrentPage));
        GoDashboard();
    }

    public object? CurrentPage => _navigation.CurrentPage;

    public bool IsAdmin => _session.IsAdmin;
    public string AssetsNavLabel => IsAdmin ? "Assets" : "My Assets";
    public string CurrentUserDisplayName => _session.CurrentUser?.DisplayName ?? string.Empty;
    public string CurrentUserRoleLabel => _session.IsAdmin ? "Admin" : "Staff";
    public string CurrentUserInitials => string.Concat(
        (_session.CurrentUser?.DisplayName ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2).Select(p => char.ToUpperInvariant(p[0])));

    public event Action? SignedOut;

    public string ActiveNav { get; private set; } = "Dashboard";

    [RelayCommand]
    private void GoDashboard()
    {
        ActiveNav = "Dashboard";
        _navigation.NavigateTo<DashboardViewModel>();
        OnPropertyChanged(nameof(ActiveNav));
    }

    [RelayCommand]
    private void GoAssets()
    {
        ActiveNav = "Assets";

        // Staff get their own "currently assigned + history" page (reusing the user-detail
        // view scoped to themselves), never the org-wide asset list/search.
        if (IsAdmin)
            _navigation.NavigateTo<AssetsListViewModel>();
        else
            _navigation.NavigateTo<UserDetailViewModel>(vm => vm.Load(_session.CurrentUser!.Id));

        OnPropertyChanged(nameof(ActiveNav));
    }

    [RelayCommand]
    private void GoUsers()
    {
        if (!IsAdmin) return;
        ActiveNav = "Users";
        _navigation.NavigateTo<UsersListViewModel>();
        OnPropertyChanged(nameof(ActiveNav));
    }

    [RelayCommand]
    private void GoActivity()
    {
        if (!IsAdmin) return;
        ActiveNav = "Activity";
        _navigation.NavigateTo<ActivityLogViewModel>();
        OnPropertyChanged(nameof(ActiveNav));
    }

    [RelayCommand]
    private void SignOut()
    {
        _session.SignOut();
        SignedOut?.Invoke();
    }
}
