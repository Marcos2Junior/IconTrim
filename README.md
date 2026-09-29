<p align="center">
  <img src="assets/icontrim.png" alt="IconTrim logo" width="124">
</p>

<h1 align="center">IconTrim</h1>

<p align="center"><strong>Bootstrap Icons tree shaking for .NET and ASP.NET Core.</strong></p>

<p align="center">
  <a href="https://www.nuget.org/packages/IconTrim/"><img alt="NuGet version" src="https://img.shields.io/nuget/v/IconTrim?label=NuGet"></a>
  <a href="https://www.nuget.org/packages/IconTrim/"><img alt="NuGet downloads" src="https://img.shields.io/nuget/dt/IconTrim?label=downloads"></a>
  <a href="LICENSE"><img alt="MIT License" src="https://img.shields.io/badge/license-MIT-blue"></a>
</p>

```sh
dotnet add package IconTrim
```

Scan configured Razor, HTML, JavaScript, and C# files to remove unused Bootstrap Icons. IconTrim uses font subsetting to generate a smaller, versioned WOFF2 font with matching CSS. The package brings Core, Bootstrap Icons, FontTools, and Hosting through NuGet dependencies; the CLI remains a repository executable.

## Why IconTrim?

Applications often ship the entire Bootstrap Icons font while using only a small fraction of its icons. In one project that motivated IconTrim:

```text
Bootstrap Icons available:  2,000+
Icons actually used:          126

Original assets:          ~211 KB
IconTrim output:           ~17 KB

Reduction:                 ~92%
```

This is one project, not a universal benchmark. Results vary with the Bootstrap Icons version, icons used, and project configuration.

## Features

- Scan multiple roots with configurable extensions, ignored directory names, and icon prefix.
- Add a file or programmatic safelist for icons that static scanning misses.
- Validate unknown icons with file, line, and column diagnostics.
- Generate a content-versioned WOFF2 and matching CSS; remove older generated font versions after regeneration.
- Reuse scans of unchanged files and skip FontTools when the icon set and other generation inputs are still current.
- Use DI, one-time Generic Host startup integration, a CLI, or the manual runner API.
- Keep Core independent of Bootstrap Icons, Python, FontTools, and ASP.NET Core.

## Requirements

- .NET 8 or later. The packages target `net8.0` as their minimum. The solution tests and an ASP.NET Core consumer have been validated on .NET 8; a .NET 10 consumer also passed restore, build, and font generation. Building this repository requires an SDK that can target `net8.0`.
- Python with `fonttools` and WOFF2 Brotli support for runs using the FontTools adapter. `AddFontTools()` discovers an available interpreter.

Install the Python dependencies in the interpreter you want IconTrim to use (replace `python` if your command differs):

```sh
python -m pip install fonttools brotli
```

Discovery checks an active virtual environment first. It then tries `python`, `py`, and `python3` on Windows, or `python3` and `python` elsewhere, and selects the first interpreter with FontTools and WOFF2 Brotli support. Set `FontToolsOptions.PythonExecutable` to a command or executable path to use only that interpreter. The tests use fakes and do not require Python.

## Projects

| Project | Responsibility |
| --- | --- |
| `IconTrim` | NuGet package that brings the default components together. |
| `IconTrim.Core` | Scanning, validation, incremental state, fingerprinting, and orchestration. |
| `IconTrim.BootstrapIcons` | Bootstrap Icons CSS parsing, icon definitions, and generated CSS. |
| `IconTrim.FontTools` | Python/FontTools font subsetting. |
| `IconTrim.Hosting` | One-time execution during Generic Host startup. |
| `IconTrim.Cli` | Configuration, invocation, and console output for manual or scripted runs. |

```text
BootstrapIcons ─┐
FontTools ──────┼──> Core
Hosting ────────┘
CLI ────────────> Core + adapters
```

The adapters depend on Core; Core does not know the consumer application or its hosting model.

## ASP.NET Core / Generic Host

After installing `IconTrim`, configure paths for your application in an ASP.NET Core composition root:

```csharp
using IconTrim.BootstrapIcons.DependencyInjection;
using IconTrim.Core.DependencyInjection;
using IconTrim.FontTools.DependencyInjection;
using IconTrim.Hosting.DependencyInjection;

builder.Services.AddIconTrim(options =>
{
    options.BasePath = Path.GetFullPath(builder.Environment.ContentRootPath);
    options.Scan.Roots.Add(".");
    options.Scan.IconPrefix = "bi-";

    options.Font.SourceFontPath = "wwwroot/lib/bootstrap-icons/fonts/bootstrap-icons.woff2";
    options.Output.CssPath = "wwwroot/css/bootstrap-icons.subset.css";
    options.Output.FontDirectory = "wwwroot/fonts";
    options.Output.FontUrlPrefix = "/fonts";
    options.Output.AssetPrefix = "bootstrap-icons";
})
.AddBootstrapIcons(options =>
    options.CssPath = "wwwroot/lib/bootstrap-icons/bootstrap-icons.min.css")
.AddFontTools();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddIconTrimOnStartup();
}
```

Set these input and output paths to match your project, and serve the generated CSS and font from their configured public locations. DI registration does not scan or generate assets. `AddIconTrimOnStartup()` runs once when the host starts; the host waits for it, and generation errors propagate. The consumer chooses the environments in which to register it. Hosting does not run IconTrim during `dotnet build`.

## CLI

Copy [icontrim.example.json](icontrim.example.json), edit its paths, and run from the repository root:

```sh
dotnet run --project src/IconTrim.Cli -- path/to/icontrim.json
```

Relative paths resolve against `IconTrim.BasePath`. If `BasePath` is empty in the CLI configuration, the CLI uses the configuration file's directory. DI consumers must supply an absolute `BasePath`.

## Manual execution

`IIconTrimRunner` remains available for explicit runs, including scripts and applications without Hosting:

```csharp
var result = await serviceProvider
    .GetRequiredService<IIconTrimRunner>()
    .RunAsync(cancellationToken);
```

Applications using `IconTrim.Hosting` normally do not call this themselves. `TrimResult` reports discovered, scanned, and cached files; selected icons; asset sizes and paths; duration; and whether generation was skipped because the assets were up to date.

## Unknown icons

Unknown icon names fail validation by default. For a typo such as `bi-whatsap`, the CLI reports the name and scanned source locations as `file:line:column` (using physical file paths). Set `UnknownIconBehavior` to `Ignore` to exclude unknown names and continue with known icons; generation still needs at least one known icon.

## Safelist

Dynamic class names may not appear as complete strings in source files. Add them to an optional safelist file:

```text
bi-whatsapp
bi-instagram
bi-cart
```

Set `IconTrimOptions.SafelistFile` to its path, or add names directly to `IconTrimOptions.Safelist`. A missing file contributes no icons. Blank lines and lines beginning with `#` are ignored; duplicate names are combined without regard to case.

## Incremental execution

On each run, IconTrim enumerates eligible files, reuses cached references from files whose path, size, and UTC modification time are unchanged, and scans only new or changed files. It then compares a generation fingerprint for the effective icon set, safelist, original CSS and font, and relevant configuration. If that fingerprint matches and both generated assets exist, it skips FontTools, CSS writing, and cleanup.

Changing a source file without changing the icons therefore does not require a new font:

```text
217 files discovered
  1 file scanned
216 reused from cache

Icon set unchanged → FontTools skipped
```

The local state defaults to `obj/icontrim/state.json` under `BasePath` and can be moved with `IconTrimOptions.StatePath`. Missing or invalid state is rebuilt. If either output asset is missing, generation runs again.

## Generated assets

With `AssetPrefix = "bootstrap-icons"`, a generated font is named `bootstrap-icons.<hash>.woff2`. The CSS references that versioned filename through `FontUrlPrefix`. After a successful generation, IconTrim removes older generated WOFF2 files with the same prefix.

## Extensibility

`IIconProvider` supplies available icon definitions, `IFontSubsetGenerator` writes the selected font subset, and `IIconTrimRunner` coordinates the work. Bootstrap Icons and FontTools are the current adapters; other providers or generators can implement these abstractions without changing Core.

## Tests

```sh
dotnet run --project tests/IconTrim.Tests
```

The tests use fake runners and font generators, so they do not launch Python or FontTools. They cover Hosting and key incremental cases such as unchanged files, changed icon sets, missing outputs, and invalid state.

## Status

IconTrim is actively maintained and focuses on Bootstrap Icons optimization for .NET and ASP.NET Core applications.

The current release includes:

- Bootstrap Icons source scanning and validation
- WOFF2 font subsetting with FontTools
- ASP.NET Core and Generic Host integration
- Dependency injection support
- Incremental scanning and generation
- CLI execution
- NuGet packages for the engine and default integrations

Build-time integration and additional icon providers may be added in future releases.

## License

IconTrim is licensed under the [MIT License](LICENSE).
