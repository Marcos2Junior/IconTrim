namespace IconTrim.Core.Configuration;

/// <summary>Resolves consumer-supplied physical paths using a required absolute base directory.</summary>
public static class ConfiguredPath
{
    /// <summary>Returns a full path, combining relative input with the configured base directory.</summary>
    /// <param name="basePath">Required absolute base directory.</param>
    /// <param name="path">Absolute path used as supplied, or relative path resolved against <paramref name="basePath"/>.</param>
    /// <returns>Normalized full physical path.</returns>
    /// <exception cref="ArgumentException">The base is not absolute or the path is empty.</exception>
    public static string Resolve(string basePath, string path)
    {
        if (string.IsNullOrWhiteSpace(basePath) || !Path.IsPathFullyQualified(basePath))
            throw new ArgumentException("BasePath deve ser um caminho absoluto.", nameof(basePath));
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Um caminho configurado está vazio.", nameof(path));
        return Path.GetFullPath(Path.IsPathFullyQualified(path) ? path : Path.Combine(basePath, path));
    }
}
