using System.Text.RegularExpressions;
using IconTrim.Core.Models;

namespace IconTrim.BootstrapIcons;

/// <summary>Extracts icon names and Unicode glyphs from the original Bootstrap Icons CSS.</summary>
public sealed class BootstrapIconsCssParser
{
    private static readonly Regex RuleRegex = new(@"\.bi-(?<name>[a-zA-Z0-9-]+)::before\s*\{(?<body>[^}]*)\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex ContentRegex = new(@"content\s*:\s*[""']\\(?<unicode>[0-9a-fA-F]+)[""']", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>Reads <c>.bi-name::before</c> content rules into provider-independent icon definitions.</summary>
    /// <param name="css">Original Bootstrap Icons CSS text.</param>
    /// <returns>Definitions indexed by icon name without regard to case.</returns>
    /// <exception cref="InvalidOperationException">No icon rule with a Unicode content value was found.</exception>
    public IReadOnlyDictionary<string, IconDefinition> Parse(string css)
    {
        var icons = new Dictionary<string, IconDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (Match rule in RuleRegex.Matches(css))
        {
            var content = ContentRegex.Match(rule.Groups["body"].Value);
            if (!content.Success) continue;
            var name = "bi-" + rule.Groups["name"].Value;
            icons[name] = new IconDefinition(name, content.Groups["unicode"].Value.ToUpperInvariant());
        }
        if (icons.Count == 0) throw new InvalidOperationException("Não foi possível extrair nenhum ícone do bootstrap-icons.min.css.");
        return icons;
    }
}
