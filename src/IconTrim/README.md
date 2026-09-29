# IconTrim

IconTrim scans application files for Bootstrap Icons references and generates a smaller WOFF2 font with matching CSS.

```sh
dotnet add package IconTrim
```

The package brings `IconTrim.Core`, `IconTrim.BootstrapIcons`, `IconTrim.FontTools`, and `IconTrim.Hosting` through NuGet dependencies. Font generation requires an external Python installation with FontTools and Brotli support. IconTrim discovers a usable interpreter automatically; set `FontToolsOptions.PythonExecutable` to choose one explicitly.

The package targets `net8.0` as its minimum and supports .NET 8 and later applications. The solution tests and an ASP.NET Core consumer have been validated on .NET 8; a .NET 10 consumer also passed restore, build, and font generation.

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
.AddBootstrapIcons(options => options.CssPath = "wwwroot/lib/bootstrap-icons/bootstrap-icons.min.css")
.AddFontTools();

builder.Services.AddIconTrimOnStartup();
```

See the [repository README](https://github.com/Marcos2Junior/IconTrim) for CLI, configuration, and incremental execution details.
