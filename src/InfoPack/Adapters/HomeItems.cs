using System.Collections.Generic;
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
    private readonly Il2CppItems _cache;
    private readonly List<long> _storageIds;

    private HomeItems(Il2CppItems cache, List<long> storageIds)
    {
        _cache = cache;
        _storageIds = storageIds;
    }

    /// <summary>Null until a save is loaded.</summary>
    public static HomeItems? TryRead()
    {
        var cache = ItemCache();
        var agents = GameSingleton.Existing<BattleLogicWorld>()?._AgentManager;
        var player = agents?.GetLeadingRole();

        if (cache == null || agents == null || player == null) return null;

        var storageIds = new List<long> { player.InstanceId, WorkbenchDrawerConfig.DrawerOwnerId };
        // The same call and scope the game's GetAvailableItemCount uses for crafting: hidden storages count too.
        var found = agents.GetFurnituresWithBag(agents.GetHomeMapId(), false, true, false);

        for (var i = 0; found != null && i < found.Count; i++)
        {
            var furniture = found[i];

            if (furniture != null) storageIds.Add(furniture.InstanceId);
        }

        return new HomeItems(cache, storageIds);
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

    private static Il2CppItems? ItemCache() =>
        GameSingleton.Existing<ReduxUISystem>()?.GetData<State_Data_Item>(Il2CppType.Of<State_Data_Item>())?.OwnerCache;
}
