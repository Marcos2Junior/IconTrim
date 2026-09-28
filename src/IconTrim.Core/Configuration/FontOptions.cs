namespace IconTrim.Core.Configuration;

/// <summary>Identifies the original font used as input for subsetting.</summary>
public sealed class FontOptions
{
    /// <summary>Input WOFF2 font passed to the registered subset generator.</summary>
    /// <remarks>May be absolute or relative to <see cref="IconTrimOptions.BasePath"/>. The file must exist when the runner executes; an empty or missing path is an error. Changes to its bytes invalidate the generation fingerprint.</remarks>
    public string SourceFontPath { get; set; } = "";
}
