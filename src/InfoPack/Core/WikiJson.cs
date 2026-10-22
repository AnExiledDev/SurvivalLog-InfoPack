using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace InfoPack.Core;

/// <summary>
/// The "items" and "wiki" members of the facts. A field with nothing to say is left out rather than sent empty, so the
/// thousands of entries stay small, and lists are plain arrays ([id,count]) rather than objects.
/// </summary>
public static class WikiJson
{
    /// <summary>
    /// {id:[name,icon]} for every item in the item table that has an entry or that an entry names, in ascending id
    /// order. A name the game lacks is sent as "" so the page can still find the icon.
    /// </summary>
    public static string Items(GameTables tables, IGameText text, SortedDictionary<int, WikiEntry> wiki)
    {
        var icons = new Dictionary<int, string>();

        foreach (var item in tables.Items)
        {
            if (item.Id > 0 && !icons.ContainsKey(item.Id)) icons[item.Id] = item.Icon;
        }

        var named = new SortedSet<int>(wiki.Keys.Concat(wiki.Values.SelectMany(entry => entry.ReferencedItems())).Where(icons.ContainsKey));

        return "{" + string.Join(",", named.Select(id => Key(id) + ":[" + JsText.Literal(Trimmed(text.ItemName(id))) + "," + JsText.Literal(icons[id]) + "]")) + "}";
    }

    /// <summary>{id:{...}} in ascending id order, each entry holding only the fields that apply to it.</summary>
    public static string Wiki(SortedDictionary<int, WikiEntry> wiki) =>
        "{" + string.Join(",", wiki.Select(pair => Key(pair.Key) + ":" + Entry(pair.Value))) + "}";

    private static string Entry(WikiEntry entry)
    {
        var fields = new List<string>();

        if (entry.Effects is not null) fields.Add(Field("fx", JsonArray(entry.Effects.Select(OneDecimal))));
        if (entry.ShelfLifeDays > 0) fields.Add(Field("life", Number(entry.ShelfLifeDays)));
        if (entry.RotProduct > 0) fields.Add(Field("rot", Number(entry.RotProduct)));
        if (entry.Weight > 0) fields.Add(Field("wt", Number(entry.Weight)));
        if (entry.TradeValue != 0) fields.Add(Field("tv", Number(entry.TradeValue)));
        if (entry.Uses.Count > 0) fields.Add(Field("use", JsonArray(entry.Uses.Select(Use))));
        if (entry.Sources.Count > 0) fields.Add(Field("make", JsonArray(entry.Sources.Select(Source))));
        if (entry.Shred.Count > 0) fields.Add(Field("shred", JsonArray(entry.Shred.Select(shred => Pair(shred.ConfigId, shred.Count)))));
        if (entry.CutProduct > 0) fields.Add(Field("cut", Number(entry.CutProduct)));
        if (entry.BurnValue > 0) fields.Add(Field("burn", Number(entry.BurnValue)));
        if (entry.Burn is not null) fields.Add(Field("bt", Burned(entry.Burn)));
        if (entry.Growth is not null) fields.Add(Field("grow", Grow(entry.Growth)));
        if (entry.FoundIn.Count > 0) fields.Add(Field("found", JsonArray(entry.FoundIn.Select(Found))));

        return JsonObject(fields);
    }

    private static string Use(ItemUse use) => use switch
    {
        ItemUse.Bench bench => JsonObject(new[] { Kind("bench"), Field("out", Number(bench.Product)), Field("need", Needs(bench.Needs)) }),
        ItemUse.Cook cook => JsonObject(cook.Seconds > 0
            ? new[] { Kind("cook"), Field("out", Number(cook.Dish)), Field("t", Number(cook.Seconds)) }
            : new[] { Kind("cook"), Field("out", Number(cook.Dish)) }),
        ItemUse.Brew brew => Brewed(brew),
        _ => throw new InvalidOperationException("unhandled item use " + use),
    };

    private static string Source(ItemSource source) => source switch
    {
        ItemSource.Bench bench => JsonObject(new[] { Kind("bench"), Field("need", Needs(bench.Needs)) }),
        ItemSource.Cook cook => Cooked(cook),
        ItemSource.Plant plant => JsonObject(new[] { Kind("plant"), Field("from", Number(plant.Seed)) }),
        ItemSource.Shred shred => JsonObject(new[] { Kind("shred"), Field("from", Number(shred.From)), Field("n", Number(shred.Count)) }),
        _ => throw new InvalidOperationException("unhandled item source " + source),
    };

    /// <summary>Satiety and mood always; quality, drink and items per bottle only for an item that adds satiety.</summary>
    private static string Brewed(ItemUse.Brew brew)
    {
        var fields = new List<string> { Kind("brew"), Field("s", Rounded(brew.Satiety, 1)), Field("m", Rounded(brew.Mood, 1)) };

        if (brew.Alone is not null)
        {
            fields.Add(Field("q", Rounded(brew.Alone.Quality, 2)));
            fields.Add(Field("n", Number(brew.Alone.ItemsPerBottle)));

            if (brew.Alone.Product > 0) fields.Add(Field("out", Number(brew.Alone.Product)));
        }

        return JsonObject(fields);
    }

    /// <summary>{c,h,g}: [shortest, longest] game seconds in a cooking pot, a heater and a generator, each only where there is one.</summary>
    private static string Burned(BurnTimes burn)
    {
        var fields = new List<string>();

        if (burn.Cooking is not null) fields.Add(Field("c", Span(burn.Cooking)));
        if (burn.Heater is not null) fields.Add(Field("h", Span(burn.Heater)));
        if (burn.Generator is not null) fields.Add(Field("g", Span(burn.Generator)));

        return JsonObject(fields);
    }

    private static string Span(BurnSpan span) => Pair(span.Shortest, span.Longest);

    /// <summary>A recipe that goes by food tags names no ingredients, and one without a cook time gives none.</summary>
    private static string Cooked(ItemSource.Cook cook)
    {
        var fields = new List<string> { Kind("cook") };

        if (cook.Needs.Count > 0) fields.Add(Field("need", Needs(cook.Needs)));
        if (cook.Seconds > 0) fields.Add(Field("t", Number(cook.Seconds)));

        return JsonObject(fields);
    }

    /// <summary>Every number of a plant row means something at 0 (0 light needed, no cold taken), so only gain is optional.</summary>
    private static string Grow(Growth growth)
    {
        var fields = new List<string>
        {
            Field("t", Number(growth.Seconds)),
            Field("h", Number(growth.HarvestSeconds)),
            Field("light", Number(growth.LightNeed)),
            Field("cold", Number(growth.ColdResistance)),
        };

        if (growth.Gain.Count > 0) fields.Add(Field("gain", JsonArray(growth.Gain.Select(Number))));

        fields.Add(Field("seed", Rate(growth.SeedRate)));

        return JsonObject(fields);
    }

    private static string Found(LootSource found)
    {
        var fields = new List<string> { Field("src", JsText.Literal(found.Furniture)) };

        if (found.Places.Count > 0) fields.Add(Field("at", JsonArray(found.Places.Select(JsText.Literal))));

        return JsonObject(fields);
    }

    private static string Needs(IEnumerable<Ingredient> needs) => JsonArray(needs.Select(need => Pair(need.ConfigId, need.Count)));

    private static string Pair(int id, int count) => "[" + Number(id) + "," + Number(count) + "]";

    private static string Kind(string kind) => Field("k", JsText.Literal(kind));

    private static string Field(string key, string value) => JsText.Literal(key) + ":" + value;

    private static string JsonObject(IEnumerable<string> fields) => "{" + string.Join(",", fields) + "}";

    private static string JsonArray(IEnumerable<string> values) => "[" + string.Join(",", values) + "]";

    private static string Key(int id) => "\"" + Number(id) + "\"";

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>Already rounded to one decimal; written without a trailing ".0" the way JSON numbers usually read.</summary>
    private static string OneDecimal(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);

    /// <summary>Rounded to the given decimals, with no "-0" for a value that rounds to nothing.</summary>
    private static string Rounded(double value, int decimals)
    {
        var rounded = Math.Round(value, decimals, MidpointRounding.AwayFromZero);

        return (rounded == 0 ? 0 : rounded).ToString("0.##", CultureInfo.InvariantCulture);
    }

    /// <summary>A float of 0.3 reads back as 0.30000001; three decimals keep the table's own value.</summary>
    private static string Rate(float rate)
    {
        var rounded = Math.Round(rate, 3);

        return (rounded == 0 ? 0 : rounded).ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string Trimmed(string? text) => string.IsNullOrWhiteSpace(text) ? "" : text.Trim();
}
