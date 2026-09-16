using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace AssetTrack.App.Controls;

public partial class AppFooter : UserControl
{
    public AppFooter() => InitializeComponent();

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}
