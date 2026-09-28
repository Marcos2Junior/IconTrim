using IconTrim.Core.Abstractions;
using IconTrim.Core.DependencyInjection;
using IconTrim.FontTools.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IconTrim.FontTools.DependencyInjection;

/// <summary>Registers the Python/FontTools font-subsetting adapter.</summary>
public static class FontToolsBuilderExtensions
{
    /// <summary>Adds a subset generator that invokes Python's <c>fontTools.subset</c> module.</summary>
    /// <param name="builder">Builder returned by <c>AddIconTrim</c> or a provider registration.</param>
    /// <param name="configure">Optional executable configuration; omitting it discovers a usable Python interpreter.</param>
    /// <returns>The same builder for further registration calls.</returns>
    /// <remarks>On the first run, discovery checks an active virtual environment, then common commands in operating-system-specific order. An explicit executable bypasses candidate search. Python with FontTools and WOFF2 Brotli support must be available. Registration starts no process.</remarks>
    public static IconTrimBuilder AddFontTools(this IconTrimBuilder builder, Action<FontToolsOptions>? configure = null)
    {
        var options = new FontToolsOptions();
        configure?.Invoke(options);
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<FontToolsProcessRunner>();
        builder.Services.AddSingleton<IFontSubsetGenerator, FontToolsSubsetGenerator>();
        return builder;
    }
}
