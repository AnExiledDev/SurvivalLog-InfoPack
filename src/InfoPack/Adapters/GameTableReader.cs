using System;
using System.Collections.Generic;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using Il2CppInterop.Runtime.InteropTypes;
using InfoPack.Core;
using Il2CppInts = Il2CppSystem.Collections.Generic.List<int>;

namespace InfoPack.Adapters;

/// <summary>
/// Copies what the facts need out of the game's config tables into plain data. Read only. Each table is read on its
/// own, so one the game changed shape on costs only the pieces built from it: a table that fails reads as empty and is
/// logged once. The reading is a list of steps, one table row each, so a <see cref="SlicedJob"/> can spread it over
/// frames. The copy is kept while none of the tables it came from changed, so a save load or a language change reads
/// them again only to check that; the brewing and fuel numbers follow the player's talents and are read every time.
/// </summary>
internal sealed class GameTableReader
{
    private const int FoodCategory = 1;
    private const int GeneratorType = 3;
    private const int HeaterType = 7;

    private readonly Action<string> _log;
    private readonly HashSet<string> _reportedFailures = new();
    private readonly List<long> _readStamp = new();
    private GameTables? _kept;
    private List<long> _keptStamp = new();

    public GameTableReader(Action<string> log)
    {
        _log = log;
    }

    /// <summary>Whether the game has loaded its item table, so a read would find something.</summary>
    public bool IsLoaded()
    {
        var itemTable = GameSingleton.Existing<ConfigManager>()?._Config_Item_Dict;

        return itemTable != null && itemTable.Count > 0;
    }

    /// <summary>
    /// The steps that read the tables; done gets them at the last one. Started in the frame <see cref="IsLoaded"/> said
    /// yes, the first step finds them loaded; if they are gone by then, the steps end without calling done.
    /// </summary>
    public IEnumerable<bool> Read(Action<GameTables> done)
    {
        var config = GameSingleton.Existing<ConfigManager>();
        var itemTable = config?._Config_Item_Dict;

        if (config == null || itemTable == null || itemTable.Count == 0) yield break;

        Stamp(config, _readStamp);

        var tables = _kept;

        if (tables == null || !Same(_readStamp, _keptStamp))
        {
            _kept = null;

            var attempt = new Attempt();
            var items = new List<ItemRow>();
            var shredYields = new Dictionary<int, IReadOnlyList<Yield>>();
            var production = new List<ProductionRecipe>();
            var cooking = new List<CookingRecipe>();
            var brewProducts = new List<IReadOnlyList<int>>();
            var talents = new List<TalentRow>();
            var buffDurations = new Dictionary<int, float>();
            var plants = new Dictionary<int, PlantRow>();
            var furnitureLoot = new List<LootFurniture>();
            var lootGroups = new Dictionary<int, IReadOnlyList<int>>();
            var placements = new List<Placement>();
            var aidDemandItems = new HashSet<int>();
            var trapBait = new Dictionary<int, string>();
            var robotModules = new Dictionary<int, int>();

            // The stamp is taken before the rows, so a table that changes while it is read is read again next time.
            var stamp = new List<long>(_readStamp);

            foreach (var step in Guarded("item table", Items(config, itemTable, items), items.Clear, attempt)) yield return step;
            foreach (var step in Guarded("shred yields", ShredYields(config, items, shredYields), shredYields.Clear, attempt)) yield return step;
            foreach (var step in Guarded("workbench recipes", Production(config, production), production.Clear, attempt)) yield return step;
            foreach (var step in Guarded("cooking recipes", Cooking(config, cooking), cooking.Clear, attempt)) yield return step;
            foreach (var step in Guarded("brew barrels", BrewProducts(config, brewProducts), brewProducts.Clear, attempt)) yield return step;
            foreach (var step in Guarded("talent table", Talents(config, talents), talents.Clear, attempt)) yield return step;
            foreach (var step in Guarded("buff table", BuffDurations(config, buffDurations), buffDurations.Clear, attempt)) yield return step;
            foreach (var step in Guarded("plant table", Plants(config, plants), plants.Clear, attempt)) yield return step;
            foreach (var step in Guarded("furniture loot", FurnitureLoot(config, furnitureLoot), furnitureLoot.Clear, attempt)) yield return step;
            foreach (var step in Guarded("loot groups", LootGroups(config, lootGroups), lootGroups.Clear, attempt)) yield return step;
            foreach (var step in Guarded("furniture placements", Placements(config, placements), placements.Clear, attempt)) yield return step;
            foreach (var step in Guarded("relief request demands", AidDemandItems(config, aidDemandItems), aidDemandItems.Clear, attempt)) yield return step;
            foreach (var step in Guarded("trap bait table", TrapBait(config, trapBait), trapBait.Clear, attempt)) yield return step;
            foreach (var step in Guarded("robot module table", RobotModules(config, robotModules), robotModules.Clear, attempt)) yield return step;

            tables = new GameTables(items, production, cooking, brewProducts, shredYields, talents, buffDurations, plants, furnitureLoot, lootGroups, placements)
            {
                AidDemandItems = aidDemandItems,
                TrapBait = trapBait,
                RobotModules = robotModules,
            };

            // A table that failed is read again next time rather than kept empty.
            if (!attempt.HasFailed)
            {
                _kept = tables;
                _keptStamp = stamp;
            }
        }

        done(tables with
        {
            Brewing = Guarded("brew settings", BrewSettings, BrewRules.Default),
            Fuel = Guarded("fuel burners", () => FuelBurners(config), FuelRates.None),
        });
    }

    /// <summary>Which tables these are and how often each was changed: any edit to one gives a new stamp.</summary>
    private static void Stamp(ConfigManager config, List<long> into)
    {
        into.Clear();
        into.Add(config.Pointer.ToInt64());
        Stamp(into, config._Config_Item_Dict);
        Stamp(into, config._Config_ProductionList_Dict);
        Stamp(into, config._Config_CookingRecipe_Dict);
        Stamp(into, config._Config_Furniture_Dict);
        Stamp(into, config._Config_Talent_Dict);
        Stamp(into, config._Config_Buff_Dict);
        Stamp(into, config._Config_Plant_Dict);
        Stamp(into, config._Config_RandomGroup_Dict);
        Stamp(into, config._Config_GuidanceFurniture_Dict);
        Stamp(into, config._Config_AidCommission_Dict);
        Stamp(into, config._Config_AidDemandPool_Dict);
        Stamp(into, config._Config_TrapBait_Dict);
        Stamp(into, config._Config_RobotModule_Dict);
    }

    private static void Stamp<TKey, TValue>(List<long> into, Il2CppSystem.Collections.Generic.Dictionary<TKey, TValue>? table)
    {
        if (table == null)
        {
            into.Add(0L);

            return;
        }

        into.Add(table.Pointer.ToInt64());
        into.Add(table._version);
        into.Add(table.Count);
    }

    private static bool Same(List<long> a, List<long> b)
    {
        if (a.Count != b.Count) return false;

        for (var i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i]) return false;
        }

        return true;
    }

    /// <summary>
    /// A table's rows, gathered in one step so no step holds the game's collection open across frames; each row is
    /// then read in a step of its own.
    /// </summary>
    private static List<T> Rows<T>(Il2CppSystem.Collections.Generic.Dictionary<int, T> table) where T : Il2CppObjectBase
    {
        var rows = new List<T>(table.Count);

        foreach (var row in table.Values)
        {
            if (row != null) rows.Add(row);
        }

        return rows;
    }

    /// <summary>The items a fixed request names (a demand of kind 0) and the items a drawn request can name (pool rows of kind 0).</summary>
    private static IEnumerable<bool> AidDemandItems(ConfigManager config, HashSet<int> items)
    {
        var commissions = Rows(config._Config_AidCommission_Dict ?? throw new InvalidOperationException("the game has no relief request table loaded"));
        var pool = Rows(config._Config_AidDemandPool_Dict ?? throw new InvalidOperationException("the game has no relief demand pool loaded"));

        yield return true;

        foreach (var commission in commissions)
        {
            var ids = Ints(commission.DemandItemIds);
            var kinds = Ints(commission.DemandKinds);

            for (var i = 0; i < ids.Count; i++)
            {
                if (ids[i] > 0 && (i >= kinds.Count || kinds[i] == AidDemand.ItemKind)) items.Add(ids[i]);
            }

            yield return true;
        }

        foreach (var row in pool)
        {
            if (row.Kind == AidDemand.ItemKind && row.ItemId > 0) items.Add(row.ItemId);

            yield return true;
        }
    }

    private static IEnumerable<bool> TrapBait(ConfigManager config, Dictionary<int, string> bait)
    {
        var rows = Rows(config._Config_TrapBait_Dict ?? throw new InvalidOperationException("the game has no trap bait table loaded"));

        yield return true;

        foreach (var row in rows)
        {
            var effect = row.Effect_Des;

            if (!string.IsNullOrEmpty(effect)) bait[row.ID] = effect;

            yield return true;
        }
    }

    private static IEnumerable<bool> RobotModules(ConfigManager config, Dictionary<int, int> modules)
    {
        var rows = Rows(config._Config_RobotModule_Dict ?? throw new InvalidOperationException("the game has no robot module table loaded"));

        yield return true;

        foreach (var row in rows)
        {
            modules[row.ID] = row.Level;

            yield return true;
        }
    }

    private static IEnumerable<bool> Items(ConfigManager config, Il2CppSystem.Collections.Generic.Dictionary<int, Config_Item> table, List<ItemRow> rows)
    {
        var items = Rows(table);

        rows.Capacity = items.Count;

        yield return true;

        foreach (var item in items)
        {
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
                InFoodCodex: item.Category == FoodCategory && item.InCodex,
                UseTimes: item.UseTimes));

            yield return true;
        }
    }

    /// <summary>Asked only for items the game calls shreddable; the rest have no yields to show.</summary>
    private static IEnumerable<bool> ShredYields(ConfigManager config, List<ItemRow> items, Dictionary<int, IReadOnlyList<Yield>> yields)
    {
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

            yield return true;
        }
    }

    private static IEnumerable<bool> Production(ConfigManager config, List<ProductionRecipe> recipes)
    {
        var table = config._Config_ProductionList_Dict;

        if (table == null) yield break;

        var rows = Rows(table);

        yield return true;

        foreach (var recipe in rows)
        {
            recipes.Add(new ProductionRecipe(Ints(recipe.MaterialList), ConfigManager.IsShredOnlyRecipe(recipe), First(recipe.ProductID), recipe.IsUseable));

            yield return true;
        }
    }

    private static IEnumerable<bool> Cooking(ConfigManager config, List<CookingRecipe> recipes)
    {
        var table = config._Config_CookingRecipe_Dict;

        if (table == null) yield break;

        var rows = Rows(table);

        yield return true;

        foreach (var recipe in rows)
        {
            recipes.Add(new CookingRecipe(recipe.ID, Ints(recipe.SpecificItems), recipe.NormalItemID, recipe.GoodItemID, recipe.PerfectItemID, recipe.FailItemID, recipe.CookTime));

            yield return true;
        }
    }

    /// <summary>
    /// Every furniture row's brew products. The game's brew preview indexes a barrel's RotProductOverride by tier, so
    /// each list is lowest tier first; furniture that brews nothing has an empty list.
    /// </summary>
    private static IEnumerable<bool> BrewProducts(ConfigManager config, List<IReadOnlyList<int>> lists)
    {
        var table = config._Config_Furniture_Dict;

        if (table == null) yield break;

        var rows = Rows(table);

        yield return true;

        foreach (var furniture in rows)
        {
            var products = Ints(furniture.RotProductOverride);

            if (products.Count > 0) lists.Add(products);

            yield return true;
        }
    }

    /// <summary>The game's own effective values, so a brewing talent the player has when the world loads counts.</summary>
    private static BrewRules BrewSettings()
    {
        Furniture.GetEffectiveTierThresholds(out var low, out var high);

        return new BrewRules(low, high, Furniture.GetEffectiveSatietyPerBottle());
    }

    /// <summary>
    /// The rate of every cooking pot, heater (electrical type 7) and generator (type 3) that has fuel slots, with the
    /// two fuel-saving talents as the leading role has them now.
    /// </summary>
    private static FuelRates FuelBurners(ConfigManager config)
    {
        var cookTable = config._Config_FurnitureCook_Dict ?? throw new InvalidOperationException("the game has no cooking furniture table loaded");
        var electricalTable = config._Config_FurnitureElectrical_Dict ?? throw new InvalidOperationException("the game has no electrical furniture table loaded");
        var cooking = new List<float>();
        var heaters = new List<float>();
        var generators = new List<float>();

        foreach (var pot in cookTable.Values)
        {
            if (pot != null && pot.FuelSlotCount > 0) cooking.Add(pot.FuelRate);
        }

        foreach (var device in electricalTable.Values)
        {
            if (device == null || device.FuelSlotCount <= 0) continue;

            if (device.ElectricalType == HeaterType) heaters.Add(device.FuelRate);
            else if (device.ElectricalType == GeneratorType) generators.Add(device.FuelRate);
        }

        return new FuelRates(
            cooking,
            Saving(() => Furniture.GetTalentEffectRatio("Buff/AE_FuelSavingMultiplier")),
            heaters,
            generators,
            Saving(() => PowerManager.GetTalentEffectRatio("Buff/AE_GeneratorFuelSaving")));
    }

    /// <summary>Before the leading role exists the game has no talents to ask about, which is the same as none.</summary>
    private static float Saving(Func<float> read)
    {
        try
        {
            return read();
        }
        catch (Exception)
        {
            return 0f;
        }
    }

    /// <summary>The planning cards need both this and the buff table; a missing one is thrown so it is logged once.</summary>
    private static IEnumerable<bool> Talents(ConfigManager config, List<TalentRow> talents)
    {
        var rows = Rows(config._Config_Talent_Dict ?? throw new InvalidOperationException("the game has no talent table loaded"));

        yield return true;

        foreach (var talent in rows)
        {
            talents.Add(new TalentRow(talent.ID, talent.Type, talent.BuffID));

            yield return true;
        }
    }

    private static IEnumerable<bool> BuffDurations(ConfigManager config, Dictionary<int, float> durations)
    {
        var rows = Rows(config._Config_Buff_Dict ?? throw new InvalidOperationException("the game has no buff table loaded"));

        yield return true;

        foreach (var buff in rows)
        {
            durations[buff.ID] = buff.BuffDuring;

            yield return true;
        }
    }

    private static IEnumerable<bool> Plants(ConfigManager config, Dictionary<int, PlantRow> plants)
    {
        var rows = Rows(config._Config_Plant_Dict ?? throw new InvalidOperationException("the game has no plant table loaded"));

        yield return true;

        foreach (var plant in rows)
        {
            plants[plant.ID] = new PlantRow(plant.ID, plant.GrowthTime, plant.HarvestTime, plant.LightNeed, plant.ColdResistance, Ints(plant.Gain), plant.Seed_Rate);

            yield return true;
        }
    }

    /// <summary>The _P1 to _P4 groups are the loot the furniture holds for each playable character; all of them count.</summary>
    private static IEnumerable<bool> FurnitureLoot(ConfigManager config, List<LootFurniture> furnitureWithLoot)
    {
        var rows = Rows(config._Config_Furniture_Dict ?? throw new InvalidOperationException("the game has no furniture table loaded"));

        yield return true;

        foreach (var furniture in rows)
        {
            var groups = new List<int>();

            foreach (var group in new[] { furniture.LootRandomGroupId, furniture.LootRandomGroupId_P1, furniture.LootRandomGroupId_P2, furniture.LootRandomGroupId_P3, furniture.LootRandomGroupId_P4 })
            {
                if (group > 0 && !groups.Contains(group)) groups.Add(group);
            }

            if (groups.Count > 0) furnitureWithLoot.Add(new LootFurniture(furniture.ID, groups));

            yield return true;
        }
    }

    private static IEnumerable<bool> LootGroups(ConfigManager config, Dictionary<int, IReadOnlyList<int>> groups)
    {
        var rows = Rows(config._Config_RandomGroup_Dict ?? throw new InvalidOperationException("the game has no loot group table loaded"));

        yield return true;

        foreach (var group in rows)
        {
            groups[group.ID] = Ints(group.IdList);

            yield return true;
        }
    }

    private static IEnumerable<bool> Placements(ConfigManager config, List<Placement> placements)
    {
        var rows = Rows(config._Config_GuidanceFurniture_Dict ?? throw new InvalidOperationException("the game has no furniture placement table loaded"));

        yield return true;

        foreach (var placement in rows)
        {
            placements.Add(new Placement(placement.FurnitureId, placement.MapPointID));

            yield return true;
        }
    }

    private static List<int> Ints(Il2CppInts? list)
    {
        var ints = new List<int>();

        for (var i = 0; list != null && i < list.Count; i++) ints.Add(list[i]);

        return ints;
    }

    /// <summary>The game reads a one-entry list for these (the rot product, a recipe's product); 0 when it is empty.</summary>
    private static int First(Il2CppInts? list) => list != null && list.Count > 0 ? list[0] : 0;

    /// <summary>
    /// Runs one table's steps. A step that throws ends that table: what it read so far is cleared, so the table reads
    /// as empty, the failure is logged once, and the read is marked as failed.
    /// </summary>
    private IEnumerable<bool> Guarded(string what, IEnumerable<bool> steps, Action clear, Attempt attempt)
    {
        using var each = steps.GetEnumerator();

        while (true)
        {
            bool hasStep;
            Exception? failure = null;

            try
            {
                hasStep = each.MoveNext();
            }
            catch (Exception ex)
            {
                hasStep = false;
                failure = ex;
            }

            if (failure != null)
            {
                Report(what, failure);
                clear();
                attempt.HasFailed = true;
            }

            if (!hasStep) yield break;

            yield return each.Current;
        }
    }

    private T Guarded<T>(string what, Func<T> read, T empty)
    {
        try
        {
            return read();
        }
        catch (Exception ex)
        {
            Report(what, ex);

            return empty;
        }
    }

    private void Report(string what, Exception ex)
    {
        if (_reportedFailures.Add(what)) _log($"could not read the {what}, the pieces built from it show nothing (reported once): {ex}");
    }

    /// <summary>Whether any table failed in one read, so its copy is not kept.</summary>
    private sealed class Attempt
    {
        public bool HasFailed;
    }
}
