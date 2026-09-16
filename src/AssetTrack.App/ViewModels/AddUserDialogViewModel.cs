using AssetTrack.Core.Abstractions;
using AssetTrack.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AssetTrack.App.ViewModels;

public partial class AddUserDialogViewModel(IUserService userService, ISessionContext session) : ViewModelBase
{
    public static IReadOnlyList<UserRole> Roles { get; } = [UserRole.Staff, UserRole.Admin];

    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private bool _isPasswordVisible;
    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private UserRole _role = UserRole.Staff;
    [ObservableProperty] private string? _designation;
    [ObservableProperty] private string? _department;
    [ObservableProperty] private string? _company = "Northwind Labs";
    [ObservableProperty] private string _city = string.Empty;
    [ObservableProperty] private string? _contact;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isBusy;

    public event Action<bool>? RequestClose;

    [RelayCommand]
    private void TogglePasswordVisibility() => IsPasswordVisible = !IsPasswordVisible;

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsBusy = true;
        try
        {
            var result = await userService.CreateAsync(Username, Password, DisplayName, Role,
                Company ?? string.Empty, Department ?? string.Empty, City, session.CurrentUser!.Id, Designation, Contact);
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
