namespace IconTrim.Core.Configuration;

/// <summary>Controls how references absent from the selected icon provider are handled.</summary>
public enum UnknownIconBehavior
{
    /// <summary>Abort before generating assets and report the unknown names and source locations.</summary>
    Throw,
    /// <summary>Exclude unknown names and continue if at least one known icon remains.</summary>
    Ignore
}

/// <summary>Configures scanning, generation, and incremental state for one consumer.</summary>
public sealed class IconTrimOptions
{
    /// <summary>Absolute directory used to resolve every relative path in these options and provider options.</summary>
    /// <remarks>DI consumers must set an absolute path. The CLI uses the configuration file's directory when this is empty. Fully qualified paths are used as supplied and do not depend on this base.</remarks>
    public string BasePath { get; set; } = "";
    /// <summary>JSON cache path for per-file scans and the last completed generation; defaults to <c>obj/icontrim/state.json</c>.</summary>
    /// <remarks>Relative paths resolve against <see cref="BasePath"/>; absolute paths are accepted. An empty path is an error. Parent directories are created when saving. The file need not exist; deleting it causes a full scan and regeneration. Missing, corrupt, or incompatible state is rebuilt.</remarks>
    public string StatePath { get; set; } = "obj/icontrim/state.json";
    /// <summary>Optional input file listing icons that static scanning cannot discover, such as dynamically assembled class names.</summary>
    /// <remarks>Relative paths resolve against <see cref="BasePath"/>; absolute paths are also accepted. A null, empty, or missing file contributes no icons. Blank lines and lines beginning with <c>#</c> after trimming are ignored. Duplicate names are ignored without regard to case.</remarks>
    public string? SafelistFile { get; set; }
    /// <summary>Additional icon names supplied directly by the consumer for uses that static scanning cannot find.</summary>
    /// <remarks>Defaults to an empty list. These names are combined with <see cref="SafelistFile"/> and scanned references; duplicate names are ignored without regard to case.</remarks>
    public List<string> Safelist { get; set; } = [];
    /// <summary>Determines whether unknown icon references abort generation or are excluded.</summary>
    /// <remarks>Defaults to <see cref="Configuration.UnknownIconBehavior.Throw"/>.</remarks>
    public UnknownIconBehavior UnknownIconBehavior { get; set; } = UnknownIconBehavior.Throw;
    /// <summary>Controls which files are searched for icon references and how matches are recognized.</summary>
    public ScanOptions Scan { get; set; } = new();
    /// <summary>Controls the physical output locations, generated asset name, and public font URL.</summary>
    public OutputOptions Output { get; set; } = new();
    /// <summary>Identifies the original font from which the subset is generated.</summary>
    public FontOptions Font { get; set; } = new();
}
