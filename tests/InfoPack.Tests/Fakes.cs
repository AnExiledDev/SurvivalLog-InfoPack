using System.Collections.Generic;
using InfoPack.Core;

namespace InfoPack.Tests;

/// <summary>Plain-data stand-ins for the game's tables and text, so the rules run with no game in sight.</summary>
internal static class Rows
{
    public const int Food = 1;
    public const int Weapon = 2;
    public const int Electrical = 4;
    public const int DailyGoods = 5;
    public const int Material = 9;

    /// <summary>A shreddable material with no other use: the shape every scrap item has.</summary>
    public static ItemRow Junk(int id, int tradeValue = 1) =>
        new(id, Material, tradeValue, IsShreddable: true, IsEdible: false, IsUsable: false, IsBurnable: false, CanBrew: false, IsSeed: false, IsTrap: false, PlacesFurniture: false, Stats: new float[5]);

    /// <summary>A plain item the scrap rule never marks.</summary>
    public static ItemRow Plain(int id, int category = Material, int tradeValue = 1, params float[] stats) =>
        Junk(id, tradeValue) with { Category = category, IsShreddable = false, Stats = stats.Length == 0 ? new float[5] : stats };

    public static ItemRow Edible(int id, params float[] stats) => Plain(id, Food, 1, stats) with { IsEdible = true };

    public static GameTables Tables(
        IEnumerable<ItemRow> items,
        IEnumerable<ProductionRecipe>? production = null,
        IEnumerable<CookingRecipe>? cooking = null,
        IEnumerable<IReadOnlyList<int>>? brewProducts = null,
        IReadOnlyDictionary<int, IReadOnlyList<Yield>>? shredYields = null,
        IEnumerable<TalentRow>? talents = null,
        IReadOnlyDictionary<int, float>? buffDurations = null) =>
        new(
            new List<ItemRow>(items),
            new List<ProductionRecipe>(production ?? new ProductionRecipe[0]),
            new List<CookingRecipe>(cooking ?? new CookingRecipe[0]),
            new List<IReadOnlyList<int>>(brewProducts ?? new IReadOnlyList<int>[0]),
            shredYields ?? new Dictionary<int, IReadOnlyList<Yield>>(),
            new List<TalentRow>(talents ?? new TalentRow[0]),
            buffDurations ?? new Dictionary<int, float>());

    public static CookingRecipe Recipe(int id, int normal, int good = 0, int perfect = 0, int fail = 0, params int[] ingredients) =>
        new(id, ingredients, normal, good, perfect, fail);
}

internal sealed class FakeText : IGameText
{
    public static readonly StatFormat[] English =
    {
        new("Satiety +{0}", "Satiety {0}"),
        new("Mood +{0}", "Mood {0}"),
        new("Energy +{0}", "Energy {0}"),
        new("Health +{0}", "Health {0}"),
        new("Life +{0}", "Life {0}"),
    };

    private readonly Dictionary<int, string> _descriptions = new();

    public FakeText Describes(int itemId, string description)
    {
        _descriptions[itemId] = description;

        return this;
    }

    public IReadOnlyList<StatFormat> StatFormats() => English;

    public string? Description(int itemId) => _descriptions.TryGetValue(itemId, out var description) ? description : null;
}
