using IconTrim.BootstrapIcons.DependencyInjection;
using IconTrim.Core.Abstractions;
using IconTrim.Core.Configuration;
using IconTrim.Core.DependencyInjection;
using IconTrim.Core.Models;
using Microsoft.Extensions.DependencyInjection;

internal static class IncrementalTests
{
    public static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "IconTrimIncremental-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "app"));
        try
        {
            var fileA = Path.Combine(root, "app", "a.cshtml");
            var fileB = Path.Combine(root, "app", "b.cshtml");
            var fileC = Path.Combine(root, "app", "c.cshtml");
            var cssSource = Path.Combine(root, "bootstrap.css");
            var fontSource = Path.Combine(root, "original.woff2");
            var safelist = Path.Combine(root, "icons.safelist");
            var statePath = Path.Combine(root, "obj", "icontrim", "state.json");
            var writeCount = 0;
            void Change(string path, string content)
            {
                File.WriteAllText(path, content);
                File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(++writeCount));
            }
            Change(fileA, "bi-house");
            Change(fileB, "bi-star");
            File.WriteAllText(cssSource,
                ".bi-house::before{content:'\\F001'}.bi-star::before{content:'\\F002'}.bi-heart::before{content:'\\F003'}.bi-moon::before{content:'\\F004'}");
            File.WriteAllBytes(fontSource, [1, 2, 3]);
            File.WriteAllText(safelist, "# empty\n");

            var options = new IconTrimOptions
            {
                BasePath = root,
                SafelistFile = "icons.safelist",
                Scan = new ScanOptions { Roots = ["app"], IconPrefix = "bi-" },
                Font = new FontOptions { SourceFontPath = "original.woff2" },
                Output = new OutputOptions
                {
                    CssPath = "out/icons.css", FontDirectory = "out/fonts",
                    FontUrlPrefix = "/fonts", AssetPrefix = "icons"
                }
            };
            var generator = new CountingSubsetGenerator();
            var services = new ServiceCollection();
            services.AddIconTrim(x =>
            {
                x.BasePath = options.BasePath;
                x.SafelistFile = options.SafelistFile;
                x.Scan = options.Scan;
                x.Font = options.Font;
                x.Output = options.Output;
                x.UnknownIconBehavior = options.UnknownIconBehavior;
            }).AddBootstrapIcons(x => x.CssPath = "bootstrap.css");
            services.AddSingleton<IFontSubsetGenerator>(generator);
            using var provider = services.BuildServiceProvider();
            var runner = provider.GetRequiredService<IIconTrimRunner>();
            var configured = provider.GetRequiredService<IconTrimOptions>();
            var stateStore = provider.GetRequiredService<IIconTrimStateStore>();

            var first = await runner.RunAsync();
            Check(!first.Skipped && first.FilesDiscovered == 2 && first.FilesScanned == 2 &&
                first.FilesReusedFromCache == 0 && generator.Calls == 1 && File.Exists(statePath), "first run");
            var second = await runner.RunAsync();
            Check(second.Skipped && second.FilesScanned == 0 && second.FilesReusedFromCache == 2 &&
                generator.Calls == 1, "unchanged run");

            Change(fileA, "<!-- unchanged icons --> bi-house");
            var unchangedIcons = await runner.RunAsync();
            Check(unchangedIcons.Skipped && unchangedIcons.FilesScanned == 1 &&
                unchangedIcons.FilesReusedFromCache == 1 && generator.Calls == 1, "changed file, same icons");

            Change(fileA, "bi-house bi-heart");
            var addedIcon = await runner.RunAsync();
            Check(!addedIcon.Skipped && addedIcon.GeneratedIcons == 3 && generator.Calls == 2, "icon added");
            Change(fileA, "bi-house");
            var removedIcon = await runner.RunAsync();
            Check(!removedIcon.Skipped && removedIcon.GeneratedIcons == 2 && generator.Calls == 3, "icon removed");

            Change(fileC, "bi-heart");
            var newFile = await runner.RunAsync();
            Check(!newFile.Skipped && newFile.FilesDiscovered == 3 && newFile.FilesScanned == 1 &&
                newFile.GeneratedIcons == 3 && generator.Calls == 4, "new file");
            File.Delete(fileC);
            var deletedFile = await runner.RunAsync();
            Check(!deletedFile.Skipped && deletedFile.FilesRemovedFromCache == 1 &&
                deletedFile.GeneratedIcons == 2 && generator.Calls == 5, "deleted file");

            File.WriteAllText(safelist, "bi-moon\n");
            var newSafelist = await runner.RunAsync();
            Check(!newSafelist.Skipped && newSafelist.FilesScanned == 0 &&
                newSafelist.GeneratedIcons == 3 && generator.Calls == 6, "safelist changed");
            File.WriteAllBytes(fontSource, [1, 2, 3, 4]);
            var newFont = await runner.RunAsync();
            Check(!newFont.Skipped && newFont.FilesScanned == 0 && generator.Calls == 7, "source font changed");
            File.AppendAllText(cssSource, "/* new release */");
            var newProviderCss = await runner.RunAsync();
            Check(!newProviderCss.Skipped && newProviderCss.FilesScanned == 0 && generator.Calls == 8, "provider CSS changed");

            configured.Output.FontUrlPrefix = "/assets/fonts";
            var changedOutput = await runner.RunAsync();
            Check(!changedOutput.Skipped && generator.Calls == 9 &&
                File.ReadAllText(changedOutput.GeneratedCssPath).Contains("/assets/fonts/"), "output configuration changed");

            File.Delete(changedOutput.GeneratedCssPath);
            var missingCss = await runner.RunAsync();
            Check(!missingCss.Skipped && generator.Calls == 10, "missing CSS output");
            File.Delete(missingCss.GeneratedFontPath);
            var missingFont = await runner.RunAsync();
            Check(!missingFont.Skipped && generator.Calls == 11, "missing font output");

            File.WriteAllText(statePath, "{ invalid json");
            var corruptState = await runner.RunAsync();
            Check(!corruptState.Skipped && corruptState.FilesScanned == 2 && generator.Calls == 12,
                "corrupt state rebuild");
            var incompatible = await stateStore.LoadAsync(statePath);
            incompatible!.Version = IconTrimState.CurrentVersion + 1;
            await stateStore.SaveAsync(statePath, incompatible);
            var incompatibleState = await runner.RunAsync();
            Check(!incompatibleState.Skipped && incompatibleState.FilesScanned == 2 && generator.Calls == 13,
                "incompatible state rebuild");
            var stateBeforeFailure = await stateStore.LoadAsync(statePath);
            Check(stateBeforeFailure?.GenerationFingerprint is not null, "valid state after rebuild");
            File.WriteAllBytes(fontSource, [1, 2, 3, 4, 5]);
            generator.FailNext = true;
            try { await runner.RunAsync(); throw new Exception("generation failure was swallowed"); }
            catch (InvalidOperationException ex) when (ex.Message == "fake subset failure") { }
            var stateAfterFailure = await stateStore.LoadAsync(statePath);
            Check(stateAfterFailure?.GenerationFingerprint == stateBeforeFailure!.GenerationFingerprint,
                "failed generation does not update fingerprint");
            var recovered = await runner.RunAsync();
            Check(!recovered.Skipped, "generation recovers after failure");

            configured.UnknownIconBehavior = UnknownIconBehavior.Ignore;
            Change(fileA, "bi-house\nbi-unknown");
            var ignoredUnknown = await runner.RunAsync();
            Check(!ignoredUnknown.Skipped && ignoredUnknown.UnknownIcons.SequenceEqual(["bi-unknown"]),
                "unknown icon cached under Ignore");
            configured.UnknownIconBehavior = UnknownIconBehavior.Throw;
            try { await runner.RunAsync(); throw new Exception("cached unknown icon was accepted"); }
            catch (IconValidationException ex)
            {
                Check(ex.References.Single().Line == 2 && ex.References.Single().Column == 1,
                    "cached unknown icon location");
            }
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }

        await GeneratedCssIsExcludedAsync();
    }

    private static async Task GeneratedCssIsExcludedAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "IconTrimOutputScan-" + Guid.NewGuid().ToString("N"));
        var site = Path.Combine(root, "site");
        Directory.CreateDirectory(site);
        try
        {
            var view = Path.Combine(site, "view.cshtml");
            File.WriteAllText(view, "bi-house bi-star");
            File.WriteAllText(Path.Combine(site, "theme.css"), "/* unrelated CSS */");
            File.WriteAllText(Path.Combine(site, "bootstrap.css"),
                ".bi-house::before{content:'\\F001'}.bi-star::before{content:'\\F002'}");
            File.WriteAllBytes(Path.Combine(root, "original.woff2"), [1, 2, 3]);

            var generator = new CountingSubsetGenerator();
            var services = new ServiceCollection();
            services.AddIconTrim(options =>
            {
                options.BasePath = root;
                options.Scan.Roots.Add("site");
                options.Scan.Extensions.Add(".css");
                options.Scan.IconPrefix = "bi-";
                options.Font.SourceFontPath = "original.woff2";
                options.Output.CssPath = "site/generated/icons.css";
                options.Output.FontDirectory = "site/generated/fonts";
                options.Output.FontUrlPrefix = "/fonts";
                options.Output.AssetPrefix = "icons";
            }).AddBootstrapIcons(options => options.CssPath = "site/bootstrap.css");
            services.AddSingleton<IFontSubsetGenerator>(generator);
            using var provider = services.BuildServiceProvider();
            var runner = provider.GetRequiredService<IIconTrimRunner>();
            var stateStore = provider.GetRequiredService<IIconTrimStateStore>();
            var statePath = Path.Combine(root, "obj", "icontrim", "state.json");

            var first = await runner.RunAsync();
            Check(first.GeneratedIcons == 2 && first.FilesDiscovered == 2 && generator.Calls == 1,
                "generated CSS setup");

            var outputCss = Path.Combine(root, "site", "generated", "icons.css");
            var outputMetadata = new FileInfo(outputCss);
            var state = (await stateStore.LoadAsync(statePath))!;
            const string outputRelativePath = "site/generated/icons.css";
            state.Files[outputRelativePath] = new ScannedFileState
            {
                RelativePath = outputRelativePath,
                Length = outputMetadata.Length,
                LastWriteUtcTicks = outputMetadata.LastWriteTimeUtc.Ticks,
                References = [new CachedIconReference("bi-star", 1, 1)]
            };
            var providerCss = Path.Combine(site, "bootstrap.css");
            var providerMetadata = new FileInfo(providerCss);
            const string providerRelativePath = "site/bootstrap.css";
            state.Files[providerRelativePath] = new ScannedFileState
            {
                RelativePath = providerRelativePath,
                Length = providerMetadata.Length,
                LastWriteUtcTicks = providerMetadata.LastWriteTimeUtc.Ticks,
                References = [new CachedIconReference("bi-star", 1, 1)]
            };
            await stateStore.SaveAsync(statePath, state);

            File.WriteAllText(view, "bi-house");
            var removedIcon = await runner.RunAsync();
            Check(!removedIcon.Skipped && removedIcon.GeneratedIcons == 1 &&
                removedIcon.FilesDiscovered == 2 && removedIcon.FilesScanned == 1 &&
                removedIcon.FilesReusedFromCache == 1 && removedIcon.FilesRemovedFromCache == 2 &&
                generator.Calls == 2 && !File.ReadAllText(outputCss).Contains(".bi-star", StringComparison.Ordinal),
                "generated and provider CSS excluded");

            var updatedState = (await stateStore.LoadAsync(statePath))!;
            Check(!updatedState.Files.ContainsKey(outputRelativePath) &&
                !updatedState.Files.ContainsKey(providerRelativePath), "CSS inputs removed from state");
            var unchanged = await runner.RunAsync();
            Check(unchanged.Skipped && unchanged.FilesScanned == 0 &&
                unchanged.FilesReusedFromCache == 2 && generator.Calls == 2,
                "generated CSS ignored on later runs");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception($"Failed: {name}");
    }

    private sealed class CountingSubsetGenerator : IFontSubsetGenerator
    {
        public int Calls { get; private set; }
        public bool FailNext { get; set; }

        public Task GenerateAsync(string sourceFont, string outputFont,
            IReadOnlyCollection<IconDefinition> icons, CancellationToken cancellationToken = default)
        {
            Calls++;
            if (FailNext)
            {
                FailNext = false;
                throw new InvalidOperationException("fake subset failure");
            }
            var content = Convert.ToHexString(File.ReadAllBytes(sourceFont)) + ":" +
                string.Join(",", icons.Select(x => x.UnicodeHex));
            File.WriteAllText(outputFont, content);
            return Task.CompletedTask;
        }
    }
}
