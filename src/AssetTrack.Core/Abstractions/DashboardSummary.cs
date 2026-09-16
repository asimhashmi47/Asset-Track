namespace AssetTrack.Core.Abstractions;

public class DashboardSummary
{
    public int TotalAssets { get; init; }
    public int Assigned { get; init; }
    public int Available { get; init; }
    public int Repair { get; init; }
    public int Scrap { get; init; }
    public int TotalUsers { get; init; }
    public int ReturnsRecorded { get; init; }
    public IReadOnlyList<(string Category, int Count)> ByCategory { get; init; } = [];
}
