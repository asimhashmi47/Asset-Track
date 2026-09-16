using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AssetTrack.App.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    /// <summary>
    /// Starts an async load from a constructor or a fire-and-forget call site. Observes
    /// any exception (so it never becomes an unobserved-task crash) instead of losing it.
    /// </summary>
    protected static void FireAndForget(Func<Task> work)
    {
        _ = RunAsync();

        async Task RunAsync()
        {
            try
            {
                await work();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unhandled load/save failure: {ex}");
            }
        }
    }

    /// <summary>
    /// Like <see cref="FireAndForget"/>, but defers starting the work until after the current
    /// call stack unwinds. Needed when a ViewModel is constructed via
    /// <c>INavigationService.NavigateTo(configure)</c>: the caller's <c>configure</c> callback
    /// (e.g. presetting a filter) runs synchronously right after the constructor returns, and
    /// this ensures the initial load reads that preset value instead of racing it — an eagerly
    /// awaited method call captures its arguments before the caller has had a chance to run.
    /// </summary>
    protected static void FireAndForgetAfterConstruction(Func<Task> work) =>
        Application.Current?.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () => FireAndForget(work));
}
