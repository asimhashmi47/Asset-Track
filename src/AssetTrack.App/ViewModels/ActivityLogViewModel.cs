using System.Collections.ObjectModel;
using AssetTrack.App.Infrastructure;
using AssetTrack.Core.Abstractions;
using AssetTrack.Core.Entities;
using AssetTrack.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AssetTrack.App.ViewModels;

public partial class ActivityLogViewModel : ViewModelBase
{
    private const int PageSize = 15;
    private readonly IActivityLogService _activityLogService;

    public ActivityLogViewModel(IActivityLogService activityLogService)
    {
        _activityLogService = activityLogService;
        FireAndForgetAfterConstruction(LoadAsync);
    }

    public static IReadOnlyList<ActivityEventType> EventTypes { get; } = Enum.GetValues<ActivityEventType>();

    public ObservableCollection<ActivityLog> Entries { get; } = [];

    [ObservableProperty] private ActivityEventType? _eventTypeFilter;
    [ObservableProperty] private int _page = 1;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private string _sortBy = "Timestamp";
    [ObservableProperty] private bool _sortAscending;

    /// <summary>Pre-set the event-type filter before the first load — used when the Dashboard
    /// navigates here from the "Returns recorded" stat card.</summary>
    public void SetInitialFilter(ActivityEventType? eventType) => _eventTypeFilter = eventType;

    partial void OnEventTypeFilterChanged(ActivityEventType? value) => FireAndForget(ReloadFromFirstPageAsync);

    private async Task ReloadFromFirstPageAsync()
    {
        Page = 1;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var result = await _activityLogService.SearchAsync(Page, PageSize, EventTypeFilter, SortBy, SortAscending);
        Entries.Clear();
        foreach (var e in result.Items) Entries.Add(e);
        TotalPages = Math.Max(1, result.TotalPages);
        TotalCount = result.TotalCount;
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (Page >= TotalPages) return;
        Page++;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (Page <= 1) return;
        Page--;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task SortByColumnAsync(string column)
    {
        if (SortBy == column) SortAscending = !SortAscending;
        else { SortBy = column; SortAscending = true; }
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ExportToExcelAsync()
    {
        var all = await _activityLogService.SearchAsync(1, int.MaxValue, EventTypeFilter, SortBy, SortAscending);
        ExcelExporter.Export("ActivityLog.xlsx",
            ["Timestamp", "Actor", "Action", "Asset", "Description", "Notes"],
            all.Items.Select(a => (IReadOnlyList<string>)
            [
                a.TimestampUtc.ToString("d MMM yyyy, HH:mm"), a.ActorUser?.DisplayName ?? "", a.EventType.ToString(),
                a.Asset?.AssetTag ?? "", a.Description, a.Notes ?? ""
            ]));
    }
}
