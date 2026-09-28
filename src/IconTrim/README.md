# IconTrim

IconTrim scans application files for Bootstrap Icons references and generates a smaller WOFF2 font with matching CSS.

```sh
dotnet add package IconTrim
```

The package brings `IconTrim.Core`, `IconTrim.BootstrapIcons`, `IconTrim.FontTools`, and `IconTrim.Hosting` through NuGet dependencies. Font generation requires an external Python installation with FontTools and Brotli support. IconTrim discovers a usable interpreter automatically; set `FontToolsOptions.PythonExecutable` to choose one explicitly.

The package targets `net9.0` and works with .NET 9 and .NET 10 applications. An ASP.NET Core .NET 11 RC1 consumer also passed restore, build, and end-to-end font generation with FontTools. Compatibility with the final .NET 11 release has not yet been verified.

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
