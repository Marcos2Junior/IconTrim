using System.Security.Cryptography;
using System.Text;
using IconTrim.BootstrapIcons;
using IconTrim.BootstrapIcons.DependencyInjection;
using IconTrim.Core.Abstractions;
using IconTrim.Core.Configuration;
using IconTrim.Core.DependencyInjection;
using IconTrim.Core.Models;
using IconTrim.Core.Scanning;
using Microsoft.Extensions.DependencyInjection;

var root = Path.Combine(Path.GetTempPath(), "IconTrimTests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
try
{
    Directory.CreateDirectory(Path.Combine(root, "app", "bin"));
    File.WriteAllText(Path.Combine(root, "app", "view.cshtml"), "<i class='bi-house'></i>\n<i class='bi-house'></i>\n");
    File.WriteAllText(Path.Combine(root, "app", "bin", "ignored.cs"), "bi-missing");
    File.WriteAllText(Path.Combine(root, "icons.css"), ".bi-house::before{content:\"\\F001\"}.bi-star::before{content:'\\F002'}");
    File.WriteAllBytes(Path.Combine(root, "font.woff2"), [1, 2, 3]);
    File.WriteAllText(Path.Combine(root, "icons.safelist"), "# comment\nbi-star\n");
    var options = new IconTrimOptions
    {
        BasePath = root,
        SafelistFile = "icons.safelist",
        Scan = new ScanOptions { Roots = ["app"], IconPrefix = "bi-" },
        Font = new FontOptions { SourceFontPath = "font.woff2" },
        Output = new OutputOptions { CssPath = "out/icons.css", FontDirectory = "out/fonts", FontUrlPrefix = "/fonts", AssetPrefix = "icons" }
    };
    var services = new ServiceCollection();
    services.AddIconTrim(x =>
    {
        x.BasePath = options.BasePath;
        x.SafelistFile = options.SafelistFile;
        x.Scan = options.Scan;
        x.Font = options.Font;
        x.Output = options.Output;
    }).AddBootstrapIcons(x => x.CssPath = "icons.css");
    services.AddSingleton<IFontSubsetGenerator, FakeSubsetGenerator>();
    using var provider = services.BuildServiceProvider();
    var scan = provider.GetRequiredService<IIconScanner>().Scan();
    Check(scan.FilesScanned == 1 && scan.TotalReferences == 2 && scan.UniqueIcons == 1, "scan counts");
    Check(scan.References[1].Line == 2 && scan.References[1].Column == 11, "reference location");
    var runner = provider.GetRequiredService<IIconTrimRunner>();
    var first = await runner.RunAsync();
    Check(first.GeneratedIcons == 2 && first.SafelistedIcons == 1, "safelist and subset");
    var expectedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("F001,F002"))).ToLowerInvariant()[..12];
    Check(Path.GetFileName(first.GeneratedFontPath) == $"icons.{expectedHash}.woff2", "font hash");
    Check(File.ReadAllText(first.GeneratedCssPath).Contains($"/fonts/icons.{expectedHash}.woff2"), "CSS URL");
    File.WriteAllText(Path.Combine(root, "out", "fonts", "icons.aaaaaaaaaaaa.woff2"), "old");
    File.WriteAllText(Path.Combine(root, "out", "fonts", "other.aaaaaaaaaaaa.woff2"), "keep");
    File.WriteAllBytes(Path.Combine(root, "font.woff2"), [1, 2, 3, 4]);
    var second = await runner.RunAsync();
    Check(second.RemovedAssets == 1 && File.Exists(Path.Combine(root, "out", "fonts", "other.aaaaaaaaaaaa.woff2")), "asset cleanup");
    File.AppendAllText(Path.Combine(root, "app", "view.cshtml"), "bi-missing");
    try { await runner.RunAsync(); throw new Exception("unknown icon was accepted"); }
    catch (IconValidationException ex)
    {
        Check(ex.UnknownIcons.SequenceEqual(["bi-missing"]) && ex.References.Single().Line == 3, "unknown icon location");
    }
    var parsed = new BootstrapIconsCssParser().Parse(File.ReadAllText(Path.Combine(root, "icons.css")));
    Check(parsed["bi-house"].UnicodeHex == "F001", "Bootstrap parser");
    await HostingTests.RunAsync();
    await IncrementalTests.RunAsync();
    await PythonResolutionTests.RunAsync();
    Console.WriteLine("IconTrim tests passed.");
    return 0;
}
finally
{
    Directory.Delete(root, true);
}

static void Check(bool condition, string name)
{
    if (!condition) throw new Exception($"Failed: {name}");
}

sealed class FakeSubsetGenerator : IFontSubsetGenerator
{
    public Task GenerateAsync(string sourceFont, string outputFont, IReadOnlyCollection<IconDefinition> icons, CancellationToken cancellationToken = default)
    {
        File.WriteAllText(outputFont, string.Join(",", icons.Select(x => x.UnicodeHex)));
        return Task.CompletedTask;
    }
}
