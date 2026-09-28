using IconTrim.Core.Models;

namespace IconTrim.Core.Abstractions;

/// <summary>Scans configured source roots without using a previous scan cache.</summary>
public interface IIconScanner
{
    /// <summary>Finds current icon references and their file, line, and column.</summary>
    /// <param name="cancellationToken">Stops traversal between directories, files, and matches.</param>
    /// <returns>References and counts for the current source files.</returns>
    ScanResult Scan(CancellationToken cancellationToken = default);
}
