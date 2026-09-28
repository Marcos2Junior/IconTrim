using System.Text.RegularExpressions;
using IconTrim.Core.Abstractions;
using IconTrim.Core.Configuration;
using IconTrim.Core.Models;

namespace IconTrim.Core.Scanning;

/// <summary>Finds icon references in configured source files, excluding generated and provider input files.</summary>
/// <param name="options">Consumer scan roots, extensions, ignored directory names, and icon prefix.</param>
/// <param name="provider">Optional icon provider whose input files must be excluded from scanning.</param>
public sealed class IconScanner(IconTrimOptions options, IIconProvider? provider = null) : IIconScanner, IIncrementalIconScanner
{
    /// <summary>Scans every eligible file without loading previous file state.</summary>
    /// <param name="cancellationToken">Stops traversal between directories, files, and matches.</param>
    /// <returns>Current references with file, line, and column.</returns>
    public ScanResult Scan(CancellationToken cancellationToken = default) => Scan(null, cancellationToken).Scan;

    /// <summary>Reuses references from files whose relative path, length, and UTC modification time match cached state.</summary>
    /// <param name="previousState">Previous scan entries, or null for a full scan.</param>
    /// <param name="cancellationToken">Stops traversal between directories, files, and matches.</param>
    /// <returns>Current references and replacement per-file state; deleted files are omitted.</returns>
    public IncrementalScanResult Scan(IconTrimState? previousState, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.Scan.IconPrefix))
            throw new ArgumentException("Configure Scan.IconPrefix.");
        var iconRegex = new Regex(@"(?<![a-zA-Z0-9_-])" + Regex.Escape(options.Scan.IconPrefix) + @"[a-zA-Z0-9-]+", RegexOptions.Compiled);
        var extensions = options.Scan.Extensions.Select(x => x.StartsWith('.') ? x : "." + x)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var ignored = options.Scan.IgnoredDirectories.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var excludedFiles = new HashSet<string>(OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(options.Output.CssPath))
            excludedFiles.Add(ConfiguredPath.Resolve(options.BasePath, options.Output.CssPath));
        if (provider is not null)
            foreach (var path in provider.ScanExcludedFiles)
                if (!string.IsNullOrWhiteSpace(path))
                    excludedFiles.Add(ConfiguredPath.Resolve(options.BasePath, path));
        var references = new List<IconReference>();
        var filesScanned = 0;
        var filesReused = 0;
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var currentFiles = new Dictionary<string, ScannedFileState>(StringComparer.OrdinalIgnoreCase);
        var previousFiles = previousState?.Files is null
            ? new Dictionary<string, ScannedFileState>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, ScannedFileState>(previousState.Files, StringComparer.OrdinalIgnoreCase);

        foreach (var configuredRoot in options.Scan.Roots)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var root = ConfiguredPath.Resolve(options.BasePath, configuredRoot);
            if (!Directory.Exists(root)) throw new DirectoryNotFoundException($"Diretório de scan não encontrado: {root}");
            var pending = new Stack<string>();
            pending.Push(root);
            while (pending.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var directory = pending.Pop();
                if (!visited.Add(directory)) continue;
                foreach (var child in Directory.EnumerateDirectories(directory).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                    if (!ignored.Contains(Path.GetFileName(child))) pending.Push(child);
                foreach (var file in Directory.EnumerateFiles(directory).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!extensions.Contains(Path.GetExtension(file))) continue;
                    if (excludedFiles.Contains(Path.GetFullPath(file))) continue;
                    var metadata = new FileInfo(file);
                    var relativePath = Path.GetRelativePath(options.BasePath, file).Replace('\\', '/');
                    if (previousFiles.TryGetValue(relativePath, out var cached) &&
                        cached.Length == metadata.Length && cached.LastWriteUtcTicks == metadata.LastWriteTimeUtc.Ticks)
                    {
                        currentFiles[relativePath] = cached;
                        references.AddRange(cached.References.Select(x => new IconReference(x.IconName, file, x.Line, x.Column)));
                        filesReused++;
                        continue;
                    }
                    filesScanned++;
                    var content = File.ReadAllText(file);
                    var lineStarts = new List<int> { 0 };
                    var fileReferences = new List<CachedIconReference>();
                    for (var i = 0; i < content.Length; i++)
                        if (content[i] == '\n') lineStarts.Add(i + 1);
                    foreach (Match match in iconRegex.Matches(content))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var lineIndex = lineStarts.BinarySearch(match.Index);
                        if (lineIndex < 0) lineIndex = ~lineIndex - 1;
                        var reference = new CachedIconReference(match.Value, lineIndex + 1, match.Index - lineStarts[lineIndex] + 1);
                        fileReferences.Add(reference);
                        references.Add(new IconReference(reference.IconName, file, reference.Line, reference.Column));
                    }
                    currentFiles[relativePath] = new ScannedFileState
                    {
                        RelativePath = relativePath,
                        Length = metadata.Length,
                        LastWriteUtcTicks = metadata.LastWriteTimeUtc.Ticks,
                        References = fileReferences
                    };
                }
            }
        }
        var removed = previousFiles.Keys.Count(x => !currentFiles.ContainsKey(x));
        var scan = new ScanResult(filesScanned, references)
        {
            FilesDiscovered = currentFiles.Count,
            FilesReusedFromCache = filesReused,
            FilesRemovedFromCache = removed
        };
        return new IncrementalScanResult(scan, currentFiles);
    }
}
