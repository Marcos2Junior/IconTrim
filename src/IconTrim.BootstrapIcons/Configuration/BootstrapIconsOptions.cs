namespace IconTrim.BootstrapIcons.Configuration;

/// <summary>Configures the Bootstrap Icons provider's original CSS input.</summary>
public sealed class BootstrapIconsOptions
{
    /// <summary>Input Bootstrap Icons CSS parsed to map each <c>.bi-*</c> class name to its Unicode glyph.</summary>
    /// <remarks>May be absolute or relative to <see cref="IconTrim.Core.Configuration.IconTrimOptions.BasePath"/>. The file must exist when the runner executes; an empty or missing path is an error. Its original bytes contribute to the generation fingerprint. The scanner excludes this file even if its extension is configured for scanning.</remarks>
    public string CssPath { get; set; } = "";
}
