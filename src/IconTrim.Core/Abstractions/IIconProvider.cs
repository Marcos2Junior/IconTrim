using IconTrim.Core.Models;

namespace IconTrim.Core.Abstractions;

/// <summary>Loads available icon definitions from a provider-specific source.</summary>
public interface IIconProvider
{
    /// <summary>Loads names, Unicode mappings, source size, and a source fingerprint.</summary>
    /// <param name="cancellationToken">Cancels reading provider inputs.</param>
    /// <returns>A catalog with a nonempty source fingerprint for incremental generation.</returns>
    Task<IconCatalog> LoadAsync(CancellationToken cancellationToken = default);
}
