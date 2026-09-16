using System.Windows;
using AssetTrack.App.ViewModels;

namespace AssetTrack.App.Views;

public partial class AddAssetDialog : Window
{
    public AddAssetDialog(AddAssetDialogViewModel viewModel)
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
