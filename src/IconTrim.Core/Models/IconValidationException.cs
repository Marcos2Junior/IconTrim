namespace IconTrim.Core.Models;

/// <summary>Reports icon names requested by the consumer but absent from the selected provider.</summary>
/// <remarks>Thrown before asset generation when <c>UnknownIconBehavior.Throw</c> is configured. Scanned references retain file, line, and column; names found only in the safelist have no source location.</remarks>
public sealed class IconValidationException : InvalidOperationException
{
    /// <summary>Creates a validation failure containing unknown names and their scanned references.</summary>
    /// <param name="unknownIcons">Unknown icon names, including any names supplied only by the safelist.</param>
    /// <param name="references">Scanned occurrences of unknown icons, with file, line, and column.</param>
    public IconValidationException(IReadOnlyList<string> unknownIcons, IReadOnlyList<IconReference> references)
        : base("Existem referências a ícones que não existem na biblioteca.")
    {
        UnknownIcons = unknownIcons;
        References = references;
    }

    /// <summary>Distinct icon names that the provider could not resolve.</summary>
    public IReadOnlyList<string> UnknownIcons { get; }
    /// <summary>Source occurrences of unknown names, including file, one-based line, and one-based column.</summary>
    public IReadOnlyList<IconReference> References { get; }
}
