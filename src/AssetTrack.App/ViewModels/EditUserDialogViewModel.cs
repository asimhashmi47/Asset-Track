using AssetTrack.Core.Abstractions;
using AssetTrack.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AssetTrack.App.ViewModels;

public partial class EditUserDialogViewModel(IUserService userService, ISessionContext session) : ViewModelBase
{
    public static IReadOnlyList<UserRole> Roles { get; } = [UserRole.Staff, UserRole.Admin];

    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private UserRole _role;
    [ObservableProperty] private string? _designation;
    [ObservableProperty] private string? _department;
    [ObservableProperty] private string _company = string.Empty;
    [ObservableProperty] private string _city = string.Empty;
    [ObservableProperty] private string? _contact;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isBusy;

    private int _userId;

    public event Action<bool>? RequestClose;

    public void Load(int userId, string username, string displayName, UserRole role, string? designation,
        string department, string company, string city, string? contact)
    {
        _userId = userId;
        Username = username;
        DisplayName = displayName;
        Role = role;
        Designation = designation;
        Department = department;
        Company = company;
        City = city;
        Contact = contact;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        IsBusy = true;
        try
        {
            var result = await userService.UpdateAsync(_userId, DisplayName, Role, Company, Department ?? string.Empty, City,
                session.CurrentUser!.Id, Designation, Contact);
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
