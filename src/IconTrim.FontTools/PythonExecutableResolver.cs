using IconTrim.FontTools.Configuration;

namespace IconTrim.FontTools;

internal sealed record ResolvedPython(
    string ExecutablePath, string PythonVersion, string FontToolsVersion, string BrotliIdentity);

internal sealed record PythonProbeResult(ResolvedPython? Python, string? Failure)
{
    internal static PythonProbeResult Success(ResolvedPython python) => new(python, null);
    internal static PythonProbeResult Failed(string reason) => new(null, reason);
}

internal interface IPythonProbe
{
    Task<PythonProbeResult> ProbeAsync(string command, CancellationToken cancellationToken);
}

internal sealed class PythonExecutableResolver(
    FontToolsOptions options,
    IPythonProbe probe,
    bool isWindows,
    string? virtualEnvironmentPath)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private ResolvedPython? _resolved;

    public async Task<ResolvedPython> ResolveAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_resolved is not null) return _resolved;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_resolved is not null) return _resolved;
            var explicitCommand = options.PythonExecutable?.Trim();
            var commands = string.IsNullOrWhiteSpace(explicitCommand)
                ? BuildCandidates(isWindows, virtualEnvironmentPath)
                : [explicitCommand];
            var failures = new List<string>();
            foreach (var command in commands)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = await probe.ProbeAsync(command, cancellationToken);
                if (result.Python is not null)
                    return _resolved = result.Python;
                failures.Add($"  - {command}: {result.Failure ?? "falha desconhecida"}");
            }
            throw new InvalidOperationException(
                "Não foi encontrado um Python com FontTools e suporte a WOFF2/Brotli.\n" +
                "Comandos testados:\n" + string.Join("\n", failures) + "\n" +
                "Instale no interpretador desejado com: <python> -m pip install fonttools brotli\n" +
                "Você pode fixá-lo com AddFontTools(options => options.PythonExecutable = \"<python>\").");
        }
        finally
        {
            _gate.Release();
        }
    }

    internal static IReadOnlyList<string> BuildCandidates(bool isWindows, string? virtualEnvironmentPath)
    {
        var commands = new List<string>();
        if (!string.IsNullOrWhiteSpace(virtualEnvironmentPath))
        {
            var venvPython = Path.Combine(virtualEnvironmentPath,
                isWindows ? "Scripts" : "bin", isWindows ? "python.exe" : "python");
            if (File.Exists(venvPython)) commands.Add(venvPython);
        }
        commands.AddRange(isWindows ? ["python", "py", "python3"] : ["python3", "python"]);
        return commands;
    }
}
