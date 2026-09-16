using AssetTrack.Core.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AssetTrack.App.ViewModels;

public partial class AddAssetDialogViewModel(IAssetService assetService, ISessionContext session) : ViewModelBase
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string? _category = "Laptop";
    [ObservableProperty] private string _serialNumber = string.Empty;
    [ObservableProperty] private string? _make;
    [ObservableProperty] private string? _model;
    [ObservableProperty] private int? _year;
    [ObservableProperty] private string _company = "Northwind Labs";
    [ObservableProperty] private DateTime _dateAdded = DateTime.Today;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isBusy;

    public event Action<bool>? RequestClose;

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Category))
        {
            ErrorMessage = "Category is required.";
            return;
        }

        IsBusy = true;
        try
        {
            var result = await assetService.CreateAsync(Name, Category, SerialNumber, Company, DateAdded, session.CurrentUser!.Id,
                Make, Model, Year);
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
