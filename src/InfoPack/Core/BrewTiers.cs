using System;
using System.Collections.Generic;

namespace InfoPack.Core;

/// <summary>Where a brew product sits among what its barrel can make: Rank 1 is the lowest of Of.</summary>
public readonly record struct BrewTier(int Rank, int Of);

public static class BrewTiers
{
    /// <summary>The game's brew preview clamps its tier index to 0..2 and the brew panel draws three segments.</summary>
    public const int MaxTiers = 3;

    /// <summary>
    /// The tier of every brew product. Each barrel's furniture row lists its products lowest tier first (the barrel's
    /// RotProductOverride, which the game's brew preview indexes by tier). Only the first three can ever be brewed, so
    /// a longer list is cut there. A list of one is no ladder and is skipped, and a product two barrels rank
    /// differently is left out rather than shown with a tier that may be wrong.
    /// </summary>
    public static IReadOnlyDictionary<int, BrewTier> Map(IEnumerable<IReadOnlyList<int>> productLists)
    {
        var tiers = new Dictionary<int, BrewTier>();
        var conflicting = new HashSet<int>();

        foreach (var products in productLists)
        {
            var tierCount = Math.Min(products.Count, MaxTiers);

            if (tierCount < 2) continue;

            for (var i = 0; i < tierCount; i++)
            {
                var id = products[i];
                var tier = new BrewTier(i + 1, tierCount);

                if (id <= 0) continue;
                if (tiers.TryGetValue(id, out var known) && known != tier) conflicting.Add(id);

                tiers[id] = tier;
            }
        }

        foreach (var id in conflicting) tiers.Remove(id);

        return tiers;
    }
}
