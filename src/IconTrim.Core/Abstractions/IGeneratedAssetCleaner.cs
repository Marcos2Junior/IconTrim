namespace IconTrim.Core.Abstractions;

/// <summary>Removes superseded versioned font assets after successful generation.</summary>
public interface IGeneratedAssetCleaner
{
    /// <summary>Deletes older generated WOFF2 files matching the configured asset prefix.</summary>
    /// <param name="fontDirectory">Physical generated-font directory.</param>
    /// <param name="assetPrefix">Configured prefix used to identify generated filenames.</param>
    /// <param name="currentFileName">Current versioned filename, which must be retained.</param>
    /// <returns>Number of old assets removed.</returns>
    int Clean(string fontDirectory, string assetPrefix, string currentFileName);
}
