using IconTrim.Core.Abstractions;
using IconTrim.Core.Models;

namespace IconTrim.Cli.Commands;

public sealed class RunCommand(IIconTrimRunner runner)
{
    public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await runner.RunAsync(cancellationToken);
            Console.WriteLine($"Arquivos encontrados: {result.FilesDiscovered:N0}");
            Console.WriteLine($"Arquivos analisados: {result.FilesScanned:N0}");
            Console.WriteLine($"Reutilizados do cache: {result.FilesReusedFromCache:N0}");
            Console.WriteLine($"Removidos do cache: {result.FilesRemovedFromCache:N0}");
            Console.WriteLine($"Referências encontradas: {result.TotalReferences:N0}");
            Console.WriteLine($"Ícones únicos: {result.UniqueIcons:N0}");
            Console.WriteLine($"Ícones gerados: {result.GeneratedIcons:N0}");
            Console.WriteLine($"Ícones na safelist: {result.SafelistedIcons:N0}");
            if (result.UnknownIcons.Count > 0)
                Console.WriteLine($"Ícones desconhecidos ignorados: {string.Join(", ", result.UnknownIcons)}");
            if (result.Skipped)
            {
                Console.WriteLine("Assets já estão atualizados.");
                Console.WriteLine($"Duração: {result.Duration}");
                return 0;
            }
            Console.WriteLine($"CSS: {FormatBytes(result.OriginalCssBytes)} → {FormatBytes(result.GeneratedCssBytes)}");
            Console.WriteLine($"WOFF2: {FormatBytes(result.OriginalFontBytes)} → {FormatBytes(result.GeneratedFontBytes)}");
            Console.WriteLine($"Redução bruta: {result.ReductionPercent:F1}%");
            Console.WriteLine($"Fontes antigas removidas: {result.RemovedAssets}");
            Console.WriteLine($"Fonte gerada: {result.GeneratedFontPath}");
            Console.WriteLine($"CSS gerado: {result.GeneratedCssPath}");
            Console.WriteLine($"Duração: {result.Duration}");
            return 0;
        }
        catch (IconValidationException ex)
        {
            Console.Error.WriteLine(ex.Message);
            foreach (var icon in ex.UnknownIcons)
            {
                Console.Error.WriteLine($"  - {icon}");
                foreach (var location in ex.References.Where(x => x.IconName.Equals(icon, StringComparison.OrdinalIgnoreCase)).Take(5))
                    Console.Error.WriteLine($"      {location.File}:{location.Line}:{location.Column}");
            }
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static string FormatBytes(long bytes) => bytes < 1024 ? $"{bytes} B" : $"{bytes / 1024d:F1} KB";
}
