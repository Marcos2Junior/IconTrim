using IconTrim.BootstrapIcons.Configuration;
using IconTrim.Core.Configuration;
using IconTrim.FontTools.Configuration;

namespace IconTrim.Cli.Configuration;

public sealed class CliConfiguration
{
    public IconTrimOptions IconTrim { get; set; } = new();
    public BootstrapIconsOptions BootstrapIcons { get; set; } = new();
    public FontToolsOptions FontTools { get; set; } = new();
}
