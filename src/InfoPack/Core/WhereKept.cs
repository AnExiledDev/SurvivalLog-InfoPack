using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace InfoPack.Core;

/// <summary>One home storage holding an item, and how many of it are there.</summary>
public readonly record struct KeptIn(string Storage, int Count);

/// <summary>
/// The per-storage breakdown the breakdown key shows under the home count. It reads home the way the count does, so
/// the rows always add up to the "At home" number.
/// </summary>
public static class WhereKept
{
    /// <summary>
    /// The storages holding the item, biggest count first, ties by name. Storages that share a name are numbered in
    /// the order home lists them ("Shelf", "Shelf 2"), counting every storage, so a storage keeps its number whichever
    /// item is hovered.
    /// </summary>
    public static IReadOnlyList<KeptIn> Rows(IHomeItems home, Func<long, string> nameOf, int configId)
    {
        var rows = new List<KeptIn>();
        var seen = new HashSet<long>();
        var timesNamed = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var storageId in home.HomeStorageIds())
        {
            if (!seen.Add(storageId)) continue;

            var name = Numbered(nameOf(storageId), timesNamed);
            var count = CountIn(home, storageId, configId);

            if (count > 0) rows.Add(new KeptIn(name, count));
        }

        return rows
            .OrderByDescending(row => row.Count)
            .ThenBy(row => row.Storage, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>The rows argument of the page's setWhere call: [["Fridge",6],["Inventory",3]].</summary>
    public static string ToJson(IReadOnlyList<KeptIn> rows) =>
        "[" + string.Join(",", rows.Select(row => "[" + JsText.Literal(row.Storage) + "," + row.Count.ToString(CultureInfo.InvariantCulture) + "]")) + "]";

    /// <summary>Skips stacks the cache lists under a storage they no longer belong to, as the home count does.</summary>
    private static int CountIn(IHomeItems home, long storageId, int configId) =>
        home.StacksIn(storageId)
            .Where(stack => stack.OwnerId == storageId && stack.ConfigId == configId && stack.Count > 0)
            .Sum(stack => stack.Count);

    private static string Numbered(string name, Dictionary<string, int> timesNamed)
    {
        var times = timesNamed.GetValueOrDefault(name) + 1;

        timesNamed[name] = times;

        return times == 1 ? name : name + " " + times.ToString(CultureInfo.InvariantCulture);
    }
}
