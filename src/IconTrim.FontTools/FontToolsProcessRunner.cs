using System.Diagnostics;
using IconTrim.FontTools.Configuration;

namespace IconTrim.FontTools;

/// <summary>Runs and diagnoses the external <c>fontTools.subset</c> Python module.</summary>
public sealed class FontToolsProcessRunner
{
    private readonly PythonExecutableResolver _resolver;

    /// <summary>Creates a runner that discovers a usable interpreter unless one is configured explicitly.</summary>
    /// <param name="options">Optional explicit Python executable; null enables discovery.</param>
    public FontToolsProcessRunner(FontToolsOptions options)
    {
        _resolver = new PythonExecutableResolver(
            options, new PythonProcessProbe(), OperatingSystem.IsWindows(),
            Environment.GetEnvironmentVariable("VIRTUAL_ENV"));
    }

    internal FontToolsProcessRunner(PythonExecutableResolver resolver) => _resolver = resolver;

    internal Task<ResolvedPython> ResolvePythonAsync(CancellationToken cancellationToken) =>
        _resolver.ResolveAsync(cancellationToken);

    /// <summary>Runs FontTools with redirected output and verifies that it created the requested WOFF2.</summary>
    /// <param name="source">Original WOFF2 input path.</param>
    /// <param name="output">Temporary WOFF2 output path.</param>
    /// <param name="unicodes">Comma-separated <c>U+XXXX</c> code points.</param>
    /// <param name="cancellationToken">Cancels the wait and terminates the external process.</param>
    /// <returns>A task that completes on successful process exit.</returns>
    /// <exception cref="InvalidOperationException">Python could not start, FontTools failed, or no output file was produced.</exception>
    public async Task RunAsync(string source, string output, string unicodes, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var python = await _resolver.ResolveAsync(cancellationToken);
        var startInfo = new ProcessStartInfo
        {
            FileName = python.ExecutablePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("-m");
        startInfo.ArgumentList.Add("fontTools.subset");
        startInfo.ArgumentList.Add(source);
        startInfo.ArgumentList.Add($"--unicodes={unicodes}");
        startInfo.ArgumentList.Add("--flavor=woff2");
        startInfo.ArgumentList.Add($"--output-file={output}");
        using var process = new Process { StartInfo = startInfo };
        try { process.Start(); }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Não foi possível executar o FontTools com {python.ExecutablePath}. Instale ou valide as dependências com: {python.ExecutablePath} -m pip install fonttools brotli", ex);
        }
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException) { } // O processo terminou entre a verificação e o Kill.
            await process.WaitForExitAsync(CancellationToken.None);
            throw;
        }
        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"FontTools terminou com código {process.ExitCode}.\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
        if (!File.Exists(output))
            throw new InvalidOperationException("FontTools terminou sem erro, mas o WOFF2 de saída não foi criado.");
    }
}
