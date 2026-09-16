using System.Windows;
using AssetTrack.App.ViewModels;

namespace AssetTrack.App.Views;

public partial class ReturnAssetDialog : Window
{
    private readonly ReturnAssetDialogViewModel _viewModel;
    private readonly int? _preselectedAssetId;

    public ReturnAssetDialog(ReturnAssetDialogViewModel viewModel, int? preselectedAssetId)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _preselectedAssetId = preselectedAssetId;
        DataContext = viewModel;
        viewModel.RequestClose += saved =>
        {
            DialogResult = saved;
            Close();
        };
        Loaded += async (_, _) => await _viewModel.LoadAsync(_preselectedAssetId);
    }
}
