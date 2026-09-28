using IconTrim.Core.Models;

namespace IconTrim.Core.Abstractions;

/// <summary>Produces provider-specific CSS for the selected icons and versioned font URL.</summary>
public interface ICssGenerator
{
    /// <summary>Builds CSS that references the generated subset font.</summary>
    /// <param name="icons">Definitions included in the subset font.</param>
    /// <param name="fontUrl">Public URL to write into CSS, not a filesystem path.</param>
    /// <returns>The complete generated CSS text.</returns>
    string Generate(IReadOnlyCollection<IconDefinition> icons, string fontUrl);
}
