using IconTrim.Core.Models;

namespace IconTrim.Core.Abstractions;

/// <summary>Reads and atomically persists local incremental scan and generation state.</summary>
public interface IIconTrimStateStore
{
    /// <summary>Loads compatible state or returns null when the file is absent, corrupt, or incompatible.</summary>
    /// <param name="path">Physical JSON state path.</param>
    /// <param name="cancellationToken">Cancels state-file reading.</param>
    /// <returns>Usable state, or null when a full rebuild is needed.</returns>
    Task<IconTrimState?> LoadAsync(string path, CancellationToken cancellationToken = default);
    /// <summary>Replaces the state file after writing a complete temporary file in the same directory.</summary>
    /// <param name="path">Physical JSON state path.</param>
    /// <param name="state">State to persist after a successful generation or updated scan cache.</param>
    /// <param name="cancellationToken">Cancels writing before the replacement.</param>
    /// <returns>A task that completes when the state file has been replaced.</returns>
    Task SaveAsync(string path, IconTrimState state, CancellationToken cancellationToken = default);
}
