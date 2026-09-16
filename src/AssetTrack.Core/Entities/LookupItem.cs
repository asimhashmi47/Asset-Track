using AssetTrack.Core.Enums;

namespace AssetTrack.Core.Entities;

/// <summary>
/// One option in a searchable-dropdown-with-add-new list (asset category, job designation,
/// department, company). Shared and reusable: adding "Marketing" once from any dropdown of
/// that kind makes it available everywhere. The value itself lives as a plain string on the
/// owning Asset/User row — this table is only the source of known options, not a foreign key
/// target, so existing free-text data never needs a migration.
/// </summary>
public class LookupItem
{
    public int Id { get; set; }
    public LookupKind Kind { get; set; }
    public required string Name { get; set; }
}
