using System;
using System.Collections.Generic;

namespace InfoPack.Core;

/// <summary>
/// The brewing barrel's numbers as the game has them when the world loads (Furniture.GetEffectiveTierThresholds and
/// GetEffectiveSatietyPerBottle), brewing talents included.
/// </summary>
/// <param name="TierP1">The quality the middle drink starts at.</param>
/// <param name="TierP2">The quality the best drink starts at.</param>
/// <param name="SatietyPerBottle">The satiety one bottle takes.</param>
public sealed record BrewRules(float TierP1, float TierP2, float SatietyPerBottle)
{
    /// <summary>The game's own fallbacks, for when its settings cannot be read.</summary>
    public static readonly BrewRules Default = new(0f, 0.5f, 25f);
}

/// <summary>What one item does in a brewing barrel, worked out the way the game's brew preview works out a batch.</summary>
public static class BrewMix
{
    /// <summary>
    /// The satiety and mood one fresh item adds (its stats times its uses), and, when it adds satiety, the quality and
    /// drink a barrel of nothing else gives. The game counts no negative satiety, and quality is total mood over total
    /// satiety.
    /// </summary>
    public static ItemUse.Brew Of(ItemRow item, BrewRules rules, IReadOnlyList<int> products)
    {
        var uses = Math.Max(item.UseTimes, 1);
        var satiety = Math.Max(Stat(item, 0), 0) * uses;
        var mood = Stat(item, 1) * uses;

        if (satiety <= 0) return new ItemUse.Brew(satiety, mood, null);

        var quality = mood / satiety;
        var alone = new BrewAlone(quality, Product(quality, rules, products), ItemsPerBottle(satiety, rules.SatietyPerBottle));

        return new ItemUse.Brew(satiety, mood, alone);
    }

    /// <summary>The game ranks a mix below P1 lowest and at or above P2 highest, and a broken pair falls back to 0 and 0.5.</summary>
    private static int Product(double quality, BrewRules rules, IReadOnlyList<int> products)
    {
        var (low, high) = rules.TierP2 > rules.TierP1 ? (rules.TierP1, rules.TierP2) : (BrewRules.Default.TierP1, BrewRules.Default.TierP2);
        var tier = quality < low ? 0 : quality < high ? 1 : 2;

        if (products.Count == 0) return 0;

        return Math.Max(products[Math.Min(tier, products.Count - 1)], 0);
    }

    /// <summary>The fewest of the item whose satiety fills one bottle. A tiny margin keeps 25 / 12.5 from reading as 3.</summary>
    private static int ItemsPerBottle(double satiety, float perBottle)
    {
        var bottle = perBottle > 0 ? perBottle : BrewRules.Default.SatietyPerBottle;

        return Math.Max(1, (int)Math.Ceiling(bottle / satiety - 1e-6));
    }

    private static double Stat(ItemRow item, int index) => index < item.Stats.Count ? item.Stats[index] : 0;
}
