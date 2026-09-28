using IconTrim.Core.Models;

namespace IconTrim.Core.Abstractions;

/// <summary>Creates a reduced font containing the selected icon glyphs.</summary>
public interface IFontSubsetGenerator
{
    /// <summary>Stable identity of this generator and configuration that can change its output.</summary>
    /// <remarks>The default uses the implementation's assembly-qualified type name. Adapters with output-affecting options should override this value so changes invalidate the generation fingerprint.</remarks>
    string ConfigurationFingerprint => GetType().AssemblyQualifiedName ?? GetType().FullName ?? "";

    /// <summary>Resolves the generator identity and any external tool version before the generation fingerprint is calculated.</summary>
    /// <param name="cancellationToken">Cancels external tool discovery when the adapter performs it.</param>
    /// <returns>A stable value that changes when the implementation or output-affecting tool configuration changes.</returns>
    /// <remarks>The default preserves existing implementations by returning <see cref="ConfigurationFingerprint"/> without external discovery.</remarks>
    Task<string> GetConfigurationFingerprintAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(ConfigurationFingerprint);

    /// <summary>Writes a subset font to the requested temporary output path.</summary>
    /// <param name="sourceFont">Existing original font file.</param>
    /// <param name="outputFont">Temporary physical destination; the runner later hashes and renames it.</param>
    /// <param name="icons">Definitions whose Unicode glyphs must be included.</param>
    /// <param name="cancellationToken">Cancels the adapter's work where supported.</param>
    /// <returns>A task that finishes after the output file is written.</returns>
    Task GenerateAsync(string sourceFont, string outputFont, IReadOnlyCollection<IconDefinition> icons, CancellationToken cancellationToken = default);
}
