using AssetTrack.Core.Enums;

namespace AssetTrack.Core.Abstractions;

/// <summary>Backs every searchable-dropdown-with-add-new field (category, designation, department, company).</summary>
public interface ILookupService
{
    Task<IReadOnlyList<string>> GetValuesAsync(LookupKind kind);

    /// <summary>Adds a new value if it doesn't already exist (case-insensitive); returns the canonical stored value either way.</summary>
    Task<ServiceResult<string>> AddValueAsync(LookupKind kind, string value);
}
