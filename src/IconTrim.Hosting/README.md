# IconTrim.Hosting

Runs IconTrim once during Generic Host startup through `AddIconTrimOnStartup()`. The host waits for generation to finish and propagates failures. The consumer chooses when to register this integration.

Most applications install the `IconTrim` package, which brings this package transitively.
