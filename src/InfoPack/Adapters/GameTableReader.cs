using System;
using System.Collections.Generic;
using GameCore.HotUpdate;
using InfoPack.Core;
using Il2CppInts = Il2CppSystem.Collections.Generic.List<int>;

namespace InfoPack.Adapters;

/// <summary>
/// Copies what the facts need out of the game's config tables into plain data. Read only. Each table is read on its
/// own, so one the game changed shape on costs only the pieces built from it: a table that fails reads as empty and is
/// logged once.
/// </summary>
internal sealed class GameTableReader
{
    private const int FoodCategory = 1;

    private readonly Action<string> _log;
    private readonly HashSet<string> _reportedFailures = new();

    public GameTableReader(Action<string> log)
    {
        _log = log;
    }

    /// <summary>Null until the game has loaded its item table.</summary>
    public GameTables? TryRead()
    {
        var config = GameSingleton.Existing<ConfigManager>();
        var itemTable = config?._Config_Item_Dict;

        if (config == null || itemTable == null || itemTable.Count == 0) return null;

        var items = Guarded("item table", () => Items(config, itemTable), new List<ItemRow>());
        var shredYields = Guarded("shred yields", () => ShredYields(config, items), new Dictionary<int, IReadOnlyList<Yield>>());

        return new GameTables(
            items,
            Guarded("workbench recipes", () => Production(config), new List<ProductionRecipe>()),
            Guarded("cooking recipes", () => Cooking(config), new List<CookingRecipe>()),
            Guarded("brew barrels", () => BrewProducts(config), new List<IReadOnlyList<int>>()),
            shredYields,
            Guarded("talent table", () => Talents(config), new List<TalentRow>()),
            Guarded("buff table", () => BuffDurations(config), new Dictionary<int, float>()),
            Guarded("plant table", () => Plants(config), new Dictionary<int, PlantRow>()),
            Guarded("furniture loot", () => FurnitureLoot(config), new List<LootFurniture>()),
            Guarded("loot groups", () => LootGroups(config), new Dictionary<int, IReadOnlyList<int>>()),
            Guarded("furniture placements", () => Placements(config), new List<Placement>()))
        {
            AidDemandItems = Guarded("relief request demands", () => AidDemandItems(config), new HashSet<int>()),
            TrapBait = Guarded("trap bait table", () => TrapBait(config), new Dictionary<int, string>()),
            RobotModules = Guarded("robot module table", () => RobotModules(config), new Dictionary<int, int>()),
        };
    }

    /// <summary>The items a fixed request names (a demand of kind 0) and the items a drawn request can name (pool rows of kind 0).</summary>
    private static HashSet<int> AidDemandItems(ConfigManager config)
    {
        var items = new HashSet<int>();
        var commissions = config._Config_AidCommission_Dict ?? throw new InvalidOperationException("the game has no relief request table loaded");
        var pool = config._Config_AidDemandPool_Dict ?? throw new InvalidOperationException("the game has no relief demand pool loaded");

        foreach (var commission in commissions.Values)
        {
            var ids = Ints(commission?.DemandItemIds);
            var kinds = Ints(commission?.DemandKinds);

            for (var i = 0; i < ids.Count; i++)
            {
                if (ids[i] > 0 && (i >= kinds.Count || kinds[i] == AidDemand.ItemKind)) items.Add(ids[i]);
            }
        }

        foreach (var row in pool.Values)
        {
            if (row != null && row.Kind == AidDemand.ItemKind && row.ItemId > 0) items.Add(row.ItemId);
        }

        return items;
    }

    private static Dictionary<int, string> TrapBait(ConfigManager config)
    {
        var bait = new Dictionary<int, string>();
        var table = config._Config_TrapBait_Dict ?? throw new InvalidOperationException("the game has no trap bait table loaded");

        foreach (var row in table.Values)
        {
            if (row != null && !string.IsNullOrEmpty(row.Effect_Des)) bait[row.ID] = row.Effect_Des;
        }

        return bait;
    }

    private static Dictionary<int, int> RobotModules(ConfigManager config)
    {
        var modules = new Dictionary<int, int>();
        var table = config._Config_RobotModule_Dict ?? throw new InvalidOperationException("the game has no robot module table loaded");

        foreach (var row in table.Values)
        {
            if (row != null) modules[row.ID] = row.Level;
        }

        return modules;
    }

    private static List<ItemRow> Items(ConfigManager config, Il2CppSystem.Collections.Generic.Dictionary<int, Config_Item> table)
    {
        var rows = new List<ItemRow>(table.Count);

        foreach (var item in table.Values)
        {
            if (item == null) continue;

            rows.Add(new ItemRow(
                item.ID,
                item.Category,
                item.TradeValue,
                IsShreddable: config.IsShredableItem(item.ID),
                IsEdible: item.CanCook || item.FoodTag1 > 0 || item.FoodTag2 > 0 || item.FoodTag3 > 0,
                IsUsable: !item.CantUse,
                IsBurnable: item.Burnable,
                CanBrew: item.CanBrew,
                IsSeed: item.Plant > 0,
                IsTrap: item.Trap > 0,
                PlacesFurniture: item.TargetFurnitureID > 0,
                Stats: new[] { item.ValueDisplay1, item.ValueDisplay2, item.ValueDisplay3, item.ValueDisplay4, item.ValueDisplay5 },
                Weight: item.weight,
                ShelfLifeDays: item.Life,
                RotProduct: First(item.It_Rot_Product_Id),
                BurnValue: item.BurnValue,
                CutProduct: item.CutProductId,
                PlantId: item.Plant,
                Icon: item.WebIcon ?? "",
                InFoodCodex: item.Category == FoodCategory && item.InCodex));
        }

        return rows;
    }

    /// <summary>Asked only for items the game calls shreddable; the rest have no yields to show.</summary>
    private static Dictionary<int, IReadOnlyList<Yield>> ShredYields(ConfigManager config, IEnumerable<ItemRow> items)
    {
        var yields = new Dictionary<int, IReadOnlyList<Yield>>();

        foreach (var item in items)
        {
            if (!item.IsShreddable) continue;

            var found = config.GetShredYields(item.Id);
            var list = new List<Yield>();

            for (var i = 0; found != null && i < found.Count; i++)
            {
                var entry = found[i];

                if (entry != null) list.Add(new Yield(entry.ConfigId, entry.Count));
            }

            yields[item.Id] = list;
        }

        return yields;
    }

    private static List<ProductionRecipe> Production(ConfigManager config)
    {
        var recipes = new List<ProductionRecipe>();
        var table = config._Config_ProductionList_Dict;

        if (table == null) return recipes;

        foreach (var recipe in table.Values)
        {
            if (recipe != null) recipes.Add(new ProductionRecipe(Ints(recipe.MaterialList), ConfigManager.IsShredOnlyRecipe(recipe), First(recipe.ProductID), recipe.IsUseable));
        }

        return recipes;
    }

    private static List<CookingRecipe> Cooking(ConfigManager config)
    {
        var recipes = new List<CookingRecipe>();
        var table = config._Config_CookingRecipe_Dict;

        if (table == null) return recipes;

        foreach (var recipe in table.Values)
        {
            if (recipe != null) recipes.Add(new CookingRecipe(recipe.ID, Ints(recipe.SpecificItems), recipe.NormalItemID, recipe.GoodItemID, recipe.PerfectItemID, recipe.FailItemID, recipe.CookTime));
        }

        return recipes;
    }

    /// <summary>
    /// Every furniture row's brew products. The game's brew preview indexes a barrel's RotProductOverride by tier, so
    /// each list is lowest tier first; furniture that brews nothing has an empty list.
    /// </summary>
    private static List<IReadOnlyList<int>> BrewProducts(ConfigManager config)
    {
        var lists = new List<IReadOnlyList<int>>();
        var table = config._Config_Furniture_Dict;

        if (table == null) return lists;

        foreach (var furniture in table.Values)
        {
            var products = furniture == null ? new List<int>() : Ints(furniture.RotProductOverride);

            if (products.Count > 0) lists.Add(products);
        }

        return lists;
    }

    /// <summary>The planning cards need both this and the buff table; a missing one is thrown so it is logged once.</summary>
    private static List<TalentRow> Talents(ConfigManager config)
    {
        var talents = new List<TalentRow>();
        var table = config._Config_Talent_Dict ?? throw new InvalidOperationException("the game has no talent table loaded");

        foreach (var talent in table.Values)
        {
            if (talent != null) talents.Add(new TalentRow(talent.ID, talent.Type, talent.BuffID));
        }

        return talents;
    }

    private static Dictionary<int, float> BuffDurations(ConfigManager config)
    {
        var durations = new Dictionary<int, float>();
        var table = config._Config_Buff_Dict ?? throw new InvalidOperationException("the game has no buff table loaded");

        foreach (var buff in table.Values)
        {
            if (buff != null) durations[buff.ID] = buff.BuffDuring;
        }

        return durations;
    }

    private static Dictionary<int, PlantRow> Plants(ConfigManager config)
    {
        var plants = new Dictionary<int, PlantRow>();
        var table = config._Config_Plant_Dict ?? throw new InvalidOperationException("the game has no plant table loaded");

        foreach (var plant in table.Values)
        {
            if (plant != null) plants[plant.ID] = new PlantRow(plant.ID, plant.GrowthTime, plant.HarvestTime, plant.LightNeed, plant.ColdResistance, Ints(plant.Gain), plant.Seed_Rate);
        }

        return plants;
    }

    /// <summary>The _P1 to _P4 groups are the loot the furniture holds for each playable character; all of them count.</summary>
    private static List<LootFurniture> FurnitureLoot(ConfigManager config)
    {
        var furnitureWithLoot = new List<LootFurniture>();
        var table = config._Config_Furniture_Dict ?? throw new InvalidOperationException("the game has no furniture table loaded");

        foreach (var furniture in table.Values)
        {
            if (furniture == null) continue;

            var groups = new List<int>();

            foreach (var group in new[] { furniture.LootRandomGroupId, furniture.LootRandomGroupId_P1, furniture.LootRandomGroupId_P2, furniture.LootRandomGroupId_P3, furniture.LootRandomGroupId_P4 })
            {
                if (group > 0 && !groups.Contains(group)) groups.Add(group);
            }

            if (groups.Count > 0) furnitureWithLoot.Add(new LootFurniture(furniture.ID, groups));
        }

        return furnitureWithLoot;
    }

    private static Dictionary<int, IReadOnlyList<int>> LootGroups(ConfigManager config)
    {
        var groups = new Dictionary<int, IReadOnlyList<int>>();
        var table = config._Config_RandomGroup_Dict ?? throw new InvalidOperationException("the game has no loot group table loaded");

        foreach (var group in table.Values)
        {
            if (group != null) groups[group.ID] = Ints(group.IdList);
        }

        return groups;
    }

    private static List<Placement> Placements(ConfigManager config)
    {
        var placements = new List<Placement>();
        var table = config._Config_GuidanceFurniture_Dict ?? throw new InvalidOperationException("the game has no furniture placement table loaded");

        foreach (var placement in table.Values)
        {
            if (placement != null) placements.Add(new Placement(placement.FurnitureId, placement.MapPointID));
        }

        return placements;
    }

    private static List<int> Ints(Il2CppInts? list)
    {
        var ints = new List<int>();

        for (var i = 0; list != null && i < list.Count; i++) ints.Add(list[i]);

        return ints;
    }

    /// <summary>The game reads a one-entry list for these (the rot product, a recipe's product); 0 when it is empty.</summary>
    private static int First(Il2CppInts? list) => list != null && list.Count > 0 ? list[0] : 0;

    private T Guarded<T>(string what, Func<T> read, T empty)
    {
        try
        {
            return read();
        }
        catch (Exception ex)
        {
            if (_reportedFailures.Add(what)) _log($"could not read the {what}, the pieces built from it show nothing (reported once): {ex}");

            return empty;
        }
    }
}
