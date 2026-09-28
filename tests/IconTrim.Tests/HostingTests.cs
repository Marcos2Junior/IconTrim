using IconTrim.Core.Abstractions;
using IconTrim.Core.Models;
using IconTrim.Hosting.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

internal static class HostingTests
{
    public static async Task RunAsync()
    {
        await RegistrationAndCancellationAsync();
        await HostWaitsForRunnerAsync();
        await FailureStopsHostAsync();
    }

    private static async Task RegistrationAndCancellationAsync()
    {
        var release = new TaskCompletionSource<TrimResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var runner = new FakeRunner(_ => release.Task);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IIconTrimRunner>(runner);
        services.AddIconTrimOnStartup();
        services.AddIconTrimOnStartup();
        Check(services.Count(x => x.ServiceType == typeof(IHostedService)) == 1, "idempotent registration");
        Check(runner.Calls == 0, "registration has no side effects");
        using var provider = services.BuildServiceProvider();
        var hosted = provider.GetRequiredService<IHostedService>();
        using var cancellation = new CancellationTokenSource();
        var first = hosted.StartAsync(cancellation.Token);
        var second = hosted.StartAsync(cancellation.Token);
        Check(ReferenceEquals(first, second), "single startup task");
        Check(runner.Calls == 1, "runner called once");
        Check(runner.ReceivedToken == cancellation.Token, "startup token forwarded");
        Check(!first.IsCompleted, "startup waits for runner");
        release.SetResult(Result());
        await Task.WhenAll(first, second);
        await hosted.StopAsync(CancellationToken.None);
    }

    private static async Task HostWaitsForRunnerAsync()
    {
        var release = new TaskCompletionSource<TrimResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var runner = new FakeRunner(_ => { started.SetResult(); return release.Task; });
        using var host = new HostBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton<IIconTrimRunner>(runner);
                services.AddIconTrimOnStartup();
                services.AddIconTrimOnStartup();
            }).Build();
        var startup = host.StartAsync();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Check(!startup.IsCompleted && runner.Calls == 1, "host waits for exactly one run");
        release.SetResult(Result());
        await startup.WaitAsync(TimeSpan.FromSeconds(10));
        await host.StopAsync();
    }

    private static async Task FailureStopsHostAsync()
    {
        var failure = new InvalidOperationException("subset failed");
        var runner = new FakeRunner(_ => Task.FromException<TrimResult>(failure));
        using var host = new HostBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton<IIconTrimRunner>(runner);
                services.AddIconTrimOnStartup();
            }).Build();
        try
        {
            await host.StartAsync();
            throw new Exception("host accepted IconTrim failure");
        }
        catch (InvalidOperationException ex) when (ReferenceEquals(ex, failure)) { }
        Check(runner.Calls == 1, "failure calls runner once");
    }

    private static TrimResult Result() => new(
        2, 3, 1, 1, 0, [], 100, 10, 100, 10, 0, TimeSpan.Zero,
        false, "font.woff2", "icons.css");

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception($"Failed: {name}");
    }

    private sealed class FakeRunner(Func<CancellationToken, Task<TrimResult>> run) : IIconTrimRunner
    {
        public int Calls { get; private set; }
        public CancellationToken ReceivedToken { get; private set; }

        public Task<TrimResult> RunAsync(CancellationToken cancellationToken = default)
        {
            Calls++;
            ReceivedToken = cancellationToken;
            return run(cancellationToken);
        }
    }
}
