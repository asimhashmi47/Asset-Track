namespace AssetTrack.App.ViewModels;

/// <summary>Plain class (not a ValueTuple) so WPF's binding reflection can see named properties.</summary>
public record CategoryBar(string Category, int Count, double Fraction);
