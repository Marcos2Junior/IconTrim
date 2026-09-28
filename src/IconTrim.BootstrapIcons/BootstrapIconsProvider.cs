using IconTrim.BootstrapIcons.Configuration;
using IconTrim.Core.Abstractions;
using IconTrim.Core.Configuration;
using IconTrim.Core.Models;
using System.Security.Cryptography;
using System.Text;

namespace IconTrim.BootstrapIcons;

/// <summary>Loads icon definitions and a source hash from configured Bootstrap Icons CSS.</summary>
/// <param name="coreOptions">Base directory used for relative CSS paths.</param>
/// <param name="options">Original Bootstrap Icons CSS path.</param>
/// <param name="parser">Converts CSS content rules into icon definitions.</param>
public sealed class BootstrapIconsProvider(IconTrimOptions coreOptions, BootstrapIconsOptions options, BootstrapIconsCssParser parser) : IIconProvider
{
    /// <summary>Reads the CSS input and returns Unicode mappings and SHA-256 of its original bytes.</summary>
    /// <param name="cancellationToken">Cancels reading the CSS file.</param>
    /// <returns>Catalog used for validation and generation fingerprinting.</returns>
    /// <exception cref="FileNotFoundException">The configured CSS input does not exist.</exception>
    public async Task<IconCatalog> LoadAsync(CancellationToken cancellationToken = default)
    {
        var path = ConfiguredPath.Resolve(coreOptions.BasePath, options.CssPath);
        if (!File.Exists(path)) throw new FileNotFoundException($"Arquivo não encontrado: {path}", path);
        var cssBytes = await File.ReadAllBytesAsync(path, cancellationToken);
        var css = Encoding.UTF8.GetString(cssBytes);
        return new IconCatalog(parser.Parse(css), cssBytes.LongLength)
        {
            SourceFingerprint = Convert.ToHexString(SHA256.HashData(cssBytes))
        };
    }
}
