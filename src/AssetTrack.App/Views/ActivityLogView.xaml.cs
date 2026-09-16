using System.Windows;
using System.Windows.Controls;
using AssetTrack.App.ViewModels;

namespace AssetTrack.App.Views;

public partial class ActivityLogView : UserControl
{
    public ActivityLogView() => InitializeComponent();

    private void ClearFilter_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is ActivityLogViewModel vm)
            vm.EventTypeFilter = null;
    }
}
