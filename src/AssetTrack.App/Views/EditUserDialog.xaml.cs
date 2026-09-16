using System.Windows;
using AssetTrack.App.ViewModels;

namespace AssetTrack.App.Views;

public partial class EditUserDialog : Window
{
    public EditUserDialog(EditUserDialogViewModel viewModel)
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
