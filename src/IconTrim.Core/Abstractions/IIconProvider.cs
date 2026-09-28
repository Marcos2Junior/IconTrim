using IconTrim.Core.Models;

namespace IconTrim.Core.Abstractions;

/// <summary>Loads available icon definitions from a provider-specific source.</summary>
public interface IIconProvider
{
    /// <summary>Provider input files that the source scanner must not treat as application references.</summary>
    /// <remarks>Paths may be absolute or relative to <see cref="IconTrim.Core.Configuration.IconTrimOptions.BasePath"/>. Providers with no scannable input files can use the default empty collection.</remarks>
    IReadOnlyCollection<string> ScanExcludedFiles => [];

    /// <summary>Loads names, Unicode mappings, source size, and a source fingerprint.</summary>
    /// <param name="cancellationToken">Cancels reading provider inputs.</param>
    /// <returns>A catalog with a nonempty source fingerprint for incremental generation.</returns>
    Task<IconCatalog> LoadAsync(CancellationToken cancellationToken = default);
}
