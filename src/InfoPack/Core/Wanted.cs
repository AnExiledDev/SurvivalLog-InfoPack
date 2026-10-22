using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace InfoPack.Core;

/// <summary>One thing a relief request asks for, as the save holds it (AidDemandSave).</summary>
/// <param name="Kind">0 asks for an item by id; any other kind asks for an amount of a category.</param>
/// <param name="Need">How many the request wants in all.</param>
/// <param name="Paid">How many have been handed in so far.</param>
public readonly record struct AidDemand(int Kind, int ItemId, int Need, int Paid)
{
    public const int ItemKind = 0;

    public int Left => Math.Max(0, Need - Paid);
}

/// <summary>
/// What the player is after right now: the food they crave, the items accepted relief requests still need, and the
/// food and drink they have never had. The page draws tooltip lines and the backpack star from it.
/// </summary>
public static class Wanted
{
    /// <summary>
    /// How many more of each item the requests still need. Only demands that name an item count; a demand for an amount
    /// of a category is left out, since nearly any item of that category would match it. An item that answers two
    /// demands needs the sum. matches(demandItemId, itemId) is the game's own rule for which items a demand takes.
    /// </summary>
    public static SortedDictionary<int, int> AidNeeds(IEnumerable<AidDemand> demands, IEnumerable<int> itemIds, Func<int, int, bool> matches)
    {
        var open = demands.Where(demand => demand.Kind == AidDemand.ItemKind && demand.ItemId > 0 && demand.Left > 0).ToList();
        var needs = new SortedDictionary<int, int>();

        if (open.Count == 0) return needs;

        foreach (var itemId in itemIds.Distinct())
        {
            var left = open.Where(demand => matches(demand.ItemId, itemId)).Sum(demand => demand.Left);

            if (left > 0) needs[itemId] = left;
        }

        return needs;
    }

    /// <summary>The food codex items the player has not unlocked by eating them, in ascending id order.</summary>
    public static SortedSet<int> Untried(IEnumerable<int> foodCodexItems, Func<int, bool> isTried) =>
        new(foodCodexItems.Where(id => !isTried(id)));

    /// <summary>{"wish":[id],"aid":{id:left},"untried":[id]}, ids ascending so the same state always gives the same text.</summary>
    public static string ToJson(IEnumerable<int> wish, IReadOnlyDictionary<int, int> aid, IEnumerable<int> untried) =>
        "{\"wish\":" + List(wish)
        + ",\"aid\":{" + string.Join(",", aid.OrderBy(pair => pair.Key).Select(pair => "\"" + Number(pair.Key) + "\":" + Number(pair.Value))) + "}"
        + ",\"untried\":" + List(untried)
        + "}";

    private static string List(IEnumerable<int> ids) => "[" + string.Join(",", ids.Distinct().OrderBy(id => id).Select(Number)) + "]";

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}
