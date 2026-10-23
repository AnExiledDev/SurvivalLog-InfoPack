using System.Collections.Generic;
using System.Globalization;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using GameCore.HotUpdate.ReduxUI;
using Il2CppInterop.Runtime;
using InfoPack.Core;
using Il2CppItems = Il2CppSystem.Collections.Generic.Dictionary<long, Il2CppSystem.Collections.Generic.Dictionary<long, GameCore.HotUpdate.ReduxUI.Data_Item>>;

namespace InfoPack.Adapters;

/// <summary>
/// The game's item cache as the home count reads it: the UI store's per-owner item lists (the same source Storage Hub
/// reads), with home meaning the pool the game's crafting draws from: the player's bag, every storage on the home map
/// and its home group, and the workbench drawer. Read only; one snapshot per count.
/// </summary>
internal sealed class HomeItems : IHomeItems
{
    private const string InventoryTextId = "ToolTable_0";
    private const string WorkbenchDrawerTextId = "ToolTable_14";

    private readonly Il2CppItems _cache;
    private readonly long _playerId;
    private readonly List<long> _storageIds;
    private readonly Dictionary<long, int> _furnitureConfigIds;

    private HomeItems(Il2CppItems cache, long playerId, List<long> storageIds, Dictionary<long, int> furnitureConfigIds)
    {
        _cache = cache;
        _playerId = playerId;
        _storageIds = storageIds;
        _furnitureConfigIds = furnitureConfigIds;
    }

    /// <summary>Null until a save is loaded.</summary>
    public static HomeItems? TryRead()
    {
        var cache = ItemCache();
        var agents = GameSingleton.Existing<BattleLogicWorld>()?._AgentManager;
        var player = agents?.GetLeadingRole();

        if (cache == null || agents == null || player == null) return null;

        var storageIds = new List<long> { player.InstanceId, WorkbenchDrawerConfig.DrawerOwnerId };
        var furnitureConfigIds = new Dictionary<long, int>();
        // The same call and scope the game's GetAvailableItemCount uses for crafting: hidden storages count too.
        var found = agents.GetFurnituresWithBag(agents.GetHomeMapId(), false, true, false);

        for (var i = 0; found != null && i < found.Count; i++)
        {
            var furniture = found[i];

            if (furniture == null || IsNeighborBasket(furniture)) continue;

            storageIds.Add(furniture.InstanceId);
            furnitureConfigIds[furniture.InstanceId] = furniture.AgentConfigId;
        }

        return new HomeItems(cache, player.InstanceId, storageIds, furnitureConfigIds);
    }

    /// <summary>
    /// What is staged in the neighbor's basket is going to her, so it is not counted as at home. The game's storage
    /// query includes the basket; its config id is 0 when there is none.
    /// </summary>
    private static bool IsNeighborBasket(Furniture furniture)
    {
        var basketId = NeighborRules.GetBasketFurnitureId();

        return basketId > 0 && furniture.AgentConfigId == basketId;
    }

    /// <summary>
    /// The config id of the item with this instance id, wherever the cache holds it, or 0 when it holds no such item.
    /// The trade window names its own items by instance id only.
    /// </summary>
    public static int ConfigOfInstance(long itemId)
    {
        var cache = ItemCache();

        if (cache == null) return 0;

        foreach (var held in cache.Values)
        {
            if (held != null && held.TryGetValue(itemId, out var item) && item != null) return item.ItemConfigId;
        }

        return 0;
    }

    public IReadOnlyList<long> HomeStorageIds() => _storageIds;

    public IEnumerable<HeldStack> StacksIn(long storageId)
    {
        if (!_cache.TryGetValue(storageId, out var held) || held == null) yield break;

        foreach (var item in held.Values)
        {
            if (item != null) yield return new HeldStack(item.OwnerId, item.ItemConfigId, item.ItemCount);
        }
    }

    /// <summary>
    /// A home storage's name in the game's language: the game's own words for the bag and the workbench drawer, and a
    /// furniture's name from its table row, as Storage Hub names them.
    /// </summary>
    public string StorageName(long storageId, bool isChinese)
    {
        if (storageId == _playerId) return ConstantText(InventoryTextId) ?? Texts.Inventory.Pick(isChinese);

        if (storageId == WorkbenchDrawerConfig.DrawerOwnerId) return ConstantText(WorkbenchDrawerTextId) ?? Texts.WorkbenchDrawer.Pick(isChinese);

        return FurnitureName(storageId) ?? string.Format(CultureInfo.InvariantCulture, Texts.UnnamedStorage.Pick(isChinese), storageId);
    }

    private string? FurnitureName(long storageId)
    {
        var config = GameSingleton.Existing<ConfigManager>();

        if (config == null || !_furnitureConfigIds.TryGetValue(storageId, out var configId)) return null;

        var name = config.Get_Config_Furniture(configId)?.Name;

        return Translate(config, name) ?? NonEmpty(name);
    }

    /// <summary>The game's lookup hands the key back when it has no text for it.</summary>
    private static string? Translate(ConfigManager config, string? key)
    {
        if (string.IsNullOrEmpty(key)) return null;

        return NonEmpty(config.GetLocalTxt(key)) is { } text && text != key ? text : null;
    }

    private static string? ConstantText(string textId) => NonEmpty(ConstantTextTools.ToConstantTextOrEmpty(textId));

    private static string? NonEmpty(string? text) => string.IsNullOrWhiteSpace(text) ? null : text;

    private static Il2CppItems? ItemCache() =>
        GameSingleton.Existing<ReduxUISystem>()?.GetData<State_Data_Item>(Il2CppType.Of<State_Data_Item>())?.OwnerCache;
}
