namespace AssetTrack.App.Navigation;

/// <summary>Swaps the shell's current page ViewModel. Views are resolved via DataTemplates.</summary>
public interface INavigationService
{
    object? CurrentPage { get; }
    event Action? CurrentPageChanged;
    void NavigateTo<TViewModel>() where TViewModel : class;
    void NavigateTo<TViewModel>(Action<TViewModel> configure) where TViewModel : class;
}
