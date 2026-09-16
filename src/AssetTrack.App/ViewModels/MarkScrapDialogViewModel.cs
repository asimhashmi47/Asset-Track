using AssetTrack.Core.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AssetTrack.App.ViewModels;

public partial class MarkScrapDialogViewModel(IAssetService assetService, ISessionContext session) : ViewModelBase
{
    [ObservableProperty] private string _assetName = string.Empty;
    [ObservableProperty] private string? _holderName;
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isBusy;

    private int _assetId;

    public event Action<bool>? RequestClose;

    public void Load(int assetId, string assetName, string? holderName)
    {
        _assetId = assetId;
        AssetName = assetName;
        HolderName = holderName;
    }

    [RelayCommand]
    private async Task ConfirmAsync()
    {
        IsBusy = true;
        try
        {
            var result = await assetService.MarkScrapAsync(_assetId, Notes, session.CurrentUser!.Id);
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
