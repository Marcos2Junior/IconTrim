using IconTrim.Core.Models;

namespace IconTrim.Core.Abstractions;

/// <summary>Runs icon discovery, validation, and asset generation explicitly.</summary>
/// <remarks>This is the low-level manual API. Consumers using IconTrim.Hosting normally register <c>AddIconTrimOnStartup</c> instead of calling it themselves.</remarks>
public interface IIconTrimRunner
{
    /// <summary>Updates the scan cache and generates assets only when the generation fingerprint or outputs require it.</summary>
    /// <param name="cancellationToken">Cancels state I/O, scan checks, hashing, and generation where supported.</param>
    /// <returns>Counts, output paths, sizes, and whether the assets were already up to date.</returns>
    /// <exception cref="IconValidationException">A referenced icon is absent from the provider and unknown icons are configured to throw.</exception>
    Task<TrimResult> RunAsync(CancellationToken cancellationToken = default);
}
