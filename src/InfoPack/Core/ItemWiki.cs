using System;
using System.Collections.Generic;
using System.Linq;

namespace InfoPack.Core;

/// <summary>A count of one item, one line of what a recipe takes.</summary>
public readonly record struct Ingredient(int ConfigId, int Count);

/// <summary>Something the item goes into.</summary>
public abstract record ItemUse
{
    private ItemUse()
    {
    }

    /// <summary>A workbench recipe making Product out of Needs, the item among them.</summary>
    public sealed record Bench(int Product, IReadOnlyList<Ingredient> Needs) : ItemUse;

    /// <summary>A cooking recipe that names the item as an ingredient. Dish is the dish the recipe stands for.</summary>
    public sealed record Cook(int Dish) : ItemUse;

    /// <summary>The item can go in a brewing barrel.</summary>
    public sealed record Brew : ItemUse;
}

/// <summary>A way to get the item out of other items.</summary>
public abstract record ItemSource
{
    private ItemSource()
    {
    }

    /// <summary>A workbench recipe that makes the item.</summary>
    public sealed record Bench(IReadOnlyList<Ingredient> Needs) : ItemSource;

    /// <summary>A cooking recipe with the item as one of its results. Needs is empty for a recipe that goes by food tags.</summary>
    public sealed record Cook(IReadOnlyList<Ingredient> Needs, int Seconds) : ItemSource;

    /// <summary>Planting Seed and harvesting it.</summary>
    public sealed record Plant(int Seed) : ItemSource;

    /// <summary>Shredding From gives Count of the item.</summary>
    public sealed record Shred(int From, int Count) : ItemSource;
}

/// <summary>How a seed grows, from its plant table row.</summary>
public sealed record Growth(int Seconds, int HarvestSeconds, int LightNeed, int ColdResistance, IReadOnlyList<int> Gain, float SeedRate);

/// <summary>A furniture kind whose loot can hold the item, by name, and the names of the places it stands.</summary>
public sealed record LootSource(string Furniture, IReadOnlyList<string> Places);

/// <summary>
/// Everything the tooltip's wiki sections say about one item. A number of 0 and an empty list mean there is nothing
/// to say, so each field holds a value only where it applies: no shelf life for an item that never spoils, no burn
/// value for one that does not burn.
/// </summary>
/// <param name="Effects">Satiety, mood, energy, health and life to one decimal, or null when all of them are 0.</param>
public sealed record WikiEntry(
    IReadOnlyList<double>? Effects,
    int ShelfLifeDays,
    int RotProduct,
    int Weight,
    int TradeValue,
    IReadOnlyList<ItemUse> Uses,
    IReadOnlyList<ItemSource> Sources,
    IReadOnlyList<Yield> Shred,
    int CutProduct,
    int BurnValue,
    Growth? Growth,
    IReadOnlyList<LootSource> FoundIn)
{
    public bool IsEmpty =>
        Effects is null && ShelfLifeDays == 0 && RotProduct == 0 && Weight == 0 && TradeValue == 0 && Uses.Count == 0 &&
        Sources.Count == 0 && Shred.Count == 0 && CutProduct == 0 && BurnValue == 0 && Growth is null && FoundIn.Count == 0;

    /// <summary>Every other item the entry names, so the page can show their names and icons.</summary>
    public IEnumerable<int> ReferencedItems()
    {
        var ids = new List<int> { RotProduct, CutProduct };

        foreach (var use in Uses) ids.AddRange(Referenced(use));
        foreach (var source in Sources) ids.AddRange(Referenced(source));

        ids.AddRange(Shred.Select(shred => shred.ConfigId));

        if (Growth is not null) ids.AddRange(Growth.Gain);

        return ids.Where(id => id > 0);
    }

    private static IEnumerable<int> Referenced(ItemUse use) => use switch
    {
        ItemUse.Bench bench => bench.Needs.Select(need => need.ConfigId).Append(bench.Product),
        ItemUse.Cook cook => new[] { cook.Dish },
        ItemUse.Brew => Array.Empty<int>(),
        _ => throw new InvalidOperationException("unhandled item use " + use),
    };

    private static IEnumerable<int> Referenced(ItemSource source) => source switch
    {
        ItemSource.Bench bench => bench.Needs.Select(need => need.ConfigId),
        ItemSource.Cook cook => cook.Needs.Select(need => need.ConfigId),
        ItemSource.Plant plant => new[] { plant.Seed },
        ItemSource.Shred shred => new[] { shred.From },
        _ => throw new InvalidOperationException("unhandled item source " + source),
    };
}

/// <summary>
/// The mini wiki behind the item tooltips: for every item, what it does, how long it keeps, what it goes into, how to
/// get it and where it turns up as loot. Worked out once per loaded world from the tables.
/// </summary>
public static class ItemWiki
{
    /// <summary>At most this many loot sources per item, so a common material does not list every box in the game.</summary>
    public const int FoundInCap = 8;

    /// <summary>The entry of every item in the item table that has anything to say, in ascending id order.</summary>
    public static SortedDictionary<int, WikiEntry> Build(GameTables tables, IGameText text)
    {
        var items = FirstRowPerId(tables.Items);
        var uses = Uses(tables, items.Values);
        var sources = Sources(tables, items.Values);
        var foundIn = FoundIn(tables, text);
        var wiki = new SortedDictionary<int, WikiEntry>();

        foreach (var item in items.Values)
        {
            var entry = Entry(item, tables, uses, sources, foundIn);

            if (!entry.IsEmpty) wiki[item.Id] = entry;
        }

        return wiki;
    }

    /// <summary>A recipe's materials as one count per item, in ascending id order.</summary>
    public static IReadOnlyList<Ingredient> Fold(IEnumerable<int> materials) =>
        materials.Where(id => id > 0).GroupBy(id => id).OrderBy(group => group.Key).Select(group => new Ingredient(group.Key, group.Count())).ToList();

    private static SortedDictionary<int, ItemRow> FirstRowPerId(IEnumerable<ItemRow> rows)
    {
        var items = new SortedDictionary<int, ItemRow>();

        foreach (var row in rows)
        {
            if (row.Id > 0 && !items.ContainsKey(row.Id)) items[row.Id] = row;
        }

        return items;
    }

    private static WikiEntry Entry(
        ItemRow item,
        GameTables tables,
        IReadOnlyDictionary<int, List<ItemUse>> uses,
        IReadOnlyDictionary<int, List<ItemSource>> sources,
        IReadOnlyDictionary<int, List<LootSource>> foundIn)
    {
        var spoils = item.ShelfLifeDays > 0;

        return new WikiEntry(
            Effects(item.Stats),
            spoils ? item.ShelfLifeDays : 0,
            spoils ? Math.Max(item.RotProduct, 0) : 0,
            Math.Max(item.Weight, 0),
            item.TradeValue,
            Ordered(Find(uses, item.Id), CompareUses),
            Ordered(Find(sources, item.Id), CompareSources),
            ShredYields(tables, item.Id),
            Math.Max(item.CutProduct, 0),
            item.IsBurnable ? Math.Max(item.BurnValue, 0) : 0,
            Grow(item, tables.Plants),
            Find(foundIn, item.Id));
    }

    /// <summary>Rounded the way the stat line rounds them, so the two never disagree. Always five, so the page can index them.</summary>
    private static IReadOnlyList<double>? Effects(IReadOnlyList<float> stats)
    {
        var effects = stats.Concat(Enumerable.Repeat(0f, 5)).Take(5).Select(value => Math.Round(value, 1)).Select(value => value == 0 ? 0 : value).ToList();

        return effects.Any(value => value != 0) ? effects : null;
    }

    /// <summary>A seed whose plant row is missing grows nothing the wiki can describe.</summary>
    private static Growth? Grow(ItemRow item, IReadOnlyDictionary<int, PlantRow> plants)
    {
        if (item.PlantId <= 0 || !plants.TryGetValue(item.PlantId, out var plant)) return null;

        return new Growth(plant.GrowthTime, plant.HarvestTime, plant.LightNeed, plant.ColdResistance, plant.Gain.Where(id => id > 0).Distinct().ToList(), plant.SeedRate);
    }

    private static IReadOnlyList<Yield> ShredYields(GameTables tables, int itemId) =>
        tables.ShredYields.TryGetValue(itemId, out var yields) ? yields.Where(shred => shred.ConfigId > 0 && shred.Count > 0).ToList() : new List<Yield>();

    /// <summary>The recipes the workbench shows. Shred recipes are left out: breaking an item down is not using it.</summary>
    private static IEnumerable<ProductionRecipe> WorkbenchRecipes(IEnumerable<ProductionRecipe> production) =>
        production.Where(recipe => recipe.IsListed && !recipe.IsShredOnly && recipe.Product > 0 && recipe.Materials.Any(id => id > 0));

    private static Dictionary<int, List<ItemUse>> Uses(GameTables tables, IEnumerable<ItemRow> items)
    {
        var uses = new Dictionary<int, List<ItemUse>>();

        foreach (var recipe in WorkbenchRecipes(tables.Production))
        {
            var needs = Fold(recipe.Materials);

            foreach (var need in needs) Add(uses, need.ConfigId, new ItemUse.Bench(recipe.Product, needs));
        }

        foreach (var recipe in tables.Cooking)
        {
            if (recipe.Dish <= 0) continue;

            foreach (var ingredient in recipe.SpecificItems.Distinct()) Add(uses, ingredient, new ItemUse.Cook(recipe.Dish));
        }

        foreach (var item in items)
        {
            if (item.CanBrew) Add(uses, item.Id, new ItemUse.Brew());
        }

        return uses;
    }

    private static Dictionary<int, List<ItemSource>> Sources(GameTables tables, IEnumerable<ItemRow> items)
    {
        var sources = new Dictionary<int, List<ItemSource>>();

        foreach (var recipe in WorkbenchRecipes(tables.Production)) Add(sources, recipe.Product, new ItemSource.Bench(Fold(recipe.Materials)));

        foreach (var recipe in tables.Cooking)
        {
            var cooked = new ItemSource.Cook(Fold(recipe.SpecificItems), Math.Max(recipe.CookTime, 0));

            foreach (var dish in recipe.Results.Distinct()) Add(sources, dish, cooked);
        }

        foreach (var seed in items)
        {
            var growth = Grow(seed, tables.Plants);

            // A plant whose harvest gives its own seed back would read as "harvest of itself".
            foreach (var gain in growth?.Gain ?? Array.Empty<int>())
            {
                if (gain != seed.Id) Add(sources, gain, new ItemSource.Plant(seed.Id));
            }
        }

        foreach (var pair in tables.ShredYields)
        {
            foreach (var shred in pair.Value)
            {
                if (shred.ConfigId > 0 && shred.Count > 0) Add(sources, shred.ConfigId, new ItemSource.Shred(pair.Key, shred.Count));
            }
        }

        return sources;
    }

    /// <summary>
    /// The loot sources of every item. Furniture rows sharing a name (the game has many rows per kind) read as one
    /// source, placed wherever the rows that drop the item stand, and an item a row can drop from several groups counts
    /// once. The sources standing in the most places come first, then the lowest furniture id, so the same tables always
    /// pick the same eight.
    /// </summary>
    private static Dictionary<int, List<LootSource>> FoundIn(GameTables tables, IGameText text)
    {
        var placesByFurniture = PlacesByFurniture(tables.Placements);
        var kindsByItem = new Dictionary<int, Dictionary<string, LootKind>>();

        foreach (var furniture in tables.LootFurniture.OrderBy(furniture => furniture.Id))
        {
            var name = Trimmed(text.FurnitureName(furniture.Id));

            if (name is null) continue;

            var places = placesByFurniture.TryGetValue(furniture.Id, out var placeIds) ? placeIds : new SortedSet<int>();

            foreach (var drop in Drops(furniture, tables.LootGroups))
            {
                if (!kindsByItem.TryGetValue(drop, out var kinds)) kindsByItem[drop] = kinds = new Dictionary<string, LootKind>();

                if (!kinds.TryGetValue(name, out var kind)) kinds[name] = kind = new LootKind(furniture.Id);

                kind.PlaceIds.UnionWith(places);
            }
        }

        var placeNames = new Dictionary<int, string?>();

        return kindsByItem.ToDictionary(
            pair => pair.Key,
            pair => pair.Value
                .Select(kind => (Kind: kind.Value, Source: new LootSource(kind.Key, PlaceNames(kind.Value.PlaceIds, text, placeNames))))
                .OrderByDescending(found => found.Source.Places.Count)
                .ThenBy(found => found.Kind.FirstFurnitureId)
                .Take(FoundInCap)
                .Select(found => found.Source)
                .ToList());
    }

    private static HashSet<int> Drops(LootFurniture furniture, IReadOnlyDictionary<int, IReadOnlyList<int>> lootGroups)
    {
        var drops = new HashSet<int>();

        foreach (var group in furniture.Groups)
        {
            if (lootGroups.TryGetValue(group, out var items)) drops.UnionWith(items.Where(id => id > 0));
        }

        return drops;
    }

    private static Dictionary<int, SortedSet<int>> PlacesByFurniture(IEnumerable<Placement> placements)
    {
        var places = new Dictionary<int, SortedSet<int>>();

        foreach (var placement in placements)
        {
            if (placement.PlaceId <= 0) continue;

            if (!places.TryGetValue(placement.FurnitureId, out var ids)) places[placement.FurnitureId] = ids = new SortedSet<int>();

            ids.Add(placement.PlaceId);
        }

        return places;
    }

    /// <summary>In place id order. Two places with one name (floors of one building) show once.</summary>
    private static IReadOnlyList<string> PlaceNames(IEnumerable<int> placeIds, IGameText text, Dictionary<int, string?> cache)
    {
        var names = new List<string>();

        foreach (var id in placeIds)
        {
            if (!cache.TryGetValue(id, out var name)) cache[id] = name = Trimmed(text.PlaceName(id));

            if (name is not null && !names.Contains(name)) names.Add(name);
        }

        return names;
    }

    private static string? Trimmed(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private static void Add<T>(Dictionary<int, List<T>> lists, int id, T value)
    {
        if (id <= 0) return;

        if (!lists.TryGetValue(id, out var list)) lists[id] = list = new List<T>();

        list.Add(value);
    }

    private static IReadOnlyList<T> Find<T>(IReadOnlyDictionary<int, List<T>> lists, int id) =>
        lists.TryGetValue(id, out var list) ? list : new List<T>();

    /// <summary>Sorted, with exact repeats (two recipe rows that read the same) shown once.</summary>
    private static IReadOnlyList<T> Ordered<T>(IEnumerable<T> values, Comparison<T> compare)
    {
        var sorted = values.ToList();
        var ordered = new List<T>(sorted.Count);

        sorted.Sort(compare);

        foreach (var value in sorted)
        {
            if (ordered.Count == 0 || compare(ordered[ordered.Count - 1], value) != 0) ordered.Add(value);
        }

        return ordered;
    }

    /// <summary>Workbench recipes first, then cooking, then brewing; within a kind by what comes out.</summary>
    private static int CompareUses(ItemUse left, ItemUse right) => (left, right) switch
    {
        (ItemUse.Bench a, ItemUse.Bench b) => Then(a.Product.CompareTo(b.Product), CompareNeeds(a.Needs, b.Needs)),
        (ItemUse.Cook a, ItemUse.Cook b) => a.Dish.CompareTo(b.Dish),
        _ => Rank(left).CompareTo(Rank(right)),
    };

    /// <summary>Workbench, cooking, harvest, shredding; within a kind by what goes in.</summary>
    private static int CompareSources(ItemSource left, ItemSource right) => (left, right) switch
    {
        (ItemSource.Bench a, ItemSource.Bench b) => CompareNeeds(a.Needs, b.Needs),
        (ItemSource.Cook a, ItemSource.Cook b) => Then(CompareNeeds(a.Needs, b.Needs), a.Seconds.CompareTo(b.Seconds)),
        (ItemSource.Plant a, ItemSource.Plant b) => a.Seed.CompareTo(b.Seed),
        (ItemSource.Shred a, ItemSource.Shred b) => Then(a.From.CompareTo(b.From), a.Count.CompareTo(b.Count)),
        _ => Rank(left).CompareTo(Rank(right)),
    };

    private static int Rank(ItemUse use) => use switch
    {
        ItemUse.Bench => 0,
        ItemUse.Cook => 1,
        ItemUse.Brew => 2,
        _ => throw new InvalidOperationException("unhandled item use " + use),
    };

    private static int Rank(ItemSource source) => source switch
    {
        ItemSource.Bench => 0,
        ItemSource.Cook => 1,
        ItemSource.Plant => 2,
        ItemSource.Shred => 3,
        _ => throw new InvalidOperationException("unhandled item source " + source),
    };

    private static int CompareNeeds(IReadOnlyList<Ingredient> left, IReadOnlyList<Ingredient> right)
    {
        for (var i = 0; i < Math.Min(left.Count, right.Count); i++)
        {
            var order = Then(left[i].ConfigId.CompareTo(right[i].ConfigId), left[i].Count.CompareTo(right[i].Count));

            if (order != 0) return order;
        }

        return left.Count.CompareTo(right.Count);
    }

    private static int Then(int first, int second) => first != 0 ? first : second;

    /// <summary>The furniture rows of one name that drop one item, while the sources are gathered.</summary>
    private sealed class LootKind
    {
        public LootKind(int firstFurnitureId)
        {
            FirstFurnitureId = firstFurnitureId;
        }

        public int FirstFurnitureId { get; }

        public SortedSet<int> PlaceIds { get; } = new();
    }
}
