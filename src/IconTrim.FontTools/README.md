# IconTrim.FontTools

Provides WOFF2 font subsetting through the external Python FontTools module. Python, FontTools, and Brotli support must be installed separately for font generation; NuGet restore and build do not install or run them.

The adapter automatically discovers a usable Python interpreter. Set `FontToolsOptions.PythonExecutable` to a command or executable path to select one explicitly. Most applications install the `IconTrim` package, which brings this package transitively.
