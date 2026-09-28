using IconTrim.BootstrapIcons.Configuration;
using IconTrim.Core.Abstractions;
using IconTrim.Core.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace IconTrim.BootstrapIcons.DependencyInjection;

/// <summary>Registers the Bootstrap Icons provider and CSS generator.</summary>
public static class BootstrapIconsBuilderExtensions
{
    /// <summary>Adds Bootstrap Icons CSS parsing, icon discovery, and matching CSS generation.</summary>
    /// <param name="builder">Builder returned by <c>AddIconTrim</c>.</param>
    /// <param name="configure">Sets the path to the original Bootstrap Icons CSS file.</param>
    /// <returns>The same builder so a subset adapter can be chained.</returns>
    /// <remarks>The configured CSS is read when the runner executes, not during DI registration. The input CSS must exist at that time.</remarks>
    public static IconTrimBuilder AddBootstrapIcons(this IconTrimBuilder builder, Action<BootstrapIconsOptions> configure)
    {
        var options = new BootstrapIconsOptions();
        configure(options);
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<BootstrapIconsCssParser>();
        builder.Services.AddSingleton<IIconProvider, BootstrapIconsProvider>();
        builder.Services.AddSingleton<ICssGenerator, BootstrapIconsCssGenerator>();
        return builder;
    }
}
