using System.Collections.Generic;
using System.Linq;

namespace InfoPack.Core;

/// <summary>
/// What the facts are worked out from: the game's config tables, read once per loaded world and copied into plain
/// data so nothing below touches the game.
/// </summary>
/// <param name="Plants">The plant table by plant id, what a seed's Config_Item.Plant points at.</param>
/// <param name="LootFurniture">Every furniture kind with a loot group.</param>
/// <param name="LootGroups">The item ids each loot group (Config_RandomGroup) can drop, by group id.</param>
/// <param name="Placements">Where the game sets furniture down (Config_GuidanceFurniture), one row per placement.</param>
public sealed record GameTables(
    IReadOnlyList<ItemRow> Items,
    IReadOnlyList<ProductionRecipe> Production,
    IReadOnlyList<CookingRecipe> Cooking,
    IReadOnlyList<IReadOnlyList<int>> BrewProducts,
    IReadOnlyDictionary<int, IReadOnlyList<Yield>> ShredYields,
    IReadOnlyList<TalentRow> Talents,
    IReadOnlyDictionary<int, float> BuffDurations,
    IReadOnlyDictionary<int, PlantRow> Plants,
    IReadOnlyList<LootFurniture> LootFurniture,
    IReadOnlyDictionary<int, IReadOnlyList<int>> LootGroups,
    IReadOnlyList<Placement> Placements)
{
    /// <summary>
    /// Every item a relief request can ask for by name (Config_AidCommission demands and Config_AidDemandPool rows of
    /// kind 0). Such an item has a use, so it never gets the scrap mark.
    /// </summary>
    public IReadOnlyCollection<int> AidDemandItems { get; init; } = new HashSet<int>();

    /// <summary>The bait kind's text key (Config_TrapBait.Effect_Des) for every item a trap takes as bait.</summary>
    public IReadOnlyDictionary<int, string> TrapBait { get; init; } = new Dictionary<int, string>();

    /// <summary>The level of every item that is a robot module (Config_RobotModule).</summary>
    public IReadOnlyDictionary<int, int> RobotModules { get; init; } = new Dictionary<int, int>();

    /// <summary>The brewing barrel's quality thresholds and bottle size.</summary>
    public BrewRules Brewing { get; init; } = BrewRules.Default;

    /// <summary>How fast the cooking pots, heaters and generators use fuel.</summary>
    public FuelRates Fuel { get; init; } = FuelRates.None;
}

/// <summary>One row of the item table, reduced to what the pieces ask about.</summary>
/// <param name="Category">The game's ItemCategory number (0 None, 1 Food, 4 Electrical, 5 Daily goods, 9 Material).</param>
/// <param name="IsShreddable">
/// The game's own IsShredableItem: an id of 20000 or more that is not the tower repair part and not blocked in the demo,
/// with a shred recipe, and either a material (category 9) or with a shred recipe whose NoteCategory is 9.
/// </param>
/// <param name="IsEdible">CanCook or any food tag: the cooking pot takes it.</param>
/// <param name="IsUsable">The item has a use action the player can trigger (CantUse is off).</param>
/// <param name="IsSeed">The item plants something.</param>
/// <param name="PlacesFurniture">Using the item places a piece of furniture.</param>
/// <param name="Stats">ValueDisplay1 to 5: satiety, mood, energy, health, life.</param>
/// <param name="Weight">Grams (Config_Item.weight).</param>
/// <param name="ShelfLifeDays">
/// Config_Item.Life: the days a fresh item keeps. The game only spoils an item whose Life is above 0
/// (ItemManager.GetRemainingShelfDays), so 0 or below means it never spoils.
/// </param>
/// <param name="RotProduct">The first entry of It_Rot_Product_Id, what the item turns into when it spoils; 0 for none.</param>
/// <param name="BurnValue">How much fuel the item gives; it only counts when IsBurnable.</param>
/// <param name="CutProduct">CutProductId, 0 for none.</param>
/// <param name="PlantId">Config_Item.Plant, the plant table row a seed grows; 0 for anything that is not a seed.</param>
/// <param name="Icon">WebIcon as the game writes it, a path the page can use as an image; empty for none.</param>
/// <param name="UseTimes">Config_Item.UseTimes, how many uses a fresh item holds; the brew counts 0 or less as 1.</param>
/// <param name="InFoodCodex">
/// The item has a Food entry in the codex (CodexManager.IsInCodex: category 1 with InCodex set). Eating it unlocks the
/// entry, which is how the game remembers what the player has tried.
/// </param>
public sealed record ItemRow(
    int Id,
    int Category,
    int TradeValue,
    bool IsShreddable,
    bool IsEdible,
    bool IsUsable,
    bool IsBurnable,
    bool CanBrew,
    bool IsSeed,
    bool IsTrap,
    bool PlacesFurniture,
    IReadOnlyList<float> Stats,
    int Weight = 0,
    int ShelfLifeDays = 0,
    int RotProduct = 0,
    int BurnValue = 0,
    int CutProduct = 0,
    int PlantId = 0,
    string Icon = "",
    bool InFoodCodex = false,
    int UseTimes = 1);

/// <summary>A workbench recipe. MaterialList names one item id per unit, so an id can repeat.</summary>
/// <param name="IsShredOnly">The game's IsShredOnlyRecipe: the recipe breaks its material down rather than using it.</param>
/// <param name="Product">The first entry of ProductID, the item the recipe makes; 0 for none.</param>
/// <param name="IsListed">IsUseable: the game's workbench only lists a recipe that has it (ProficiencyManager.IsRecipeListable).</param>
public sealed record ProductionRecipe(IReadOnlyList<int> Materials, bool IsShredOnly, int Product = 0, bool IsListed = true);

/// <summary>A cooking recipe: the ingredients it asks for by name and the dish each result quality gives.</summary>
/// <param name="CookTime">Game seconds the dish takes to cook.</param>
public sealed record CookingRecipe(
    int Id,
    IReadOnlyList<int> SpecificItems,
    int NormalItemId,
    int GoodItemId,
    int PerfectItemId,
    int FailItemId,
    int CookTime = 0)
{
    /// <summary>Every dish the recipe can give: normal, good, perfect and failed, the ones it names.</summary>
    public IEnumerable<int> Results => new[] { NormalItemId, GoodItemId, PerfectItemId, FailItemId }.Where(id => id > 0);

    /// <summary>The dish the recipe stands for: the normal result, or the next result it names; 0 when it names none.</summary>
    public int Dish => Results.FirstOrDefault();
}

/// <summary>One row of the talent table. Survival Planning cards are the rows of Type 2.</summary>
/// <param name="BuffId">The buff the talent gives, 0 for none. BuffDurations has that buff's BuffDuring.</param>
public sealed record TalentRow(int Id, int Type, int BuffId);

/// <summary>One line of what shredding an item gives back.</summary>
public readonly record struct Yield(int ConfigId, int Count);

/// <summary>One row of the plant table.</summary>
/// <param name="GrowthTime">Game seconds from planting to ripe.</param>
/// <param name="HarvestTime">Game seconds a ripe plant waits to be harvested.</param>
/// <param name="LightNeed">
/// The light the plant needs, on the weather's light scale (sunny 2, cloudy or rain 1, storms 0, times the area's
/// light, plus lamps). Below it the plant reads as short of light (PlantComponent.EvaluateEnvironment).
/// </param>
/// <param name="ColdResistance">
/// The cold the plant takes, on the weather's cold scale (cold snaps 1 to 3, less any heating). More than that and it
/// frosts, so 0 frosts in any cold snap.
/// </param>
/// <param name="Gain">The items a harvest gives.</param>
/// <param name="SeedRate">The chance, 0 to 1, that a harvest also gives a seed.</param>
public sealed record PlantRow(int Id, int GrowthTime, int HarvestTime, int LightNeed, int ColdResistance, IReadOnlyList<int> Gain, float SeedRate);

/// <summary>A furniture kind that holds loot.</summary>
/// <param name="Groups">LootRandomGroupId and its _P1 to _P4 character variants, the ones that are set.</param>
public sealed record LootFurniture(int Id, IReadOnlyList<int> Groups);

/// <summary>One piece of furniture the game sets down at a place (a Config_MapPoint id).</summary>
public readonly record struct Placement(int FurnitureId, int PlaceId);

/// <summary>The port for text that lives in the game's localisation rather than its tables.</summary>
public interface IGameText
{
    /// <summary>The item's description in the player's language, or null when it has none.</summary>
    string? Description(int itemId);

    /// <summary>The item's name in the player's language, or null when the game has none.</summary>
    string? ItemName(int itemId);

    /// <summary>The furniture kind's name in the player's language, or null when the game has none.</summary>
    string? FurnitureName(int furnitureId);

    /// <summary>The place's name (a Config_MapPoint id) in the player's language, or null when the game has none.</summary>
    string? PlaceName(int placeId);

    /// <summary>The game's text for a localisation key in the player's language, or null when it has none.</summary>
    string? LocalText(string key);

    /// <summary>True when the game shows Chinese. Anything unknown reads as English.</summary>
    bool IsChinese();
}
