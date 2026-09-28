namespace IconTrim.FontTools.Configuration;

/// <summary>Configures the external Python process used by the FontTools adapter.</summary>
public sealed class FontToolsOptions
{
    /// <summary>Optional executable used to run <c>-m fontTools.subset</c>.</summary>
    /// <remarks>Null or whitespace enables automatic discovery. A command on PATH or an executable path can be supplied to test only that interpreter, without searching alternatives. Discovery checks for FontTools and WOFF2 Brotli support. The selected interpreter and FontTools version participate in the generation fingerprint.</remarks>
    public string? PythonExecutable { get; set; }
}
