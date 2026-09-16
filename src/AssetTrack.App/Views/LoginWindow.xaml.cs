using System.Windows;
using AssetTrack.App.ViewModels;

namespace AssetTrack.App.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e) =>
        _viewModel.Password = PasswordBox.Password;

    private void FillAdmin_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.FillDemoAdmin();
        UsernameBox.Text = _viewModel.Username;
        PasswordBox.Password = _viewModel.Password;
    }

    private void FillStaff_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.FillDemoStaff();
        UsernameBox.Text = _viewModel.Username;
        PasswordBox.Password = _viewModel.Password;
    }
}
