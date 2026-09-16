using AssetTrack.Core.Abstractions;
using AssetTrack.Core.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AssetTrack.App.ViewModels;

public partial class LoginViewModel(IAuthService authService, ISessionContext session) : ViewModelBase
{
    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isBusy;

    public event Action<User>? SignedIn;

    [RelayCommand]
    private async Task SignInAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var result = await authService.SignInAsync(Username, Password);
            if (!result.Success || result.Value is null)
            {
                ErrorMessage = result.Error;
                return;
            }

            session.SignIn(result.Value);
            SignedIn?.Invoke(result.Value);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void FillDemoAdmin()
    {
        Username = "admin";
        Password = "admin123";
    }

    public void FillDemoStaff()
    {
        Username = "arjun";
        Password = "user123";
    }
}
