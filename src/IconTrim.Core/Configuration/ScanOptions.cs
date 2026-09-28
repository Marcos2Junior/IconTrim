namespace IconTrim.Core.Configuration;

/// <summary>Selects the source files searched for icon class references.</summary>
public sealed class ScanOptions
{
    /// <summary>Directories recursively scanned for icon references; multiple roots are supported.</summary>
    /// <remarks>Each entry may be absolute or relative to <see cref="IconTrimOptions.BasePath"/>. Every configured directory must exist. An empty list is an error; overlapping roots are scanned only once.</remarks>
    public List<string> Roots { get; set; } = [];
    /// <summary>Literal prefix used to recognize icon names in source files.</summary>
    /// <remarks>The scanner matches the prefix followed by one or more ASCII letters, digits, or hyphens. Configure the provider's prefix, for example <c>bi-</c> for Bootstrap Icons. An empty prefix is an error; no provider prefix is assumed by Core.</remarks>
    public string IconPrefix { get; set; } = "";
    /// <summary>File extensions eligible for scanning, compared without regard to case.</summary>
    /// <remarks>Entries may include the leading dot (<c>.cshtml</c>) or omit it (<c>cshtml</c>). Defaults to <c>.cshtml</c>, <c>.html</c>, <c>.js</c>, <c>.mjs</c>, <c>.ts</c>, and <c>.cs</c>. An empty list discovers no files.</remarks>
    public List<string> Extensions { get; set; } = [".cshtml", ".html", ".js", ".mjs", ".ts", ".cs"];
    /// <summary>Directory names whose entire subtrees are excluded from scanning.</summary>
    /// <remarks>Each child directory is compared by its final name, not its full path, without regard to case. A configured root itself is always scanned. Defaults to <c>bin</c>, <c>obj</c>, <c>node_modules</c>, <c>.git</c>, <c>lib</c>, and <c>bundles</c>. Replace the list to change these defaults; an empty list disables directory-name exclusions.</remarks>
    public List<string> IgnoredDirectories { get; set; } = ["bin", "obj", "node_modules", ".git", "lib", "bundles"];
}
