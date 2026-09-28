using System.Text.Json;
using System.Text.Json.Serialization;
using IconTrim.BootstrapIcons.DependencyInjection;
using IconTrim.Cli.Commands;
using IconTrim.Cli.Configuration;
using IconTrim.Core.DependencyInjection;
using IconTrim.FontTools.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

Console.OutputEncoding = System.Text.Encoding.UTF8;
var configPath = Path.GetFullPath(args.FirstOrDefault() ?? "icontrim.json");
if (!File.Exists(configPath))
{
    Console.Error.WriteLine($"Configuração não encontrada: {configPath}");
    return 1;
}
var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
jsonOptions.Converters.Add(new JsonStringEnumConverter());
var config = JsonSerializer.Deserialize<CliConfiguration>(await File.ReadAllTextAsync(configPath), jsonOptions)
    ?? throw new InvalidOperationException("Configuração vazia.");
if (string.IsNullOrWhiteSpace(config.IconTrim.BasePath))
    config.IconTrim.BasePath = Path.GetDirectoryName(configPath)!;
var services = new ServiceCollection();
services.AddIconTrim(x => Copy(config.IconTrim, x))
    .AddBootstrapIcons(x => x.CssPath = config.BootstrapIcons.CssPath)
    .AddFontTools(x => x.PythonExecutable = config.FontTools.PythonExecutable);
services.AddTransient<RunCommand>();
using var provider = services.BuildServiceProvider();
return await provider.GetRequiredService<RunCommand>().ExecuteAsync();

static void Copy(IconTrim.Core.Configuration.IconTrimOptions source, IconTrim.Core.Configuration.IconTrimOptions target)
{
    target.BasePath = source.BasePath;
    target.StatePath = source.StatePath;
    target.SafelistFile = source.SafelistFile;
    target.Safelist = source.Safelist;
    target.UnknownIconBehavior = source.UnknownIconBehavior;
    target.Scan = source.Scan;
    target.Output = source.Output;
    target.Font = source.Font;
}
