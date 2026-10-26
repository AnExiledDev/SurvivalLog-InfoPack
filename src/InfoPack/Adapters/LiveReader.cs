using System;
using System.Collections.Generic;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using GameCore.HotUpdate.ReduxUI;
using InfoPack.Core;

namespace InfoPack.Adapters;

/// <summary>
/// What the save says right now: the player's craving, the relief requests in progress, which foods the codex has, the
/// player's stats and the game clock. Read only. Matching every item against a craving or a request is the slow part,
/// so it is done again only when the craving or the requests change, and the codex is asked about each food only when
/// its count of eaten foods moved. <see cref="Version"/> goes up whenever an answer changes, so a caller can skip
/// building its message when nothing did. Each piece fails on its own and is logged once.
/// </summary>
internal sealed class LiveReader
{
    /// <summary>The HUD's four bars by AttrName: satiety, mood, energy, life.</summary>
    public static readonly int[] HudStats = { (int)AttrName.Satiety, (int)AttrName.Morale, (int)AttrName.Stamina, (int)AttrName.Vitality };

    private static readonly IReadOnlyCollection<int> NoItems = Array.Empty<int>();
    private static readonly IReadOnlyDictionary<int, int> NoNeeds = new SortedDictionary<int, int>();
    private static readonly Func<int, int, bool> AidMatches = (demandItem, item) => AidPlatformManager.DemandMatchTier(demandItem, item) > 0;

    private readonly Action<string> _log;
    private readonly HashSet<string> _reportedFailures = new();
    private long _version;

    // The craving the items were matched for, and the item list they were matched in.
    private bool _isWishKnown;
    private bool _hasWish;
    private int _wishType;
    private int _wishTarget;
    private readonly List<int> _wishTargets = new();
    private readonly List<int> _readTargets = new();
    private IReadOnlyList<int>? _wishItemIds;
    private readonly SortedSet<int> _wishItems = new();

    // The demands the needs were worked out for, and the item list they were worked out in.
    private bool _isAidKnown;
    private List<AidDemand> _demands = new();
    private List<AidDemand> _readDemands = new();
    private IReadOnlyList<int>? _aidItemIds;
    private IReadOnlyDictionary<int, int> _aidNeeds = NoNeeds;

    private readonly UntriedFoods _untried = new();
    private readonly Func<int, bool> _isEaten;
    private CodexManager? _codex;
    private bool _hasCodex;

    private IntPtr _role;
    private AttributeComponent? _attributes;

    public LiveReader(Action<string> log)
    {
        _log = log;
        _isEaten = id => _codex!.IsEntryUnlocked(CodexCategory.Food, id);
    }

    /// <summary>Goes up whenever the craving items, the request needs or the untried foods change.</summary>
    public long Version => _version + _untried.Version;

    /// <summary>The items that would satisfy the craving the player has now; none without one.</summary>
    public IReadOnlyCollection<int> WishItems(BattleLogicWorld world, IReadOnlyList<int> itemIds)
    {
        try
        {
            var wishes = world._WishManager;
            var wish = wishes == null || wishes.IsLocked ? null : wishes.currentWish;

            if (wish == null || wish.IsCompleted || wish.IsExpired)
            {
                if (_isWishKnown && !_hasWish) return NoItems;

                ClearWish();
                _isWishKnown = true;

                return NoItems;
            }

            var type = (int)wish.Type;
            var target = wish.TargetValue;
            var targets = wish.TargetValues;

            _readTargets.Clear();

            for (var i = 0; targets != null && i < targets.Count; i++) _readTargets.Add(targets[i]);

            if (_isWishKnown && _hasWish && type == _wishType && target == _wishTarget && ReferenceEquals(itemIds, _wishItemIds) && Same(_readTargets, _wishTargets)) return _wishItems;

            ClearWish();
            MatchWish(type, target, targets, itemIds);
            _hasWish = true;
            _wishType = type;
            _wishTarget = target;
            _wishTargets.AddRange(_readTargets);
            _wishItemIds = itemIds;
            _isWishKnown = true;

            return _wishItems;
        }
        catch (Exception ex)
        {
            Report("craving", ex);
            ClearWish();

            return NoItems;
        }
    }

    /// <summary>How many more of each item the relief requests in progress still need.</summary>
    public IReadOnlyDictionary<int, int> AidNeeds(BattleLogicWorld world, IReadOnlyList<int> itemIds)
    {
        try
        {
            ReadDemands(world._AidPlatformManager, _readDemands);

            if (_isAidKnown && ReferenceEquals(itemIds, _aidItemIds) && Same(_readDemands, _demands)) return _aidNeeds;

            var needs = Wanted.AidNeeds(_readDemands, itemIds, AidMatches);

            // The list just read becomes the one kept; the old one is read into next time.
            (_demands, _readDemands) = (_readDemands, _demands);
            _aidItemIds = itemIds;
            _isAidKnown = true;

            if (!Same(needs, _aidNeeds)) _version++;

            _aidNeeds = needs;

            return _aidNeeds;
        }
        catch (Exception ex)
        {
            Report("relief requests", ex);
            ClearAid();

            return NoNeeds;
        }
    }

    /// <summary>The food codex items the player has never eaten, ascending.</summary>
    public IReadOnlyCollection<int> Untried(BattleLogicWorld world, IReadOnlyList<int> foodCodexItems)
    {
        try
        {
            var codex = world._CodexManager;

            HasCodex(codex != null);

            if (codex == null) return NoItems;

            _codex = codex;
            _untried.Check(foodCodexItems, codex.GetUnlockedCount(CodexCategory.Food), _isEaten);

            return _untried.Items;
        }
        catch (Exception ex)
        {
            Report("food codex", ex);

            // Asked again from the start next time, as a failed read used to be.
            _untried.Reset();
            HasCodex(false);

            return NoItems;
        }
        finally
        {
            _codex = null;
        }
    }

    /// <summary>
    /// The game clock in seconds, or null before a player exists. The HUD stats go into values as the bars show them;
    /// values is cleared first.
    /// </summary>
    public int? Stats(BattleLogicWorld world, Dictionary<int, float> values)
    {
        values.Clear();

        try
        {
            var time = world._GameTimeManager;
            var role = world._AgentManager?.GetLeadingRole();

            if (time == null || role == null) return null;

            // The player's attribute component lives as long as the player does.
            if (role.Pointer != _role || _attributes == null)
            {
                _attributes = AgentTools.GetAgentComponent<AttributeComponent>(role);
                _role = _attributes == null ? IntPtr.Zero : role.Pointer;
            }

            var attributes = _attributes;
            var dictionary = attributes?.AttrDict;

            if (attributes == null || dictionary == null) return null;

            var ratio = GameKey.Attr_ScalingRatio;

            // The value the HUD bar shows: a stat kept as a fraction is scaled by the game's ratio, and the bar divides
            // it by that ratio (AttributeComponent.SyncAttr2UI). The whole-number read of a fraction stat only logs a
            // complaint.
            for (var i = 0; ratio > 0f && i < HudStats.Length; i++)
            {
                var name = (AttrName)HudStats[i];

                if (!dictionary.ContainsKey(name)) continue;

                var raw = attributes.IsSingle(name) ? attributes.GetTotalValue_Float(name) * ratio : attributes.GetTotalValue_Int(name);

                values[HudStats[i]] = raw / ratio;
            }

            return time.GetTotalSeconds();
        }
        catch (Exception ex)
        {
            Report("player stats", ex);
            values.Clear();

            return null;
        }
    }

    /// <summary>A new world: what was matched belongs to the old one.</summary>
    public void Reset()
    {
        ClearWish();
        ClearAid();
        _untried.Reset();
        _hasCodex = false;
        _role = IntPtr.Zero;
        _attributes = null;
        _version++;
    }

    private void HasCodex(bool hasCodex)
    {
        if (hasCodex == _hasCodex) return;

        _hasCodex = hasCodex;
        _version++;
    }

    private void ClearWish()
    {
        if (_wishItems.Count > 0) _version++;

        _isWishKnown = false;
        _hasWish = false;
        _wishTargets.Clear();
        _wishItemIds = null;
        _wishItems.Clear();
    }

    private void ClearAid()
    {
        if (_aidNeeds.Count > 0) _version++;

        _isAidKnown = false;
        _demands.Clear();
        _aidItemIds = null;
        _aidNeeds = NoNeeds;
    }

    private void MatchWish(int type, int targetValue, Il2CppSystem.Collections.Generic.List<int>? targetValues, IReadOnlyList<int> itemIds)
    {
        var config = GameSingleton.Existing<ConfigManager>();
        var table = config?._Config_Item_Dict;

        if (table != null)
        {
            for (var i = 0; i < itemIds.Count; i++)
            {
                var id = itemIds[i];

                if (table.TryGetValue(id, out var item) && item != null && Reducer_Web_BackpackUI.IsWishTargetItem(item, id, type, targetValue, targetValues)) _wishItems.Add(id);
            }
        }

        // ClearWish counted a non-empty list going away; this counts the new one arriving.
        if (_wishItems.Count > 0) _version++;
    }

    private static void ReadDemands(AidPlatformManager? platform, List<AidDemand> into)
    {
        into.Clear();

        var requests = platform?.GetDispatchableRequests();

        for (var i = 0; requests != null && i < requests.Count; i++)
        {
            var found = requests[i]?.Demands;

            for (var j = 0; found != null && j < found.Count; j++)
            {
                var demand = found[j];

                if (demand != null) into.Add(new AidDemand(demand.Kind, demand.ItemId, demand.Need, demand.Paid));
            }
        }
    }

    private static bool Same<T>(List<T> a, List<T> b) where T : IEquatable<T>
    {
        if (a.Count != b.Count) return false;

        for (var i = 0; i < a.Count; i++)
        {
            if (!a[i].Equals(b[i])) return false;
        }

        return true;
    }

    private static bool Same(IReadOnlyDictionary<int, int> a, IReadOnlyDictionary<int, int> b)
    {
        if (a.Count != b.Count) return false;

        foreach (var pair in a)
        {
            if (!b.TryGetValue(pair.Key, out var value) || value != pair.Value) return false;
        }

        return true;
    }

    private void Report(string what, Exception ex)
    {
        if (_reportedFailures.Add(what)) _log($"could not read the {what}, its tooltip lines show nothing (reported once): {ex}");
    }
}
