using IconTrim.Core.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IconTrim.Hosting;

/// <summary>Runs IconTrim once during Generic Host startup and waits for the assets to be ready.</summary>
/// <param name="runner">Shared IconTrim engine used by CLI and Hosting.</param>
/// <param name="logger">Writes concise startup or up-to-date metrics.</param>
/// <remarks>Failures propagate from <see cref="StartAsync"/> and prevent normal startup. There is no background loop; the consumer controls in which environments this service is registered.</remarks>
public sealed class IconTrimHostedService(IIconTrimRunner runner, ILogger<IconTrimHostedService> logger) : IHostedService
{
    private readonly object _startLock = new();
    private Task? _startTask;

    /// <summary>Awaits one IconTrim run before the host completes startup.</summary>
    /// <param name="cancellationToken">Host startup cancellation token forwarded to the runner.</param>
    /// <returns>The same task on repeated calls, including any original failure.</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        lock (_startLock)
        {
            return _startTask ??= RunOnceAsync(cancellationToken);
        }
    }

    /// <summary>Completes immediately because IconTrim has no ongoing work after startup.</summary>
    /// <param name="cancellationToken">Host shutdown cancellation token; no shutdown work is required.</param>
    /// <returns>A completed task.</returns>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("IconTrim started.");
        var result = await runner.RunAsync(cancellationToken);
        if (result.Skipped)
        {
            logger.LogInformation(
                "IconTrim up to date: {GeneratedIcons} icons, {FilesScanned} files scanned, {FilesReusedFromCache} reused from cache in {DurationMs:F0} ms.",
                result.GeneratedIcons, result.FilesScanned, result.FilesReusedFromCache, result.Duration.TotalMilliseconds);
        }
        else
        {
            logger.LogInformation(
                "IconTrim completed: {GeneratedIcons} icons, {FilesScanned} files scanned, {FilesReusedFromCache} reused, {ReductionPercent:F1}% reduction in {DurationMs:F0} ms.",
                result.GeneratedIcons, result.FilesScanned, result.FilesReusedFromCache,
                result.ReductionPercent, result.Duration.TotalMilliseconds);
        }
    }
}
