using System.Collections.Concurrent;
using IconTrim.Core.Models;

namespace IconTrim.Core.Services;

/// <summary>Serializes runs that target the same state file within one process.</summary>
public sealed class IconTrimExecutionGate
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Waits for the state path's gate, executes one run, and releases the gate afterward.</summary>
    /// <param name="statePath">Resolved state-file path used as the gate key.</param>
    /// <param name="run">Run to execute while holding the gate.</param>
    /// <param name="cancellationToken">Cancels waiting for the gate.</param>
    /// <returns>The run's result.</returns>
    public async Task<TrimResult> RunAsync(string statePath, Func<Task<TrimResult>> run, CancellationToken cancellationToken)
    {
        var gate = Gates.GetOrAdd(statePath, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try { return await run(); }
        finally { gate.Release(); }
    }
}
