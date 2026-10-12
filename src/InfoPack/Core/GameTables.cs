using System.Collections.Generic;

namespace InfoPack.Core;

/// <summary>
/// What the facts are worked out from: the game's config tables, read once per loaded world and copied into plain
/// data so nothing below touches the game.
/// </summary>
public sealed record GameTables(
    IReadOnlyList<ItemRow> Items,
    IReadOnlyList<ProductionRecipe> Production,
    IReadOnlyList<CookingRecipe> Cooking,
    IReadOnlyList<IReadOnlyList<int>> BrewProducts,
    IReadOnlyDictionary<int, IReadOnlyList<Yield>> ShredYields);

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
    IReadOnlyList<float> Stats);

/// <summary>A workbench recipe. MaterialList names one item id per unit, so an id can repeat.</summary>
/// <param name="IsShredOnly">The game's IsShredOnlyRecipe: the recipe breaks its material down rather than using it.</param>
public sealed record ProductionRecipe(IReadOnlyList<int> Materials, bool IsShredOnly);

/// <summary>A cooking recipe: the ingredients it asks for by name and the dish each result quality gives.</summary>
public sealed record CookingRecipe(int Id, IReadOnlyList<int> SpecificItems, int NormalItemId, int GoodItemId, int PerfectItemId, int FailItemId);

/// <summary>One line of what shredding an item gives back.</summary>
public readonly record struct Yield(int ConfigId, int Count);

/// <summary>The port for text that lives in the game's localisation rather than its tables.</summary>
public interface IGameText
{
    /// <summary>The game's gain and loss wording for satiety, mood, energy, health and life, in that order.</summary>
    IReadOnlyList<StatFormat> StatFormats();

    /// <summary>The item's description in the player's language, or null when it has none.</summary>
    string? Description(int itemId);
}
