using System.Windows.Controls;
using AssetTrack.App.ViewModels;
using AssetTrack.Core.Entities;

namespace AssetTrack.App.Views;

public partial class AssetsListView : UserControl
{
    public AssetsListView() => InitializeComponent();

    private void AssetsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is AssetsListViewModel vm && AssetsGrid.SelectedItem is Asset asset)
            vm.OpenAssetCommand.Execute(asset);
    }
}
