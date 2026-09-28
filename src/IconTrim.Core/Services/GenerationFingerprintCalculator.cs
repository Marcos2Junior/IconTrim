using System.Security.Cryptography;
using System.Text.Json;
using IconTrim.Core.Abstractions;
using IconTrim.Core.Configuration;
using IconTrim.Core.Models;

namespace IconTrim.Core.Services;

/// <summary>Hashes only the inputs that can change generated CSS or subset font assets.</summary>
/// <param name="options">Output and matching options that affect generation.</param>
/// <param name="provider">Provider implementation identity.</param>
/// <param name="cssGenerator">CSS implementation identity.</param>
/// <param name="subsetGenerator">Font implementation and configuration identity.</param>
public sealed class GenerationFingerprintCalculator(
    IconTrimOptions options,
    IIconProvider provider,
    ICssGenerator cssGenerator,
    IFontSubsetGenerator subsetGenerator)
{
    private const int GenerationFormatVersion = 1;

    /// <summary>Computes a deterministic SHA-256 fingerprint without hashing application source files.</summary>
    /// <param name="catalog">Available icons and hash of the provider's original input.</param>
    /// <param name="selectedIcons">Final selected icon names and Unicode mappings.</param>
    /// <param name="effectiveSafelist">Combined file and programmatic safelist after deduplication.</param>
    /// <param name="sourceFontPath">Physical original WOFF2 path whose bytes are hashed.</param>
    /// <param name="outputCssPath">Resolved physical CSS output path.</param>
    /// <param name="outputFontDirectory">Resolved physical font output directory.</param>
    /// <param name="cancellationToken">Cancels original-font hashing.</param>
    /// <returns>Lowercase SHA-256 hexadecimal fingerprint.</returns>
    public async Task<string> CalculateAsync(
        IconCatalog catalog,
        IReadOnlyCollection<IconDefinition> selectedIcons,
        IReadOnlyCollection<string> effectiveSafelist,
        string sourceFontPath,
        string outputCssPath,
        string outputFontDirectory,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(catalog.SourceFingerprint))
            throw new InvalidOperationException("O provider deve fornecer o fingerprint de sua fonte original.");
        await using var fontStream = File.OpenRead(sourceFontPath);
        var originalFontHash = Convert.ToHexString(await SHA256.HashDataAsync(fontStream, cancellationToken));
        var subsetGeneratorFingerprint = await subsetGenerator.GetConfigurationFingerprintAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var data = JsonSerializer.SerializeToUtf8Bytes(new
        {
            GenerationFormatVersion,
            CoreVersion = typeof(IconTrimRunner).Assembly.GetName().Version?.ToString(),
            Provider = provider.GetType().AssemblyQualifiedName,
            CssGenerator = cssGenerator.GetType().AssemblyQualifiedName,
            FontSubsetGenerator = subsetGeneratorFingerprint,
            ProviderSourceHash = catalog.SourceFingerprint,
            OriginalFontHash = originalFontHash,
            SourceFontPath = sourceFontPath,
            OutputCssPath = outputCssPath,
            OutputFontDirectory = outputFontDirectory,
            options.Scan.IconPrefix,
            options.Output.AssetPrefix,
            options.Output.FontUrlPrefix,
            options.UnknownIconBehavior,
            Safelist = effectiveSafelist.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray(),
            Icons = selectedIcons.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .Select(x => new { x.Name, x.UnicodeHex }).ToArray()
        });
        return Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
    }
}
