namespace IconTrim.Core.Models;

/// <summary>Icon definitions and source metadata loaded by a provider.</summary>
/// <param name="Icons">Definitions indexed by icon name.</param>
/// <param name="OriginalCssBytes">Original provider CSS input size used for reduction reporting.</param>
public sealed record IconCatalog(IReadOnlyDictionary<string, IconDefinition> Icons, long OriginalCssBytes)
{
    /// <summary>Hash of the provider's original input bytes used to invalidate generated assets when that input changes.</summary>
    /// <remarks>The runner requires a nonempty value. The Bootstrap Icons provider supplies SHA-256 of its original CSS bytes. Custom providers should supply a deterministic fingerprint of all provider inputs that affect generation.</remarks>
    public string SourceFingerprint { get; init; } = "";
}
