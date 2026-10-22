using System.Collections.Generic;
using System.Linq;

namespace InfoPack.Core;

/// <summary>
/// The order setting: section keys separated by commas. It is typed by hand, so it forgives: case and spaces do not
/// matter, a key it does not know or names twice is passed over, and a section left out keeps its default place
/// after the ones named. The result always holds every section once.
/// </summary>
public static class TipOrder
{
    /// <summary>The setting's default text, naming every section the way the per-section settings are named.</summary>
    public const string DefaultText = "Effects, Fresh, Notes, Home, UsedIn, Grow, Value, MadeFrom, Breaks, Fuel, FoundIn, About";

    public static IReadOnlyList<TipSection> Parse(string? text)
    {
        var named = new List<TipSection>();

        foreach (var key in (text ?? "").Split(','))
        {
            if (TipSections.TryParse(key, out var section)) named.Add(section);
        }

        return Complete(named);
    }

    /// <summary>The sections given, first mention kept, followed by every missing one in default order.</summary>
    public static IReadOnlyList<TipSection> Complete(IEnumerable<TipSection> sections)
    {
        var order = new List<TipSection>();
        var seen = new HashSet<TipSection>();

        foreach (var section in sections.Concat(TipSections.DefaultOrder))
        {
            if (seen.Add(section)) order.Add(section);
        }

        return order;
    }
}
