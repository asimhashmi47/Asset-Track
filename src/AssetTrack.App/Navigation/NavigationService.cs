using Microsoft.Extensions.DependencyInjection;

namespace AssetTrack.App.Navigation;

public class NavigationService(IServiceProvider serviceProvider) : INavigationService
{
    public object? CurrentPage { get; private set; }
    public event Action? CurrentPageChanged;

    public void NavigateTo<TViewModel>() where TViewModel : class
    {
        CurrentPage = serviceProvider.GetRequiredService<TViewModel>();
        CurrentPageChanged?.Invoke();
    }

    public void NavigateTo<TViewModel>(Action<TViewModel> configure) where TViewModel : class
    {
        var vm = serviceProvider.GetRequiredService<TViewModel>();
        configure(vm);
        CurrentPage = vm;
        CurrentPageChanged?.Invoke();
    }
}
