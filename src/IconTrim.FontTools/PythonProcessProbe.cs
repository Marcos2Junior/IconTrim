using System.Diagnostics;
using System.Text.Json;

namespace IconTrim.FontTools;

internal sealed class PythonProcessProbe : IPythonProbe
{
    private const string OutputMarker = "ICONTRIM_PROBE:";
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(8);
    private const string ProbeScript = """
        import json
        import sys
        from importlib import metadata
        import fontTools.subset
        from fontTools.ttLib import woff2
        if not woff2.haveBrotli:
            raise RuntimeError("WOFF2 Brotli support is unavailable")
        brotli_name = woff2.brotli.__name__
        try:
            brotli_version = metadata.version(brotli_name)
        except metadata.PackageNotFoundError:
            brotli_version = getattr(woff2.brotli, "__version__", "unknown")
        print("ICONTRIM_PROBE:" + json.dumps({
            "Executable": sys.executable,
            "PythonVersion": sys.version.split()[0],
            "FontToolsVersion": metadata.version("fonttools"),
            "BrotliIdentity": brotli_name + ":" + str(brotli_version)
        }))
        """;

    public async Task<PythonProbeResult> ProbeAsync(string command, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = command,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add(ProbeScript);
        // The Python install manager can otherwise install a missing runtime during discovery.
        startInfo.Environment["PYTHON_MANAGER_AUTOMATIC_INSTALL"] = "0";
        startInfo.Environment.Remove("PYLAUNCHER_ALLOW_INSTALL");
        startInfo.Environment.Remove("PYLAUNCHER_ALWAYS_INSTALL");
        using var process = new Process { StartInfo = startInfo };
        try
        {
            if (!process.Start()) return PythonProbeResult.Failed("o processo não iniciou");
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException or InvalidOperationException)
        {
            return PythonProbeResult.Failed(ex.Message);
        }
        process.StandardInput.Close();
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(ProbeTimeout);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
            await process.WaitForExitAsync(CancellationToken.None);
            cancellationToken.ThrowIfCancellationRequested();
            return PythonProbeResult.Failed($"tempo limite de {ProbeTimeout.TotalSeconds:0} s");
        }
        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        if (process.ExitCode != 0)
        {
            var reason = stderr.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).LastOrDefault()
                ?? stdout.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).LastOrDefault()
                ?? $"código de saída {process.ExitCode}";
            return PythonProbeResult.Failed(reason);
        }
        var markerLine = stdout.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault(line => line.StartsWith(OutputMarker, StringComparison.Ordinal));
        if (markerLine is null) return PythonProbeResult.Failed("o comando não retornou dados de diagnóstico Python");
        try
        {
            var payload = JsonSerializer.Deserialize<ProbePayload>(markerLine[OutputMarker.Length..]);
            if (payload is null || string.IsNullOrWhiteSpace(payload.Executable) ||
                !Path.IsPathFullyQualified(payload.Executable) || !File.Exists(payload.Executable) ||
                string.IsNullOrWhiteSpace(payload.PythonVersion) ||
                string.IsNullOrWhiteSpace(payload.FontToolsVersion) ||
                string.IsNullOrWhiteSpace(payload.BrotliIdentity))
                return PythonProbeResult.Failed("o interpretador retornou identidade incompleta");
            return PythonProbeResult.Success(new ResolvedPython(
                payload.Executable, payload.PythonVersion, payload.FontToolsVersion, payload.BrotliIdentity));
        }
        catch (JsonException)
        {
            return PythonProbeResult.Failed("o interpretador retornou diagnóstico inválido");
        }
    }

    private sealed record ProbePayload(
        string Executable, string PythonVersion, string FontToolsVersion, string BrotliIdentity);
}
