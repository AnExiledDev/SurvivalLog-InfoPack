using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using GameCore.HotUpdate.ReduxUI;
using InfoPack.Core;

namespace InfoPack.Adapters;

/// <summary>
/// What the save says right now: the player's craving, the relief requests in progress, which foods the codex has, the
/// player's stats and the game clock. Read only. Matching every item against a craving or a request is the slow part,
/// so it is done again only when the craving or the requests change. Each piece fails on its own and is logged once.
/// </summary>
internal sealed class LiveReader
{
    /// <summary>The HUD's four bars by AttrName: satiety, mood, energy, life.</summary>
    public static readonly int[] HudStats = { (int)AttrName.Satiety, (int)AttrName.Morale, (int)AttrName.Stamina, (int)AttrName.Vitality };

    private readonly Action<string> _log;
    private readonly HashSet<string> _reportedFailures = new();
    private string? _wishKey;
    private readonly HashSet<int> _tried = new();
    private IReadOnlyCollection<int> _wishItems = Array.Empty<int>();
    private string? _aidKey;
    private IReadOnlyDictionary<int, int> _aidNeeds = new SortedDictionary<int, int>();

    public LiveReader(Action<string> log)
    {
        _log = log;
    }

    /// <summary>The items that would satisfy the craving the player has now; none without one.</summary>
    public IReadOnlyCollection<int> WishItems(BattleLogicWorld world, IReadOnlyList<int> itemIds) => Guarded("craving", () =>
    {
        var wishes = world._WishManager;
        var wish = wishes == null || wishes.IsLocked ? null : wishes.currentWish;

        if (wish == null || wish.IsCompleted || wish.IsExpired) return Remember(ref _wishKey, ref _wishItems, "", () => Array.Empty<int>());

        var targets = Ints(wish.TargetValues);
        var key = (int)wish.Type + ":" + wish.TargetValue + ":" + string.Join(",", targets);

        return Remember(ref _wishKey, ref _wishItems, key, () => MatchWish((int)wish.Type, wish.TargetValue, wish.TargetValues, itemIds));
    }, Array.Empty<int>());

    /// <summary>How many more of each item the relief requests in progress still need.</summary>
    public IReadOnlyDictionary<int, int> AidNeeds(BattleLogicWorld world, IReadOnlyList<int> itemIds) => Guarded("relief requests", () =>
    {
        var demands = Demands(world._AidPlatformManager);
        var key = string.Join(";", demands.Select(demand => demand.Kind + ":" + demand.ItemId + ":" + demand.Need + ":" + demand.Paid));

        return Remember(ref _aidKey, ref _aidNeeds, key, () => Wanted.AidNeeds(demands, itemIds, (demandItem, item) => AidPlatformManager.DemandMatchTier(demandItem, item) > 0));
    }, new SortedDictionary<int, int>());

    /// <summary>The food codex items the player has never eaten.</summary>
    public IReadOnlyCollection<int> Untried(BattleLogicWorld world, IReadOnlyList<int> foodCodexItems) => Guarded("food codex", () =>
    {
        var codex = world._CodexManager;

        // A food once eaten stays in the codex, so only the ones not eaten yet are asked again.
        return codex == null ? new SortedSet<int>() : Wanted.Untried(foodCodexItems, id => _tried.Contains(id) || (codex.IsEntryUnlocked(CodexCategory.Food, id) && _tried.Add(id)));
    }, new SortedSet<int>());

    /// <summary>The game clock in seconds and the HUD stats as the bars show them, or null before a player exists.</summary>
    public (int Seconds, Dictionary<int, float> Values)? Stats(BattleLogicWorld world) => Guarded<(int, Dictionary<int, float>)?>("player stats", () =>
    {
        var time = world._GameTimeManager;
        var role = world._AgentManager?.GetLeadingRole();
        var attributes = role == null ? null : AgentTools.GetAgentComponent<AttributeComponent>(role);

        if (time == null || attributes?.AttrDict == null) return null;

        var values = new Dictionary<int, float>();

        foreach (var stat in HudStats)
        {
            if (Shown(attributes, (AttrName)stat) is { } value) values[stat] = value;
        }

        return (time.GetTotalSeconds(), values);
    }, null);

    /// <summary>A new world: what was matched belongs to the old one.</summary>
    public void Reset()
    {
        _wishKey = null;
        _aidKey = null;
        _tried.Clear();
    }

    private static IReadOnlyCollection<int> MatchWish(int type, int targetValue, Il2CppSystem.Collections.Generic.List<int>? targetValues, IReadOnlyList<int> itemIds)
    {
        var config = GameSingleton.Existing<ConfigManager>();
        var matched = new SortedSet<int>();

        if (config == null) return matched;

        foreach (var id in itemIds)
        {
            var item = config.Get_Config_Item(id);

            if (item != null && Reducer_Web_BackpackUI.IsWishTargetItem(item, id, type, targetValue, targetValues)) matched.Add(id);
        }

        return matched;
    }

    private static List<AidDemand> Demands(AidPlatformManager? platform)
    {
        var demands = new List<AidDemand>();
        var requests = platform?.GetDispatchableRequests();

        for (var i = 0; requests != null && i < requests.Count; i++)
        {
            var found = requests[i]?.Demands;

            for (var j = 0; found != null && j < found.Count; j++)
            {
                var demand = found[j];

                if (demand != null) demands.Add(new AidDemand(demand.Kind, demand.ItemId, demand.Need, demand.Paid));
            }
        }

        return demands;
    }

    /// <summary>
    /// The value the HUD bar shows: a stat kept as a fraction is scaled by the game's ratio, and the bar divides it by
    /// that ratio (AttributeComponent.SyncAttr2UI). The whole-number read of a fraction stat only logs a complaint.
    /// </summary>
    private static float? Shown(AttributeComponent attributes, AttrName name)
    {
        if (!attributes.AttrDict.ContainsKey(name)) return null;

        var ratio = GameKey.Attr_ScalingRatio;
        var raw = attributes.IsSingle(name) ? attributes.GetTotalValue_Float(name) * ratio : attributes.GetTotalValue_Int(name);

        return ratio > 0f ? raw / ratio : null;
    }

    private static T Remember<T>(ref string? cachedKey, ref T cached, string key, Func<T> work)
    {
        if (cachedKey == key) return cached;

        cached = work();
        cachedKey = key;

        return cached;
    }

    private static List<int> Ints(Il2CppSystem.Collections.Generic.List<int>? list)
    {
        var ints = new List<int>();

        for (var i = 0; list != null && i < list.Count; i++) ints.Add(list[i]);

        return ints;
    }

    private T Guarded<T>(string what, Func<T> read, T empty)
    {
        try
        {
            return read();
        }
        catch (Exception ex)
        {
            if (_reportedFailures.Add(what)) _log($"could not read the {what}, its tooltip lines show nothing (reported once): {ex}");

            return empty;
        }
    }
}
