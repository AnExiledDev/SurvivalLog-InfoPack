using System;
using System.Collections.Generic;
using System.Globalization;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using GameCore.HotUpdate.ReduxUI;
using Il2CppInterop.Runtime;
using InfoPack.Core;
using Il2CppHeld = Il2CppSystem.Collections.Generic.Dictionary<long, GameCore.HotUpdate.ReduxUI.Data_Item>;
using Il2CppItems = Il2CppSystem.Collections.Generic.Dictionary<long, Il2CppSystem.Collections.Generic.Dictionary<long, GameCore.HotUpdate.ReduxUI.Data_Item>>;

namespace InfoPack.Adapters;

/// <summary>
/// The game's item cache as the home count reads it: the UI store's per-owner item lists (the same source Storage Hub
/// reads), with home meaning the pool the game's crafting draws from: the player's bag, every storage on the home map
/// and its home group, and the workbench drawer. Read only. One instance is read again at every count into the same
/// buffers, and the cache and the furniture list are read straight from the game's memory, so a count makes no
/// interop object per stack or per storage. If that memory is ever not laid out as expected, the count goes through
/// the game's objects instead, as it always did, and says so once in the log. What a read finds is only good for the
/// frame it was taken in.
/// </summary>
internal sealed unsafe class HomeItems : IHomeItems
{
    private const string InventoryTextId = "ToolTable_0";
    private const string WorkbenchDrawerTextId = "ToolTable_14";

    private readonly Action<string> _log;
    private readonly List<long> _storageIds = new();
    private readonly HashSet<long> _homeIds = new();
    private readonly Dictionary<long, int> _furnitureConfigIds = new();
    private readonly Dictionary<long, IntPtr> _heldByOwner = new();
    private Il2CppSystem.Type? _itemStateType;
    private Il2CppItems? _cache;
    private long _playerId;

    // The memory reader, learned at the first read; off for good after anything unexpected.
    private bool _hasLearned;
    private bool _isDirect;
    private RawDictionary? _owners;
    private RawDictionary? _held;
    private RawList? _furniture;
    private IntPtr _itemClass;
    private int _ownerIdAt;
    private int _itemCountAt;
    private int _itemConfigIdAt;
    private int _instanceIdAt;
    private int _agentConfigIdAt;

    public HomeItems(Action<string> log)
    {
        _log = log;
    }

    /// <summary>False until a save is loaded. On true, the storages and their stacks can be asked for this frame.</summary>
    public bool TryRead()
    {
        _storageIds.Clear();
        _homeIds.Clear();
        _furnitureConfigIds.Clear();
        _heldByOwner.Clear();
        _cache = ItemCache();

        var agents = GameSingleton.Existing<BattleLogicWorld>()?._AgentManager;
        var player = agents?.GetLeadingRole();

        if (_cache == null || agents == null || player == null) return false;

        Learn(_cache);
        _playerId = player.InstanceId;
        AddStorage(_playerId);
        AddStorage(WorkbenchDrawerConfig.DrawerOwnerId);

        // The same call and scope the game's GetAvailableItemCount uses for crafting: hidden storages count too.
        var found = agents.GetFurnituresWithBag(agents.GetHomeMapId(), false, true, false);

        if (found != null && found.Count > 0)
        {
            // What is staged in the neighbor's basket is going to her, so it is not counted as at home. The game's
            // storage query includes the basket; its config id is 0 when there is none.
            var basketId = NeighborRules.GetBasketFurnitureId();

            if (!_isDirect || !TryAddFurnitureDirect(found.Pointer, basketId)) AddFurniture(found, basketId);
        }

        if (_isDirect) TryFindHeldDirect();

        return true;
    }

    public IReadOnlyList<long> HomeStorageIds() => _storageIds;

    public void StacksIn(long storageId, List<HeldStack> into)
    {
        if (_cache == null) return;

        if (_isDirect)
        {
            if (!_heldByOwner.TryGetValue(storageId, out var held)) return;

            var start = into.Count;

            if (TryAddStacksDirect(held, into)) return;

            into.RemoveRange(start, into.Count - start);
        }

        if (!_cache.TryGetValue(storageId, out var heldItems) || heldItems == null) return;

        foreach (var item in heldItems.Values)
        {
            if (item != null) into.Add(new HeldStack(item.OwnerId, item.ItemConfigId, item.ItemCount));
        }
    }

    /// <summary>
    /// The config id of the item with this instance id, wherever the cache holds it, or 0 when it holds no such item.
    /// The trade window names its own items by instance id only.
    /// </summary>
    public int ConfigOfInstance(long itemId)
    {
        var cache = ItemCache();

        if (cache == null) return 0;

        Learn(cache);

        if (_isDirect && TryConfigOfInstanceDirect(cache.Pointer, itemId, out var configId)) return configId;

        foreach (var held in cache.Values)
        {
            if (held != null && held.TryGetValue(itemId, out var item) && item != null) return item.ItemConfigId;
        }

        return 0;
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

    private void AddStorage(long storageId)
    {
        _storageIds.Add(storageId);
        _homeIds.Add(storageId);
    }

    private void AddFurniture(Il2CppSystem.Collections.Generic.List<Furniture> found, int basketId)
    {
        for (var i = 0; i < found.Count; i++)
        {
            var furniture = found[i];

            if (furniture == null || (basketId > 0 && furniture.AgentConfigId == basketId)) continue;

            AddStorage(furniture.InstanceId);
            _furnitureConfigIds[furniture.InstanceId] = furniture.AgentConfigId;
        }
    }

    private bool TryAddFurnitureDirect(IntPtr list, int basketId)
    {
        _furniture ??= RawList.Learn(IL2CPP.il2cpp_object_get_class(list));

        if (_furniture == null || !_furniture.TryOpen(list, out var items, out var count)) return GiveUp("the furniture list");

        for (var i = 0; i < count; i++)
        {
            var furniture = RawList.Item(items, i);

            if (furniture == IntPtr.Zero) continue;

            var configId = *(int*)(furniture + _agentConfigIdAt);

            if (basketId > 0 && configId == basketId) continue;

            var instanceId = *(long*)(furniture + _instanceIdAt);

            AddStorage(instanceId);
            _furnitureConfigIds[instanceId] = configId;
        }

        return true;
    }

    /// <summary>One pass over the cache's owners, keeping the item list of each home storage.</summary>
    private bool TryFindHeldDirect()
    {
        if (_owners == null || !_owners.TryOpen(_cache!.Pointer, out var entries, out var used)) return GiveUp("the item cache");

        for (var i = 0; i < used; i++)
        {
            if (!RawDictionary.IsLive(entries, i)) continue;

            var owner = RawDictionary.Key(entries, i);
            var held = RawDictionary.Value(entries, i);

            if (held != IntPtr.Zero && _homeIds.Contains(owner)) _heldByOwner[owner] = held;
        }

        return true;
    }

    private bool TryAddStacksDirect(IntPtr held, List<HeldStack> into)
    {
        if (_held == null || !_held.TryOpen(held, out var entries, out var used)) return GiveUp("a storage's item list");

        for (var i = 0; i < used; i++)
        {
            if (!RawDictionary.IsLive(entries, i)) continue;

            var item = RawDictionary.Value(entries, i);

            if (item == IntPtr.Zero) continue;

            if (*(IntPtr*)item != _itemClass) return GiveUp("an item stack");

            into.Add(new HeldStack(*(long*)(item + _ownerIdAt), *(int*)(item + _itemConfigIdAt), *(int*)(item + _itemCountAt)));
        }

        return true;
    }

    private bool TryConfigOfInstanceDirect(IntPtr cache, long itemId, out int configId)
    {
        configId = 0;

        if (_owners == null || !_owners.TryOpen(cache, out var owners, out var ownerCount)) return GiveUp("the item cache");

        for (var o = 0; o < ownerCount; o++)
        {
            var held = RawDictionary.IsLive(owners, o) ? RawDictionary.Value(owners, o) : IntPtr.Zero;

            if (held == IntPtr.Zero) continue;

            if (_held == null || !_held.TryOpen(held, out var items, out var itemCount)) return GiveUp("a storage's item list");

            for (var i = 0; i < itemCount; i++)
            {
                if (!RawDictionary.IsLive(items, i) || RawDictionary.Key(items, i) != itemId) continue;

                var item = RawDictionary.Value(items, i);

                if (item == IntPtr.Zero) continue;

                if (*(IntPtr*)item != _itemClass) return GiveUp("an item stack");

                configId = *(int*)(item + _itemConfigIdAt);

                return true;
            }
        }

        return true;
    }

    /// <summary>
    /// Where the fields sit, from the game's own type data, once. Anything missing leaves the memory reader off and the
    /// count goes through the game's objects.
    /// </summary>
    private void Learn(Il2CppItems cache)
    {
        if (_hasLearned) return;

        _hasLearned = true;

        try
        {
            _owners = RawDictionary.Learn(IL2CPP.il2cpp_object_get_class(cache.Pointer));
            _held = RawDictionary.Learn(Il2CppClassPointerStore<Il2CppHeld>.NativeClassPtr);
            _itemClass = Il2CppClassPointerStore<Data_Item>.NativeClassPtr;
            _ownerIdAt = RawDictionary.FieldOffset(_itemClass, "<OwnerId>k__BackingField");
            _itemCountAt = RawDictionary.FieldOffset(_itemClass, "<ItemCount>k__BackingField");
            _itemConfigIdAt = RawDictionary.FieldOffset(_itemClass, "<ItemConfigId>k__BackingField");
            _instanceIdAt = RawDictionary.FieldOffset(Il2CppClassPointerStore<BaseEntity>.NativeClassPtr, "<InstanceId>k__BackingField");
            _agentConfigIdAt = RawDictionary.FieldOffset(Il2CppClassPointerStore<BaseAgent>.NativeClassPtr, "<AgentConfigId>k__BackingField");
            _isDirect = _owners != null && _held != null && _itemClass != IntPtr.Zero
                && _ownerIdAt > 0 && _itemCountAt > 0 && _itemConfigIdAt > 0 && _instanceIdAt > 0 && _agentConfigIdAt > 0;
        }
        catch (Exception ex)
        {
            _log("could not learn the item cache's layout, counting through the game's objects instead (reported once): " + ex.Message);

            return;
        }

        if (!_isDirect) _log("the item cache is not laid out as expected, counting through the game's objects instead (reported once)");
    }

    /// <summary>Turns the memory reader off for good and says why, once. Always false.</summary>
    private bool GiveUp(string what)
    {
        _isDirect = false;
        _heldByOwner.Clear();
        _log("could not read " + what + " directly, counting through the game's objects instead (reported once)");

        return false;
    }

    private string? FurnitureName(long storageId)
    {
        var config = GameSingleton.Existing<ConfigManager>();

        if (config == null || !_furnitureConfigIds.TryGetValue(storageId, out var configId)) return null;

        // Looked up in the table itself: the game's Get_Config_Furniture logs an error for an id it lacks.
        var table = config._Config_Furniture_Dict;
        var name = table != null && table.TryGetValue(configId, out var row) ? row?.Name : null;

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

    private Il2CppItems? ItemCache() =>
        GameSingleton.Existing<ReduxUISystem>()?.GetData<State_Data_Item>(_itemStateType ??= Il2CppType.Of<State_Data_Item>())?.OwnerCache;
}
