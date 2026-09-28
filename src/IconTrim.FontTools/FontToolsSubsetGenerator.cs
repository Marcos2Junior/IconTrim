using IconTrim.Core.Abstractions;
using IconTrim.Core.Models;
using IconTrim.FontTools.Configuration;

namespace IconTrim.FontTools;

/// <summary>Adapts IconTrim's subset request to the external FontTools process.</summary>
/// <param name="runner">Starts and monitors the Python process.</param>
/// <param name="options">Explicit Python command or automatic selection mode included in the generation fingerprint.</param>
public sealed class FontToolsSubsetGenerator(FontToolsProcessRunner runner, FontToolsOptions options) : IFontSubsetGenerator
{
    /// <summary>Identifies the adapter and the configured Python selection mode.</summary>
    public string ConfigurationFingerprint =>
        $"{GetType().AssemblyQualifiedName}|{options.PythonExecutable?.Trim() ?? "auto"}";

    /// <summary>Includes the selected interpreter path and Python, FontTools, and Brotli versions in the incremental generation fingerprint.</summary>
    /// <param name="cancellationToken">Cancels interpreter discovery.</param>
    /// <returns>Identity of the adapter and the resolved Python/FontTools installation.</returns>
    public async Task<string> GetConfigurationFingerprintAsync(CancellationToken cancellationToken = default)
    {
        var python = await runner.ResolvePythonAsync(cancellationToken);
        return $"{ConfigurationFingerprint}|{python.ExecutablePath}|{python.PythonVersion}|{python.FontToolsVersion}|{python.BrotliIdentity}";
    }

    /// <summary>Invokes FontTools to write a WOFF2 containing the selected Unicode code points.</summary>
    /// <param name="sourceFont">Existing original WOFF2 input.</param>
    /// <param name="outputFont">Temporary WOFF2 destination.</param>
    /// <param name="icons">Icons whose Unicode code points are retained.</param>
    /// <param name="cancellationToken">Cancels process execution and terminates the process when cancellation is observed.</param>
    /// <returns>A task completed after a successful process exit and output-file check.</returns>
    public async Task GenerateAsync(string sourceFont, string outputFont, IReadOnlyCollection<IconDefinition> icons, CancellationToken cancellationToken = default)
    {
        if (File.Exists(outputFont)) File.Delete(outputFont);
        var unicodes = string.Join(",", icons.Select(x => $"U+{x.UnicodeHex}"));
        await runner.RunAsync(sourceFont, outputFont, unicodes, cancellationToken);
    }
}
