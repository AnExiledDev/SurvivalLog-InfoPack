using System.Collections.Generic;
using System.Linq;

namespace InfoPack.Core;

/// <summary>
/// Which items are only good for scrap: worth breaking down and nothing else. The bar is "no recipe, food or trade
/// use", and the rule leans towards leaving an item unmarked, because a wrong mark tells the player to throw away
/// something they need.
/// </summary>
public static class ScrapRule
{
    private const int CategoryNone = 0;
    private const int CategoryElectrical = 4;
    private const int CategoryDailyGoods = 5;
    private const int CategoryMaterial = 9;

    /// <summary>Every item the scrap mark goes on.</summary>
    public static SortedSet<int> ScrapOnly(GameTables tables)
    {
        var materials = RecipeMaterials(tables.Production, tables.Cooking);
        var tradeValues = tables.Items.GroupBy(item => item.Id).ToDictionary(group => group.Key, group => group.First().TradeValue);
        var marked = new SortedSet<int>();

        foreach (var item in tables.Items)
        {
            var yields = tables.ShredYields.TryGetValue(item.Id, out var found) ? found : new List<Yield>();

            if (IsScrapOnly(item, materials.Contains(item.Id), ScrapValue(yields, tradeValues))) marked.Add(item.Id);
        }

        return marked;
    }

    /// <summary>
    /// The rule for one item. It has to be shreddable, used by no recipe, one of the junk categories (none, electrical,
    /// daily goods, material), and of no other use: not food, not usable, not fuel, not brewable, not a seed, trap or
    /// furniture. And it may not be worth more in trade than what shredding it gives back.
    /// </summary>
    public static bool IsScrapOnly(ItemRow item, bool isRecipeMaterial, int scrapValue)
    {
        if (!item.IsShreddable || isRecipeMaterial) return false;
        if (item.Category is not (CategoryNone or CategoryElectrical or CategoryDailyGoods or CategoryMaterial)) return false;
        if (item.IsEdible || item.IsUsable || item.IsBurnable || item.CanBrew) return false;
        if (item.IsSeed || item.IsTrap || item.PlacesFurniture) return false;

        return item.TradeValue <= scrapValue;
    }

    /// <summary>
    /// Every item a recipe takes: workbench materials and named cooking ingredients. Shred recipes are left out, since
    /// breaking an item down is the one use a scrap item is allowed.
    /// </summary>
    public static HashSet<int> RecipeMaterials(IEnumerable<ProductionRecipe> production, IEnumerable<CookingRecipe> cooking)
    {
        var materials = new HashSet<int>();

        foreach (var recipe in production)
        {
            if (!recipe.IsShredOnly) materials.UnionWith(recipe.Materials);
        }

        foreach (var recipe in cooking) materials.UnionWith(recipe.SpecificItems);

        return materials;
    }

    /// <summary>What shredding gives back, in trade value. A yield the item table does not know is worth nothing.</summary>
    public static int ScrapValue(IReadOnlyList<Yield> yields, IReadOnlyDictionary<int, int> tradeValues)
    {
        var total = 0;

        foreach (var shred in yields)
        {
            if (shred.Count > 0 && tradeValues.TryGetValue(shred.ConfigId, out var value)) total += value * shred.Count;
        }

        return total;
    }
}
