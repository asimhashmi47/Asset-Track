using System.Windows;
using AssetTrack.App.ViewModels;

namespace AssetTrack.App.Views;

public partial class AllocateAssetDialog : Window
{
    private readonly AllocateAssetDialogViewModel _viewModel;
    private readonly int? _preselectedUserId;

    public AllocateAssetDialog(AllocateAssetDialogViewModel viewModel, int? preselectedUserId)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _preselectedUserId = preselectedUserId;
        DataContext = viewModel;
        viewModel.RequestClose += saved =>
        {
            DialogResult = saved;
            Close();
        };
        // Load once the dialog is actually on screen, not before ShowDialog's modal loop
        // starts — avoids racing the window's own initial render.
        Loaded += async (_, _) => await _viewModel.LoadAsync(_preselectedUserId);
    }
}
