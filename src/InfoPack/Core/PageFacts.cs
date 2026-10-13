using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace InfoPack.Core;

/// <summary>
/// The JSON the page script draws from. The facts are fixed for a loaded world (stat lines, descriptions, which dish a
/// recipe makes, brew tiers, the scrap list, planning card badges) and only the pieces the player left on are worked
/// out. The home counts change as the player plays and go in their own message.
/// </summary>
public static class PageFacts
{
    private const int CategoryFood = 1;

    /// <summary>
    /// {"stats":{id:text},"desc":{id:text},"dish":{recipeId:itemId},"brew":{id:[rank,of]},"scrap":[id],
    /// "plan":{talentId:badge}}, with a key left out when its piece is off. Keys are in ascending id order so the same
    /// tables always give the same text.
    /// </summary>
    public static string Build(GameTables tables, IGameText text, Features features)
    {
        var parts = new List<string>();

        if (features.NeedsStats) parts.Add(Member("stats", TextObject(Stats(tables.Items, text.StatFormats()))));

        if (features.CookingCodex)
        {
            parts.Add(Member("desc", TextObject(Descriptions(tables, text))));
            parts.Add(Member("dish", NumberObject(Dishes(tables.Cooking))));
        }

        if (features.AlcoholQuality) parts.Add(Member("brew", BrewObject(BrewTiers.Map(tables.BrewProducts))));
        if (features.ScrapOnly) parts.Add(Member("scrap", "[" + string.Join(",", ScrapRule.ScrapOnly(tables).Select(Number)) + "]"));
        if (features.PlanningBadges) parts.Add(Member("plan", TextObject(PlanBadges.Map(tables.Talents, tables.BuffDurations))));

        return "{" + string.Join(",", parts) + "}";
    }

    /// <summary>{configId:count} in ascending id order, so equal counts always give equal text.</summary>
    public static string Counts(IReadOnlyDictionary<int, int> counts) =>
        NumberObject(new SortedDictionary<int, int>(counts.ToDictionary(pair => pair.Key, pair => pair.Value)));

    /// <summary>The stat line of every item that changes a stat.</summary>
    public static SortedDictionary<int, string> Stats(IEnumerable<ItemRow> items, IReadOnlyList<StatFormat> formats)
    {
        var lines = new SortedDictionary<int, string>();

        foreach (var item in items)
        {
            var line = StatLine.Describe(formats, item.Stats);

            if (line is not null) lines[item.Id] = line;
        }

        return lines;
    }

    /// <summary>
    /// The description of every item the cooking window can show: food, anything the pot takes, every named ingredient
    /// and every dish. The rest of the item table is left out to keep the message small.
    /// </summary>
    public static SortedDictionary<int, string> Descriptions(GameTables tables, IGameText text)
    {
        var cookingIds = new HashSet<int>(tables.Items.Where(item => item.Category == CategoryFood || item.IsEdible).Select(item => item.Id));
        var descriptions = new SortedDictionary<int, string>();

        foreach (var recipe in tables.Cooking)
        {
            cookingIds.UnionWith(recipe.SpecificItems);
            cookingIds.UnionWith(new[] { recipe.NormalItemId, recipe.GoodItemId, recipe.PerfectItemId, recipe.FailItemId });
        }

        foreach (var id in cookingIds)
        {
            var description = id > 0 ? text.Description(id) : null;

            if (!string.IsNullOrWhiteSpace(description)) descriptions[id] = description.Trim();
        }

        return descriptions;
    }

    /// <summary>
    /// The dish each cooking recipe stands for: the normal result, or the next result the recipe names when it has no
    /// normal one. A recipe that names no result is left out.
    /// </summary>
    public static SortedDictionary<int, int> Dishes(IEnumerable<CookingRecipe> recipes)
    {
        var dishes = new SortedDictionary<int, int>();

        foreach (var recipe in recipes)
        {
            var dish = new[] { recipe.NormalItemId, recipe.GoodItemId, recipe.PerfectItemId, recipe.FailItemId }.FirstOrDefault(id => id > 0);

            if (recipe.Id > 0 && dish > 0) dishes[recipe.Id] = dish;
        }

        return dishes;
    }

    private static string Member(string key, string value) => JsText.Literal(key) + ":" + value;

    private static string TextObject(SortedDictionary<int, string> values) =>
        "{" + string.Join(",", values.Select(pair => Key(pair.Key) + ":" + JsText.Literal(pair.Value))) + "}";

    private static string NumberObject(SortedDictionary<int, int> values) =>
        "{" + string.Join(",", values.Select(pair => Key(pair.Key) + ":" + Number(pair.Value))) + "}";

    private static string BrewObject(IReadOnlyDictionary<int, BrewTier> tiers)
    {
        var json = new StringBuilder("{");

        foreach (var pair in tiers.OrderBy(pair => pair.Key))
        {
            if (json.Length > 1) json.Append(',');

            json.Append(Key(pair.Key)).Append(":[").Append(Number(pair.Value.Rank)).Append(',').Append(Number(pair.Value.Of)).Append(']');
        }

        return json.Append('}').ToString();
    }

    private static string Key(int id) => "\"" + Number(id) + "\"";

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}
