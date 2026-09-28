namespace IconTrim.Core.Models;

/// <summary>An icon name and the Unicode code point used by the provider's source font.</summary>
/// <param name="Name">Provider-specific icon name, such as <c>bi-house</c>.</param>
/// <param name="UnicodeHex">Uppercase or lowercase hexadecimal code point without the <c>U+</c> prefix.</param>
public sealed record IconDefinition(string Name, string UnicodeHex);
