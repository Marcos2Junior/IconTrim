using System.Text.Json;
using IconTrim.Core.Abstractions;
using IconTrim.Core.Models;

namespace IconTrim.Core.Services;

/// <summary>Stores versioned incremental state in a readable JSON file.</summary>
public sealed class JsonIconTrimStateStore : IIconTrimStateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>Reads usable state or returns null when it is absent, malformed, or from another format version.</summary>
    /// <param name="path">Physical state-file path.</param>
    /// <param name="cancellationToken">Cancels asynchronous reading.</param>
    /// <returns>Compatible state, or null to request a rebuild.</returns>
    public async Task<IconTrimState?> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path)) return null;
        try
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
                4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
            var state = await JsonSerializer.DeserializeAsync<IconTrimState>(stream, JsonOptions, cancellationToken);
            if (state is null || state.Version != IconTrimState.CurrentVersion || state.Files is null ||
                (state.GenerationFingerprint is not null &&
                 (state.GenerationFingerprint.Length != 64 || !state.GenerationFingerprint.All(Uri.IsHexDigit) ||
                  string.IsNullOrWhiteSpace(state.LastGeneratedFontPath) ||
                  string.IsNullOrWhiteSpace(state.LastGeneratedCssPath) ||
                  !Path.IsPathFullyQualified(state.LastGeneratedFontPath) ||
                  !Path.IsPathFullyQualified(state.LastGeneratedCssPath))) ||
                (state.GenerationFingerprint is null &&
                 (state.LastGeneratedFontPath is not null || state.LastGeneratedCssPath is not null)) ||
                state.Files.Any(x => x.Value is null || string.IsNullOrWhiteSpace(x.Key) ||
                    x.Value.RelativePath != x.Key || x.Value.Length < 0 || x.Value.LastWriteUtcTicks < 0 ||
                    x.Value.References is null || x.Value.References.Any(r => r is null ||
                        string.IsNullOrWhiteSpace(r.IconName) || r.Line < 1 || r.Column < 1)))
                return null;
            return state;
        }
        catch (JsonException) { return null; }
        catch (NotSupportedException) { return null; }
        catch (FileNotFoundException) { return null; }
    }

    /// <summary>Writes JSON to a temporary sibling file, flushes it, then replaces the state path.</summary>
    /// <param name="path">Physical state-file path; its parent directory is created if needed.</param>
    /// <param name="state">Completed state to persist.</param>
    /// <param name="cancellationToken">Cancels writing before replacement.</param>
    /// <returns>A task completed after the replacement.</returns>
    public async Task SaveAsync(string path, IconTrimState state, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                4096, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(stream, state, JsonOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
                stream.Flush(flushToDisk: true);
            }
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }
}
