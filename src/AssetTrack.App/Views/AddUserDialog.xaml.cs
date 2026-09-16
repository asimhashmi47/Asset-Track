using System.Windows;
using AssetTrack.App.ViewModels;

namespace AssetTrack.App.Views;

public partial class AddUserDialog : Window
{
    private readonly AddUserDialogViewModel _viewModel;

    public AddUserDialog(AddUserDialogViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.RequestClose += saved =>
        {
            DialogResult = saved;
            Close();
        };
        // PasswordBox can't be data-bound; keep its masked value in sync whenever we switch
        // back to it from the plain-text view (which IS bound and may have changed).
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(AddUserDialogViewModel.IsPasswordVisible) && !viewModel.IsPasswordVisible)
                PasswordBoxInput.Password = viewModel.Password;
        };
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e) =>
        _viewModel.Password = PasswordBoxInput.Password;
}
