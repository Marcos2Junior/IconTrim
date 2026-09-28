using IconTrim.Core.Abstractions;
using IconTrim.Core.Configuration;
using IconTrim.Core.Generation;
using IconTrim.Core.Scanning;
using IconTrim.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace IconTrim.Core.DependencyInjection;

/// <summary>Exposes the service collection to provider and subset-adapter registration extensions.</summary>
/// <remarks>Returned by <see cref="IconTrimServiceCollectionExtensions.AddIconTrim"/> so calls such as <c>AddBootstrapIcons(...).AddFontTools(...)</c> can be chained. It does not execute the runner.</remarks>
public sealed class IconTrimBuilder(IServiceCollection services)
{
    /// <summary>Service collection receiving the IconTrim provider and adapter registrations.</summary>
    public IServiceCollection Services { get; } = services;
}

/// <summary>Registers the provider-independent IconTrim services.</summary>
public static class IconTrimServiceCollectionExtensions
{
    /// <summary>Registers scanning, state storage, fingerprinting, cleanup, and <see cref="IIconTrimRunner"/>.</summary>
    /// <param name="services">The consumer's service collection.</param>
    /// <param name="configure">Sets the base path, scan roots, source font, and output paths for this consumer.</param>
    /// <returns>A builder for chaining provider and font-adapter registrations.</returns>
    /// <remarks>Registration only creates service descriptors and options; no files are scanned or generated until the runner executes. Register a provider and a subset generator separately. For automatic Generic Host startup execution, add IconTrim.Hosting.</remarks>
    public static IconTrimBuilder AddIconTrim(this IServiceCollection services, Action<IconTrimOptions> configure)
    {
        var options = new IconTrimOptions();
        configure(options);
        services.AddSingleton(options);
        services.AddSingleton<IconScanner>();
        services.AddSingleton<IIconScanner>(sp => sp.GetRequiredService<IconScanner>());
        services.AddSingleton<IIncrementalIconScanner>(sp => sp.GetRequiredService<IconScanner>());
        services.AddSingleton<IIconTrimStateStore, JsonIconTrimStateStore>();
        services.AddSingleton<GenerationFingerprintCalculator>();
        services.AddSingleton<IconTrimExecutionGate>();
        services.AddSingleton<IGeneratedAssetCleaner, GeneratedAssetCleaner>();
        services.AddTransient<IIconTrimRunner, IconTrimRunner>();
        return new IconTrimBuilder(services);
    }
}
