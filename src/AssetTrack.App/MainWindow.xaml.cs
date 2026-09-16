using System.Windows;
using AssetTrack.App.ViewModels;

namespace AssetTrack.App;

public partial class MainWindow : Window
{
    public bool WasSignedOut { get; private set; }

    public MainWindow(ShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Title = $"AssetTrack — {viewModel.CurrentUserRoleLabel}";
        viewModel.SignedOut += () =>
        {
            WasSignedOut = true;
            Close();
        };
    }
}
