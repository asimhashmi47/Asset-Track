using System.Windows;
using System.Windows.Controls;
using AssetTrack.App.ViewModels;
using AssetTrack.Core.Abstractions;
using AssetTrack.Core.Enums;

namespace AssetTrack.App.Views;

public partial class UsersListView : UserControl
{
    public UsersListView() => InitializeComponent();

    private void UsersGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is UsersListViewModel vm && UsersGrid.SelectedItem is UserListItem item)
            vm.OpenUserCommand.Execute(item);
    }

    private UsersListViewModel ViewModel => (UsersListViewModel)DataContext;

    private void AllRoles_Click(object sender, RoutedEventArgs e) => ViewModel.RoleFilter = null;
    private void StandardRole_Click(object sender, RoutedEventArgs e) => ViewModel.RoleFilter = UserRole.Staff;
    private void AdminRole_Click(object sender, RoutedEventArgs e) => ViewModel.RoleFilter = UserRole.Admin;

    private void AllActive_Click(object sender, RoutedEventArgs e) => ViewModel.ActiveFilter = null;
    private void ActiveOnly_Click(object sender, RoutedEventArgs e) => ViewModel.ActiveFilter = true;
    private void InactiveOnly_Click(object sender, RoutedEventArgs e) => ViewModel.ActiveFilter = false;
}
