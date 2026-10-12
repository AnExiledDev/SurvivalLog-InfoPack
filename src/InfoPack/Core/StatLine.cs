using System;
using System.Collections.Generic;
using System.Globalization;

namespace InfoPack.Core;

/// <summary>
/// The game's own wording for one stat: one format for a gain and one for a loss, each with a "{0}" where the number
/// goes. The game's item tips use the same pair (satiety, mood, energy, health, life, in its ValueDisplay order).
/// </summary>
public readonly record struct StatFormat(string Gain, string Loss);

public static class StatLine
{
    /// <summary>
    /// One line naming every stat the item changes, in the game's wording, or null when it changes none. A loss keeps
    /// its minus sign, as the game's loss format carries no sign of its own. Whole numbers print bare and anything else
    /// to one decimal.
    /// </summary>
    public static string? Describe(IReadOnlyList<StatFormat> formats, IReadOnlyList<float> values)
    {
        var parts = new List<string>();
        var count = Math.Min(formats.Count, values.Count);

        for (var i = 0; i < count; i++)
        {
            var value = Math.Round(values[i], 1);

            // A value too small to show at one decimal would print as "0" or "-0", so it is left out.
            if (value == 0) continue;

            var format = value > 0 ? formats[i].Gain : formats[i].Loss;

            parts.Add(Fill(format, value.ToString("0.#", CultureInfo.InvariantCulture)));
        }

        return parts.Count == 0 ? null : string.Join(", ", parts);
    }

    private static string Fill(string format, string number) =>
        format.Contains("{0}", StringComparison.Ordinal) ? format.Replace("{0}", number, StringComparison.Ordinal) : format + " " + number;
}
