using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AssetTrack.Core.Abstractions;
using AssetTrack.Core.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace AssetTrack.App.Controls;

/// <summary>
/// A searchable dropdown backed by a shared, DB-persisted lookup list (category, designation,
/// department, company). Type to filter; if nothing matches, an "Add '&lt;text&gt;'" option
/// appears — picking it (or just tabbing away with unmatched text) selects the value and adds
/// it to the shared list, so it's available to everyone from then on.
/// </summary>
/// <remarks>
/// Deliberately a TextBox + Popup rather than an editable ComboBox: WPF's editable ComboBox
/// writes its own Text property internally whenever the selection changes, which clobbers a
/// two-way Text binding and leaves the bound value and the visible text permanently out of sync.
/// Here nothing but this class writes the text.
/// </remarks>
public partial class SearchableDropdown : UserControl
{
    private const string AddPrefix = "+ Add “";
    private const string AddSuffix = "”";

    private List<string> _allItems = [];
    private bool _isUpdatingProgrammatically;

    private Window? _hostWindow;

    public SearchableDropdown()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        // The popup is StaysOpen (see the XAML) so it never captures the mouse, which means
        // nothing closes it on its own. Watch the host window instead: a click anywhere else,
        // or the window moving or losing activation, dismisses it.
        if (_hostWindow is null)
        {
            _hostWindow = Window.GetWindow(this);
            if (_hostWindow is not null)
            {
                _hostWindow.PreviewMouseDown += OnHostWindowPreviewMouseDown;
                _hostWindow.Deactivated += OnHostWindowDismissed;
                _hostWindow.LocationChanged += OnHostWindowDismissed;
            }
        }

        await LoadItemsAsync();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_hostWindow is null) return;

        _hostWindow.PreviewMouseDown -= OnHostWindowPreviewMouseDown;
        _hostWindow.Deactivated -= OnHostWindowDismissed;
        _hostWindow.LocationChanged -= OnHostWindowDismissed;
        _hostWindow = null;
    }

    private void OnHostWindowDismissed(object? sender, EventArgs e) => Suggestions.IsOpen = false;

    private void OnHostWindowPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!Suggestions.IsOpen) return;

        // Popup content sits in its own visual root but still routes events up to this window,
        // so the popup has to be checked explicitly -- walking the visual tree from a suggestion
        // stops at the popup root and would otherwise look like an outside click.
        if (e.OriginalSource is DependencyObject source &&
            (IsWithin(source, this) || (Suggestions.Child is not null && IsWithin(source, Suggestions.Child))))
            return;

        Suggestions.IsOpen = false;
        Commit(TypedText);
    }

    private static bool IsWithin(DependencyObject? node, DependencyObject root)
    {
        while (node is not null)
        {
            if (ReferenceEquals(node, root)) return true;
            node = node is Visual ? VisualTreeHelper.GetParent(node) : LogicalTreeHelper.GetParent(node);
        }

        return false;
    }

    public static readonly DependencyProperty KindProperty =
        DependencyProperty.Register(nameof(Kind), typeof(LookupKind), typeof(SearchableDropdown),
            new PropertyMetadata(LookupKind.Category));

    public LookupKind Kind
    {
        get => (LookupKind)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public static readonly DependencyProperty SelectedValueProperty =
        DependencyProperty.Register(nameof(SelectedValue), typeof(string), typeof(SearchableDropdown),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedValueChanged));

    public string? SelectedValue
    {
        get => (string?)GetValue(SelectedValueProperty);
        set => SetValue(SelectedValueProperty, value);
    }

    public ObservableCollection<string> FilteredItems { get; } = [];

    public static readonly DependencyProperty TypedTextProperty =
        DependencyProperty.Register(nameof(TypedText), typeof(string), typeof(SearchableDropdown),
            new PropertyMetadata(string.Empty, OnTypedTextChanged));

    public string TypedText
    {
        get => (string)GetValue(TypedTextProperty);
        set => SetValue(TypedTextProperty, value);
    }

    private static void OnSelectedValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (SearchableDropdown)d;
        if (control._isUpdatingProgrammatically) return;

        control._isUpdatingProgrammatically = true;
        control.TypedText = (e.NewValue as string) ?? string.Empty;
        control._isUpdatingProgrammatically = false;
    }

    private static void OnTypedTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (SearchableDropdown)d;
        control.RefreshFilter();

        // Typing (as opposed to a programmatic set) should reveal the matches and the "+ Add"
        // option, the way any search box does.
        if (!control._isUpdatingProgrammatically && control.Input.IsKeyboardFocusWithin)
            control.OpenPopup();
    }

    private async Task LoadItemsAsync()
    {
        var lookupService = App.Services.GetRequiredService<ILookupService>();
        _allItems = (await lookupService.GetValuesAsync(Kind)).ToList();
        RefreshFilter();
    }

    private void RefreshFilter()
    {
        var text = (TypedText ?? string.Empty).Trim();

        var matches = string.IsNullOrEmpty(text)
            ? _allItems
            : _allItems.Where(i => i.Contains(text, StringComparison.OrdinalIgnoreCase)).ToList();

        FilteredItems.Clear();

        foreach (var m in matches.Take(50))
            FilteredItems.Add(m);

        // "+ Add" goes last, after the real matches: typing a prefix and pressing Down+Enter
        // should land on the obvious existing match, not silently create a new entry from a
        // half-typed word.
        var hasExactMatch = _allItems.Any(i => string.Equals(i, text, StringComparison.OrdinalIgnoreCase));
        if (text.Length > 0 && !hasExactMatch)
            FilteredItems.Add($"{AddPrefix}{text}{AddSuffix}");
    }

    private void OpenPopup()
    {
        if (FilteredItems.Count == 0)
        {
            Suggestions.IsOpen = false;
            return;
        }

        Suggestions.IsOpen = true;
    }

    private void Input_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) => ShowFullListOnOpen();

    // Clicking a field that already holds focus won't raise GotKeyboardFocus, so without this
    // the list can't be reopened after it has been dismissed.
    private void Input_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => ShowFullListOnOpen();

    // Opening the field (focus or click) shows every value, not just ones matching the text
    // already in the box — an Edit dialog pre-fills this control with the current value, so
    // filtering by that text on open would show a list of one and make the control look broken.
    // RefreshFilter's text-based filtering only kicks in once the user actually types
    // (OnTypedTextChanged), which is also when it's meaningful to offer "+ Add".
    private void ShowFullListOnOpen()
    {
        FilteredItems.Clear();
        foreach (var m in _allItems.Take(50))
            FilteredItems.Add(m);

        if (FilteredItems.Count == 0)
        {
            Suggestions.IsOpen = false;
            return;
        }

        var current = (TypedText ?? string.Empty).Trim();
        if (current.Length > 0)
        {
            var index = FilteredItems.IndexOf(FilteredItems.FirstOrDefault(i => string.Equals(i, current, StringComparison.OrdinalIgnoreCase)) ?? string.Empty);
            if (index >= 0)
            {
                SuggestionList.SelectedIndex = index;
                SuggestionList.ScrollIntoView(SuggestionList.SelectedItem);
            }
        }

        Suggestions.IsOpen = true;
    }

    private void Input_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down:
                if (!Suggestions.IsOpen) OpenPopup();
                else MoveHighlight(1);
                e.Handled = true;
                break;

            case Key.Up:
                if (Suggestions.IsOpen) MoveHighlight(-1);
                e.Handled = true;
                break;

            // First Enter commits this field; a second one falls through to the dialog's
            // default Save button.
            case Key.Enter:
                if (Suggestions.IsOpen)
                {
                    Commit(SuggestionList.SelectedItem as string ?? TypedText);
                    e.Handled = true;
                }
                break;

            case Key.Escape:
                if (Suggestions.IsOpen)
                {
                    Suggestions.IsOpen = false;
                    e.Handled = true;
                }
                break;
        }
    }

    private void MoveHighlight(int delta)
    {
        if (FilteredItems.Count == 0) return;

        var next = SuggestionList.SelectedIndex + delta;
        SuggestionList.SelectedIndex = Math.Clamp(next, 0, FilteredItems.Count - 1);
        SuggestionList.ScrollIntoView(SuggestionList.SelectedItem);
    }

    // PreviewMouseLeftButtonDown + Handled: the click commits the value without ever moving
    // keyboard focus off the TextBox, so picking a suggestion can't trigger the blur path.
    private void SuggestionList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source) return;

        var container = ItemsControl.ContainerFromElement(SuggestionList, source) as ListBoxItem;
        if (container?.Content is not string picked) return;

        Commit(picked);
        e.Handled = true;
    }

    private void Input_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        Suggestions.IsOpen = false;
        Commit(TypedText);
    }

    /// <summary>
    /// Applies a value to both the visible text and the bound <see cref="SelectedValue"/>
    /// synchronously, so a Save that happens immediately afterwards always sees it. Persisting
    /// a brand-new value to the shared lookup list happens afterwards and never gates the value.
    /// </summary>
    private void Commit(string? value)
    {
        var text = (value ?? string.Empty).Trim();

        if (text.StartsWith(AddPrefix, StringComparison.Ordinal) && text.EndsWith(AddSuffix, StringComparison.Ordinal))
            text = text[AddPrefix.Length..^AddSuffix.Length];

        var resolved = string.IsNullOrEmpty(text)
            ? null
            : _allItems.FirstOrDefault(i => string.Equals(i, text, StringComparison.OrdinalIgnoreCase)) ?? text;

        _isUpdatingProgrammatically = true;
        SelectedValue = resolved;
        TypedText = resolved ?? string.Empty;
        RefreshFilter();
        _isUpdatingProgrammatically = false;

        Suggestions.IsOpen = false;

        if (resolved is not null && !_allItems.Contains(resolved, StringComparer.OrdinalIgnoreCase))
            _ = PersistNewValueAsync(resolved);
    }

    private async Task PersistNewValueAsync(string value)
    {
        var lookupService = App.Services.GetRequiredService<ILookupService>();
        var result = await lookupService.AddValueAsync(Kind, value);
        if (!result.Success || result.Value is null) return;

        if (!_allItems.Contains(result.Value, StringComparer.OrdinalIgnoreCase))
        {
            _allItems.Add(result.Value);
            RefreshFilter();
        }
    }
}
