using AssetTrack.Core.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AssetTrack.App.ViewModels;

public partial class EditAssetDialogViewModel(IAssetService assetService, ISessionContext session) : ViewModelBase
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string? _category;
    [ObservableProperty] private string _serialNumber = string.Empty;
    [ObservableProperty] private string? _make;
    [ObservableProperty] private string? _model;
    [ObservableProperty] private int? _year;
    [ObservableProperty] private string _company = string.Empty;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isBusy;

    private int _assetId;

    public event Action<bool>? RequestClose;

    public void Load(int assetId, string name, string category, string serialNumber, string company,
        string? make, string? model, int? year)
    {
        _assetId = assetId;
        Name = name;
        Category = category;
        SerialNumber = serialNumber;
        Company = company;
        Make = make;
        Model = model;
        Year = year;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(Category))
        {
            ErrorMessage = "Category is required.";
            return;
        }

        IsBusy = true;
        try
        {
            var result = await assetService.UpdateAsync(_assetId, Name, Category, SerialNumber, Company, session.CurrentUser!.Id,
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
