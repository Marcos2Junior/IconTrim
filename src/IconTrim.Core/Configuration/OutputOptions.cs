namespace IconTrim.Core.Configuration;

/// <summary>Configures where generated assets are written and how the font is referenced in CSS.</summary>
public sealed class OutputOptions
{
    /// <summary>Physical destination file for generated CSS.</summary>
    /// <remarks>May be absolute or relative to <see cref="IconTrimOptions.BasePath"/>. An empty path is an error. Its parent directory is created when generation runs; the file need not exist beforehand and is replaced on regeneration. This is a filesystem path, not a public URL. The scanner excludes this file so previously generated icons cannot keep themselves in the next subset.</remarks>
    public string CssPath { get; set; } = "";
    /// <summary>Physical directory in which the versioned WOFF2 font is written.</summary>
    /// <remarks>May be absolute or relative to <see cref="IconTrimOptions.BasePath"/>. An empty path is an error. It is created when generation runs and need not exist beforehand. Use <see cref="FontUrlPrefix"/> separately for the URL emitted in CSS.</remarks>
    public string FontDirectory { get; set; } = "";
    /// <summary>Public URL prefix written into the generated CSS font-face rule.</summary>
    /// <remarks>This is a URL, not a filesystem path; it is not resolved against <see cref="IconTrimOptions.BasePath"/>. The generated filename is appended after one slash. It must be nonempty.</remarks>
    /// <example><c>/fonts</c> produces <c>/fonts/bootstrap-icons.abc123def456.woff2</c> for a matching asset prefix.</example>
    public string FontUrlPrefix { get; set; } = "";
    /// <summary>Filename prefix for the versioned WOFF2 asset and for identifying older generated versions to remove.</summary>
    /// <remarks>It must be nonempty and cannot contain directory separators. The generated filename has the form <c>{prefix}.{12-character content hash}.woff2</c>. This value is not a directory or URL.</remarks>
    /// <example><c>bootstrap-icons</c> can produce <c>bootstrap-icons.abc123def456.woff2</c>.</example>
    public string AssetPrefix { get; set; } = "";
}
