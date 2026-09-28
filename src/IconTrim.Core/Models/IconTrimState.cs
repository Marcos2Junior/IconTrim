namespace IconTrim.Core.Models;

/// <summary>Versioned local cache of file scans and the last successfully generated assets.</summary>
/// <remarks>Scan entries may be newer than the recorded generation, but the generation fingerprint is saved only after both outputs and cleanup finish.</remarks>
public sealed class IconTrimState
{
    /// <summary>JSON state format understood by this release.</summary>
    public const int CurrentVersion = 1;

    /// <summary>Format version used to reject incompatible cached data.</summary>
    public int Version { get; set; } = CurrentVersion;
    /// <summary>Fingerprint of the last completed generation, or null before any completed generation.</summary>
    public string? GenerationFingerprint { get; set; }
    /// <summary>Physical path of the versioned WOFF2 from the last completed generation.</summary>
    public string? LastGeneratedFontPath { get; set; }
    /// <summary>Physical path of the CSS from the last completed generation.</summary>
    public string? LastGeneratedCssPath { get; set; }
    /// <summary>Per-file references keyed by path relative to the configured base path.</summary>
    public Dictionary<string, ScannedFileState> Files { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Metadata and reference locations cached for one eligible source file.</summary>
public sealed class ScannedFileState
{
    /// <summary>File path relative to the configured base path, used as the cache key.</summary>
    public string RelativePath { get; set; } = "";
    /// <summary>File byte length compared before deciding whether to rescan.</summary>
    public long Length { get; set; }
    /// <summary>UTC last-write timestamp ticks compared before deciding whether to rescan.</summary>
    public long LastWriteUtcTicks { get; set; }
    /// <summary>Occurrences retained so unknown icons still have line and column after cache reuse.</summary>
    public List<CachedIconReference> References { get; set; } = [];
}

/// <summary>Location of one icon occurrence within a cached source file.</summary>
/// <param name="IconName">Matched icon name.</param>
/// <param name="Line">One-based line number.</param>
/// <param name="Column">One-based column number.</param>
public sealed record CachedIconReference(string IconName, int Line, int Column);

/// <summary>Current scan result together with the file entries to persist for the next run.</summary>
/// <param name="Scan">References and work counters for this run.</param>
/// <param name="Files">Current file states, excluding deleted or no longer eligible files.</param>
public sealed record IncrementalScanResult(ScanResult Scan, Dictionary<string, ScannedFileState> Files);
