using IconTrim.Core.Abstractions;

namespace IconTrim.Core.Generation;

/// <summary>Removes old generated WOFF2 versions while retaining the current hashed file.</summary>
public sealed class GeneratedAssetCleaner : IGeneratedAssetCleaner
{
    /// <summary>Deletes old files named with the configured prefix and a 12-character hexadecimal hash.</summary>
    /// <param name="fontDirectory">Physical directory containing generated fonts.</param>
    /// <param name="assetPrefix">Prefix of generated WOFF2 filenames.</param>
    /// <param name="currentFileName">Current font filename, which is never deleted.</param>
    /// <returns>Number of deleted files.</returns>
    public int Clean(string fontDirectory, string assetPrefix, string currentFileName)
    {
        var removed = 0;
        foreach (var file in Directory.EnumerateFiles(fontDirectory, "*.woff2"))
        {
            var name = Path.GetFileName(file);
            if (!name.StartsWith(assetPrefix + ".", StringComparison.OrdinalIgnoreCase) ||
                !name.EndsWith(".woff2", StringComparison.OrdinalIgnoreCase) ||
                name.Equals(currentFileName, StringComparison.OrdinalIgnoreCase)) continue;
            var hash = name[(assetPrefix.Length + 1)..^6];
            if (hash.Length != 12 || !hash.All(Uri.IsHexDigit)) continue;
            File.Delete(file);
            removed++;
        }
        return removed;
    }
}
