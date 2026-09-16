using System.Windows;
using AssetTrack.App.ViewModels;

namespace AssetTrack.App.Views;

public partial class EditAssetDialog : Window
{
    public EditAssetDialog(EditAssetDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.RequestClose += saved =>
        {
            DialogResult = saved;
            Close();
        };
    }
}
