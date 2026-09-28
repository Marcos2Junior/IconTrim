namespace IconTrim.Core.Models;

/// <summary>References found in the current scan roots and counters for incremental reuse.</summary>
/// <param name="FilesScanned">Files whose contents were opened and searched during this scan.</param>
/// <param name="References">All current occurrences, including references restored from cache.</param>
public sealed record ScanResult(int FilesScanned, IReadOnlyList<IconReference> References)
{
    /// <summary>Total eligible files discovered across the configured roots.</summary>
    public int FilesDiscovered { get; init; }
    /// <summary>Files reused because path, length, and UTC modification time matched cached state.</summary>
    public int FilesReusedFromCache { get; init; }
    /// <summary>Previously cached files absent from the current eligible file set.</summary>
    public int FilesRemovedFromCache { get; init; }
    /// <summary>Number of icon occurrences, including duplicates.</summary>
    public int TotalReferences => References.Count;
    /// <summary>Number of distinct icon names found in source files, excluding safelist-only names.</summary>
    public int UniqueIcons => References.Select(x => x.IconName).Distinct(StringComparer.OrdinalIgnoreCase).Count();
}
