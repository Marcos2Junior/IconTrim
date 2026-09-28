using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using IconTrim.Core.Abstractions;
using IconTrim.Core.Configuration;
using IconTrim.Core.Models;

namespace IconTrim.Core.Services;

/// <summary>Coordinates incremental scanning, validation, fingerprinting, and asset generation.</summary>
/// <param name="options">Consumer paths and behavior.</param>
/// <param name="provider">Source of available icon definitions.</param>
/// <param name="scanner">Incremental source scanner.</param>
/// <param name="subsetGenerator">Font adapter invoked only when regeneration is required.</param>
/// <param name="cssGenerator">Provider-specific CSS generator.</param>
/// <param name="cleaner">Removes superseded versioned fonts after generation.</param>
/// <param name="stateStore">Persists incremental state.</param>
/// <param name="fingerprintCalculator">Checks whether generation inputs changed.</param>
/// <param name="executionGate">Serializes runs targeting the same state path within this process.</param>
public sealed class IconTrimRunner(
    IconTrimOptions options,
    IIconProvider provider,
    IIncrementalIconScanner scanner,
    IFontSubsetGenerator subsetGenerator,
    ICssGenerator cssGenerator,
    IGeneratedAssetCleaner cleaner,
    IIconTrimStateStore stateStore,
    GenerationFingerprintCalculator fingerprintCalculator,
    IconTrimExecutionGate executionGate) : IIconTrimRunner
{
    /// <summary>Runs a scan and regenerates assets only when inputs changed or recorded outputs are missing.</summary>
    /// <param name="cancellationToken">Cancels asynchronous I/O, hashing, and font generation, and stops scanner traversal between files.</param>
    /// <returns>Current asset paths, metrics, and an up-to-date flag.</returns>
    public Task<TrimResult> RunAsync(CancellationToken cancellationToken = default)
    {
        ValidateOptions();
        var statePath = ConfiguredPath.Resolve(options.BasePath, options.StatePath);
        return executionGate.RunAsync(statePath, () => RunOnceAsync(statePath, cancellationToken), cancellationToken);
    }

    private async Task<TrimResult> RunOnceAsync(string statePath, CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();
        var previousState = await stateStore.LoadAsync(statePath, cancellationToken);
        var incremental = scanner.Scan(previousState, cancellationToken);
        var scan = incremental.Scan;
        var safelist = LoadSafelist(cancellationToken);
        var catalog = await provider.LoadAsync(cancellationToken);
        var requested = scan.References.Select(x => x.IconName).Concat(safelist)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unknown = requested.Where(x => !catalog.Icons.ContainsKey(x))
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
        if (unknown.Length > 0 && options.UnknownIconBehavior == UnknownIconBehavior.Throw)
            throw new IconValidationException(unknown, scan.References
                .Where(x => unknown.Contains(x.IconName, StringComparer.OrdinalIgnoreCase)).ToArray());
        var selected = requested.Where(catalog.Icons.ContainsKey).Select(x => catalog.Icons[x])
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        if (selected.Length == 0) throw new InvalidOperationException("Nenhum ícone foi encontrado.");

        var sourceFont = ConfiguredPath.Resolve(options.BasePath, options.Font.SourceFontPath);
        if (!File.Exists(sourceFont)) throw new FileNotFoundException($"Arquivo não encontrado: {sourceFont}", sourceFont);
        var cssPath = ConfiguredPath.Resolve(options.BasePath, options.Output.CssPath);
        var fontDirectory = ConfiguredPath.Resolve(options.BasePath, options.Output.FontDirectory);
        var fingerprint = await fingerprintCalculator.CalculateAsync(
            catalog, selected, safelist, sourceFont, cssPath, fontDirectory, cancellationToken);

        var isCurrent = previousState?.GenerationFingerprint == fingerprint &&
            string.Equals(previousState.LastGeneratedCssPath, cssPath, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(previousState.LastGeneratedFontPath) &&
            File.Exists(cssPath) && File.Exists(previousState.LastGeneratedFontPath);
        if (isCurrent)
        {
            if (scan.FilesScanned > 0 || scan.FilesRemovedFromCache > 0)
            {
                previousState!.Files = incremental.Files;
                await stateStore.SaveAsync(statePath, previousState, cancellationToken);
            }
            timer.Stop();
            return CreateResult(scan, selected.Length, safelist.Count, unknown, catalog.OriginalCssBytes,
                sourceFont, cssPath, previousState!.LastGeneratedFontPath!, 0, timer.Elapsed, skipped: true);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(cssPath)!);
        Directory.CreateDirectory(fontDirectory);
        var tempFont = Path.Combine(fontDirectory, $"{options.Output.AssetPrefix}.{Guid.NewGuid():N}.tmp.woff2");
        try
        {
            await subsetGenerator.GenerateAsync(sourceFont, tempFont, selected, cancellationToken);
            await using var fontStream = File.OpenRead(tempFont);
            var fontHash = Convert.ToHexString(await SHA256.HashDataAsync(fontStream, cancellationToken))
                .ToLowerInvariant()[..12];
            fontStream.Close();
            var fontName = $"{options.Output.AssetPrefix}.{fontHash}.woff2";
            var finalFont = Path.Combine(fontDirectory, fontName);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(tempFont, finalFont, overwrite: true);
            var fontUrl = options.Output.FontUrlPrefix.TrimEnd('/') + "/" + fontName;
            var css = cssGenerator.Generate(selected, fontUrl);
            await File.WriteAllTextAsync(cssPath, css, new UTF8Encoding(false), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            var removed = cleaner.Clean(fontDirectory, options.Output.AssetPrefix, fontName);
            if (!File.Exists(finalFont) || !File.Exists(cssPath))
                throw new IOException("Os assets gerados não estão presentes após a geração.");
            var newState = new IconTrimState
            {
                Files = incremental.Files,
                GenerationFingerprint = fingerprint,
                LastGeneratedFontPath = finalFont,
                LastGeneratedCssPath = cssPath
            };
            await stateStore.SaveAsync(statePath, newState, cancellationToken);
            timer.Stop();
            return CreateResult(scan, selected.Length, safelist.Count, unknown, catalog.OriginalCssBytes,
                sourceFont, cssPath, finalFont, removed, timer.Elapsed, skipped: false);
        }
        finally
        {
            if (File.Exists(tempFont)) File.Delete(tempFont);
        }
    }

    private static TrimResult CreateResult(
        ScanResult scan, int generatedIcons, int safelistedIcons, IReadOnlyList<string> unknown,
        long originalCssBytes, string sourceFont, string cssPath, string generatedFont,
        int removedAssets, TimeSpan duration, bool skipped) =>
        new(scan.FilesScanned, scan.TotalReferences, scan.UniqueIcons, generatedIcons,
            safelistedIcons, unknown, originalCssBytes, new FileInfo(cssPath).Length,
            new FileInfo(sourceFont).Length, new FileInfo(generatedFont).Length,
            removedAssets, duration, skipped, generatedFont, cssPath)
        {
            FilesDiscovered = scan.FilesDiscovered,
            FilesReusedFromCache = scan.FilesReusedFromCache,
            FilesRemovedFromCache = scan.FilesRemovedFromCache
        };

    private HashSet<string> LoadSafelist(CancellationToken cancellationToken)
    {
        var result = options.Safelist.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(options.SafelistFile)) return result;
        var file = ConfiguredPath.Resolve(options.BasePath, options.SafelistFile);
        if (!File.Exists(file)) return result;
        foreach (var line in File.ReadLines(file).Select(x => x.Trim()))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (line.Length > 0 && !line.StartsWith('#')) result.Add(line);
        }
        return result;
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(options.BasePath) || !Path.IsPathFullyQualified(options.BasePath))
            throw new ArgumentException("BasePath deve ser um caminho absoluto.");
        if (options.Scan.Roots.Count == 0) throw new ArgumentException("Configure ao menos uma raiz de scan.");
        var prefix = options.Output.AssetPrefix;
        if (string.IsNullOrWhiteSpace(prefix) || prefix.Contains('/') || prefix.Contains('\\') || prefix is "." or "..")
            throw new ArgumentException("AssetPrefix deve ser um nome de arquivo válido.");
        if (string.IsNullOrWhiteSpace(options.Output.FontUrlPrefix))
            throw new ArgumentException("FontUrlPrefix deve ser informado.");
    }
}
