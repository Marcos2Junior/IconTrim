namespace IconTrim.Core.Models;

/// <summary>One occurrence of an icon name in a scanned source file.</summary>
/// <param name="IconName">Matched provider-specific icon name.</param>
/// <param name="File">Physical source file path.</param>
/// <param name="Line">One-based source line number.</param>
/// <param name="Column">One-based source column number.</param>
public sealed record IconReference(string IconName, string File, int Line, int Column);
