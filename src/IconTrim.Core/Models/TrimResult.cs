namespace IconTrim.Core.Models;

/// <summary>Reports the work performed and assets observed during one IconTrim run.</summary>
/// <param name="FilesScanned">Current source files opened and searched rather than reused from cache.</param>
/// <param name="TotalReferences">All icon occurrences in current source files, including repeated names and cached occurrences.</param>
/// <param name="UniqueIcons">Distinct icon names found in source files, before adding safelist entries.</param>
/// <param name="GeneratedIcons">Known icons selected for the resulting font, including safelist entries.</param>
/// <param name="SafelistedIcons">Distinct names in the effective file and programmatic safelist.</param>
/// <param name="UnknownIcons">Unknown requested names retained when unknown-icon behavior is Ignore.</param>
/// <param name="OriginalCssBytes">Byte length of the provider's original CSS input.</param>
/// <param name="GeneratedCssBytes">Byte length of the generated CSS, whether newly written or reused.</param>
/// <param name="OriginalFontBytes">Byte length of the original font input.</param>
/// <param name="GeneratedFontBytes">Byte length of the versioned subset font, whether newly written or reused.</param>
/// <param name="RemovedAssets">Old versioned font files removed during this run; zero on an up-to-date run.</param>
/// <param name="Duration">Elapsed time for this run's scan, fingerprint check, and any generation.</param>
/// <param name="Skipped">True only when the fingerprint matched and both recorded assets existed, so no regeneration was needed.</param>
/// <param name="GeneratedFontPath">Physical path of the current versioned WOFF2 file.</param>
/// <param name="GeneratedCssPath">Physical path of the current generated CSS file.</param>
public sealed record TrimResult(
    int FilesScanned,
    int TotalReferences,
    int UniqueIcons,
    int GeneratedIcons,
    int SafelistedIcons,
    IReadOnlyList<string> UnknownIcons,
    long OriginalCssBytes,
    long GeneratedCssBytes,
    long OriginalFontBytes,
    long GeneratedFontBytes,
    int RemovedAssets,
    TimeSpan Duration,
    bool Skipped,
    string GeneratedFontPath,
    string GeneratedCssPath)
{
    /// <summary>Number of eligible source files found across all configured scan roots.</summary>
    public int FilesDiscovered { get; init; }
    /// <summary>Eligible files whose cached references were reused without reopening their contents.</summary>
    public int FilesReusedFromCache { get; init; }
    /// <summary>Previously cached files no longer eligible or present in the current scan.</summary>
    public int FilesRemovedFromCache { get; init; }

    /// <summary>Combined CSS and font byte reduction relative to the original inputs, as a percentage.</summary>
    /// <remarks>Returns zero when the original combined size is zero. It is still available on an up-to-date run from the existing assets.</remarks>
    public double ReductionPercent => OriginalCssBytes + OriginalFontBytes == 0 ? 0 :
        (1d - (GeneratedCssBytes + GeneratedFontBytes) / (double)(OriginalCssBytes + OriginalFontBytes)) * 100d;
}
