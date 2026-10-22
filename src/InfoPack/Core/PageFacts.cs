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
    /// <summary>
    /// {"stats":{id:text},"desc":{id:text},"dish":{recipeId:itemId},"brew":{id:[rank,of]},"scrap":[id],
    /// "plan":{talentId:badge},"items":{id:[name,icon]},"wiki":{id:{...}},"ui":{key:text}}, with a key left out when its
    /// piece is off. "desc", "items", "wiki" and "ui" are always there, because every tooltip draws on them. "ui" holds the
    /// page's own words in the language the game shows. Keys are in ascending id order so the same tables always give the
    /// same text.
    /// </summary>
    public static string Build(GameTables tables, IGameText text, Features features)
    {
        var parts = new List<string>();
        var wiki = ItemWiki.Build(tables, text);

        if (features.NeedsStats) parts.Add(Member("stats", TextObject(Stats(tables.Items, text.StatFormats()))));

        parts.Add(Member("desc", TextObject(Descriptions(tables.Items, text))));

        if (features.CookingCodex) parts.Add(Member("dish", NumberObject(Dishes(tables.Cooking))));
        if (features.AlcoholQuality) parts.Add(Member("brew", BrewObject(BrewTiers.Map(tables.BrewProducts))));
        if (features.ScrapOnly) parts.Add(Member("scrap", "[" + string.Join(",", ScrapRule.ScrapOnly(tables).Select(Number)) + "]"));
        if (features.PlanningBadges) parts.Add(Member("plan", TextObject(PlanBadges.Map(tables.Talents, tables.BuffDurations))));

        parts.Add(Member("items", WikiJson.Items(tables, text, wiki)));
        parts.Add(Member("wiki", WikiJson.Wiki(wiki)));
        parts.Add(Member("ui", Ui(text.IsChinese())));

        return "{" + string.Join(",", parts) + "}";
    }

    /// <summary>{key:text} for every phrase the page script shows, in English or Chinese.</summary>
    public static string Ui(bool isChinese) =>
        "{" + string.Join(",", Texts.ByPageKey.Select(pair => Member(pair.Key, JsText.Literal(pair.Value.Pick(isChinese))))) + "}";

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

    /// <summary>The description of every item that has one: the cooking window and every tooltip's about line show it.</summary>
    public static SortedDictionary<int, string> Descriptions(IEnumerable<ItemRow> items, IGameText text)
    {
        var descriptions = new SortedDictionary<int, string>();

        foreach (var item in items)
        {
            var description = item.Id > 0 ? text.Description(item.Id) : null;

            if (!string.IsNullOrWhiteSpace(description)) descriptions[item.Id] = description.Trim();
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
            if (recipe.Id > 0 && recipe.Dish > 0) dishes[recipe.Id] = recipe.Dish;
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
