using IconTrim.FontTools;
using IconTrim.FontTools.Configuration;

internal static class PythonResolutionTests
{
    public static async Task RunAsync()
    {
        Check(PythonExecutableResolver.BuildCandidates(true, null).SequenceEqual(["python", "py", "python3"]),
            "Windows candidate order");
        Check(PythonExecutableResolver.BuildCandidates(false, null).SequenceEqual(["python3", "python"]),
            "Unix candidate order");

        var venv = Path.Combine(Path.GetTempPath(), "IconTrimPythonTest-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(Path.Combine(venv, "Scripts"));
            Directory.CreateDirectory(Path.Combine(venv, "bin"));
            var windowsPython = Path.Combine(venv, "Scripts", "python.exe");
            var unixPython = Path.Combine(venv, "bin", "python");
            File.WriteAllText(windowsPython, "fake");
            File.WriteAllText(unixPython, "fake");
            Check(PythonExecutableResolver.BuildCandidates(true, venv)[0] == windowsPython,
                "Windows virtual environment priority");
            Check(PythonExecutableResolver.BuildCandidates(false, venv)[0] == unixPython,
                "Unix virtual environment priority");
        }
        finally { Directory.Delete(venv, recursive: true); }

        var resolved = new ResolvedPython(Path.Combine(Path.GetTempPath(), "python-test"), "3.12.1", "4.58.0", "brotli:1.1.0");
        var fallbackProbe = new FakeProbe(command => command == "py"
            ? PythonProbeResult.Success(resolved)
            : PythonProbeResult.Failed("fontTools não instalado"));
        var fallback = new PythonExecutableResolver(new FontToolsOptions(), fallbackProbe, true, null);
        Check(await fallback.ResolveAsync(CancellationToken.None) == resolved, "fallback selects usable Python");
        Check(fallbackProbe.Calls.SequenceEqual(["python", "py"]), "unusable candidate is skipped");
        await fallback.ResolveAsync(CancellationToken.None);
        Check(fallbackProbe.Calls.Count == 2, "resolution cached per runner");

        var explicitProbe = new FakeProbe(_ => PythonProbeResult.Success(resolved));
        var options = new FontToolsOptions { PythonExecutable = "custom-python" };
        var explicitResolver = new PythonExecutableResolver(options, explicitProbe, true, null);
        var generator = new FontToolsSubsetGenerator(new FontToolsProcessRunner(explicitResolver), options);
        var fingerprint = await generator.GetConfigurationFingerprintAsync();
        Check(explicitProbe.Calls.SequenceEqual(["custom-python"]), "explicit executable bypasses search");
        Check(fingerprint.Contains(resolved.ExecutablePath) && fingerprint.Contains(resolved.PythonVersion) &&
            fingerprint.Contains(resolved.FontToolsVersion) && fingerprint.Contains(resolved.BrotliIdentity),
            "resolved environment is fingerprinted");

        var failedProbe = new FakeProbe(_ => PythonProbeResult.Failed("module missing"));
        var failedResolver = new PythonExecutableResolver(new FontToolsOptions(), failedProbe, false, null);
        try { await failedResolver.ResolveAsync(CancellationToken.None); throw new Exception("missing Python accepted"); }
        catch (InvalidOperationException ex)
        {
            Check(ex.Message.Contains("python3") && ex.Message.Contains("python") &&
                ex.Message.Contains("fonttools brotli"), "actionable discovery error");
        }
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var cancelledProbe = new FakeProbe(_ => PythonProbeResult.Success(resolved));
        var cancelledResolver = new PythonExecutableResolver(new FontToolsOptions(), cancelledProbe, true, null);
        try { await cancelledResolver.ResolveAsync(cancelled.Token); throw new Exception("cancelled discovery accepted"); }
        catch (OperationCanceledException) { }
        Check(cancelledProbe.Calls.Count == 0, "cancellation before probing");
    }

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception($"Failed: {name}");
    }

    private sealed class FakeProbe(Func<string, PythonProbeResult> response) : IPythonProbe
    {
        public List<string> Calls { get; } = [];

        public Task<PythonProbeResult> ProbeAsync(string command, CancellationToken cancellationToken)
        {
            Calls.Add(command);
            return Task.FromResult(response(command));
        }
    }
}
