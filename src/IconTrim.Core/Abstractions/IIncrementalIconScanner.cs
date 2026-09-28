using IconTrim.Core.Models;

namespace IconTrim.Core.Abstractions;

/// <summary>Reuses file references whose path, size, and UTC modification time have not changed.</summary>
public interface IIncrementalIconScanner
{
    /// <summary>Builds current references and a replacement set of per-file scan entries.</summary>
    /// <param name="previousState">Previously loaded state, or null to scan every eligible file.</param>
    /// <param name="cancellationToken">Stops traversal between directories, files, and matches.</param>
    /// <returns>Current references, work counters, and file entries ready to persist.</returns>
    IncrementalScanResult Scan(IconTrimState? previousState, CancellationToken cancellationToken = default);
}
