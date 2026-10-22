using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GameCore.HotUpdate.Battle.Logic;
using InfoPack.Core;
using UnityEngine;

namespace InfoPack.Adapters;

/// <summary>
/// The mod's game side, run once a frame. It keeps the page script installed, answers its messages, sends it the facts
/// once per loaded world and language, the home counts whenever they change, and the detail and breakdown keys whenever
/// they are pressed or let go. It only reads; nothing here changes the game.
/// </summary>
internal sealed class InfoSession
{
    private const float CheckSeconds = 2f;
    private const int DrainWindowSeconds = 4 * 3600;
    private const int DrainMinimumSeconds = 1800;
    private const int Satiety = 1;

    private Features _features;
    private readonly TipKeys _keys;
    private readonly WebPage _page;
    private readonly GameTableReader _tables;
    private readonly GameTextReader _text;
    private readonly Action<string> _log;
    private readonly CountRefresh _counts = new(CheckSeconds);
    private readonly FactsSchedule _factsSchedule = new(CheckSeconds);
    private readonly KeySync _keySync = new();
    private readonly LiveReader _live;
    private readonly CountRefresh _wanted = new(CheckSeconds);
    private readonly CountRefresh _stats = new(CheckSeconds);
    private readonly StatDrain _drain = new(DrainWindowSeconds, DrainMinimumSeconds);
    private LiveLists? _lists;
    private long _liveWorld;
    private string? _facts;
    private bool _isPageReady;
    private bool _hasSentFacts;
    private bool _hasReportedWhereFailure;

    public InfoSession(Features features, TipKeys keys, WebPage page, GameTableReader tables, GameTextReader text, Action<string> log)
    {
        _features = features;
        _keys = keys;
        _page = page;
        _tables = tables;
        _text = text;
        _log = log;
        _live = new LiveReader(log);
    }

    /// <summary>
    /// Called on the main thread at start and after each change. The page gets the new flags at once (it clears what it
    /// drew and draws again), and the facts are built again for what is now on.
    /// </summary>
    public void UseFeatures(Features features)
    {
        _features = features;
        _factsSchedule.SettingsChanged();

        if (_isPageReady) _page.Features(features.ToJson());
    }

    public void Update()
    {
        var now = Time.unscaledTime;

        _page.KeepInstalled(now);
        TakePageCommands();

        if (!_isPageReady) return;

        SendKeys();
        CheckFacts(now);
        SendFacts();
        SendCounts(now);
        SendLive(now);
    }

    private void TakePageCommands()
    {
        while (PageMessages.TryTake(out var command))
        {
            switch (command)
            {
                case PageCommand.Ready:
                    _log("page script ready");
                    PageReloaded();
                    _page.Features(_features.ToJson());
                    break;
                case PageCommand.NoScript:
                    _isPageReady = false;
                    _page.Install();
                    break;
                case PageCommand.Instance instance:
                    _page.Instance(instance.ItemId, HomeItems.ConfigOfInstance(instance.ItemId));
                    break;
                case PageCommand.Where where:
                    AnswerWhere(where.ConfigId);
                    break;
                case PageCommand.Unknown unknown:
                    _log("page said: " + unknown.Text);
                    break;
                default:
                    throw new InvalidOperationException("unhandled page command " + command);
            }
        }
    }

    /// <summary>A fresh script holds nothing, so everything goes again.</summary>
    private void PageReloaded()
    {
        _isPageReady = true;
        _hasSentFacts = false;
        _counts.PageReloaded();
        _wanted.PageReloaded();
        _stats.PageReloaded();
        _factsSchedule.PageReloaded();
        _keySync.PageReloaded();
    }

    private void SendKeys()
    {
        var held = _keys.Read();

        if (_keySync.ShouldSend(held)) _page.Keys(held.ToJson());
    }

    /// <summary>
    /// The breakdown belongs to the home count, so it is only answered while the count is on. A home that cannot be
    /// read yet gets no answer and the page asks again; a failure is logged once and the tooltip keeps its count.
    /// </summary>
    private void AnswerWhere(int configId)
    {
        if (!_features.BaseCounts) return;

        try
        {
            var home = HomeItems.TryRead();

            if (home == null) return;

            var isChinese = _text.IsChinese();
            var rows = WhereKept.Rows(home, storageId => home.StorageName(storageId, isChinese), configId);

            _page.Where(configId, WhereKept.ToJson(rows));
        }
        catch (Exception ex)
        {
            if (!_hasReportedWhereFailure) _log("could not list where an item is kept (reported once): " + ex.Message);

            _hasReportedWhereFailure = true;
        }
    }

    /// <summary>
    /// The facts are built once per loaded world and language: the tables do not change while a world runs, and the
    /// descriptions are in the language the game shows. Both are looked at on the count cadence rather than every frame.
    /// A world whose tables are not loaded yet is tried again at the next look.
    /// </summary>
    private void CheckFacts(float now)
    {
        if (!_factsSchedule.IsDue(now)) return;

        _factsSchedule.Checked(now);

        var world = GameSingleton.Existing<BattleLogicWorld>();

        if (world == null) return;

        var key = new FactsKey(world.Pointer.ToInt64(), _text.LanguageKey());

        if (_factsSchedule.NeedsBuild(key)) BuildFacts(key);
    }

    private void SendFacts()
    {
        if (_facts == null || _hasSentFacts) return;

        _page.Facts(_facts);
        _hasSentFacts = true;
    }

    private void BuildFacts(FactsKey key)
    {
        var timer = Stopwatch.StartNew();
        var tables = _tables.TryRead();

        if (tables == null) return;

        // Marked as built before building, so a build that throws is reported once by the ticker and not retried at every look.
        _factsSchedule.Building(key);
        _facts = null;
        _hasSentFacts = false;
        _facts = PageFacts.Build(tables, _text, _features);
        _lists = new LiveLists(
            tables.Items.Select(item => item.Id).Distinct().ToList(),
            tables.Items.Where(item => item.InFoodCodex).Select(item => item.Id).Distinct().ToList(),
            FoodForecast.SatietyById(tables.Items));
        _log($"facts built from {tables.Items.Count} items ({_facts.Length} characters, language {key.Language ?? "unknown"}) in {timer.ElapsedMilliseconds} ms");
    }

    /// <summary>
    /// The craving, the relief requests, the untried foods and the stat drain, on the count cadence. The drain keeps
    /// measuring while its page pieces are on; a new world starts it over.
    /// </summary>
    private void SendLive(float now)
    {
        if (!_features.NeedsLive || _lists == null || !_wanted.IsDue(now)) return;

        _wanted.Counted(now);

        var world = GameSingleton.Existing<BattleLogicWorld>();

        if (world == null) return;

        var worldKey = world.Pointer.ToInt64();

        if (worldKey != _liveWorld)
        {
            _liveWorld = worldKey;
            _drain.Reset();
            _live.Reset();
        }

        if (_features.Wanted || _features.NeverTried) SendWanted(world, _lists);

        if (_features.NeedsDrain) SendStats(world, _lists);
    }

    private void SendWanted(BattleLogicWorld world, LiveLists lists)
    {
        var none = Array.Empty<int>();
        var wish = _features.Wanted ? _live.WishItems(world, lists.ItemIds) : none;
        var aid = _features.Wanted ? _live.AidNeeds(world, lists.ItemIds) : new SortedDictionary<int, int>();
        var untried = _features.NeverTried ? _live.Untried(world, lists.FoodCodex) : none;
        var json = Wanted.ToJson(wish, aid, untried);

        if (_wanted.ShouldSend(json)) _page.Wanted(json);
    }

    private void SendStats(BattleLogicWorld world, LiveLists lists)
    {
        if (_live.Stats(world) is not { } reading) return;

        _drain.Add(reading.Seconds, reading.Values);

        var rates = _features.StatRates ? _drain.ToJson(LiveReader.HudStats) : "{}";
        var days = _features.FoodForecast ? FoodDays(lists) : null;
        var json = FoodForecast.StatsJson(rates, days);

        if (_stats.ShouldSend(json)) _page.Stats(json);
    }

    private float? FoodDays(LiveLists lists)
    {
        var home = HomeItems.TryRead();

        return home == null ? null : FoodForecast.Days(FoodForecast.Satiety(HomeCounts.Count(home), lists.Satiety), _drain.PerHour(Satiety));
    }

    private void SendCounts(float now)
    {
        if (!_features.BaseCounts || !_counts.IsDue(now)) return;

        _counts.Counted(now);

        var home = HomeItems.TryRead();

        if (home == null) return;

        var counts = PageFacts.Counts(HomeCounts.Count(home));

        if (_counts.ShouldSend(counts)) _page.Counts(counts);
    }

    /// <summary>What the live pieces are matched against, taken from the tables when the facts are built.</summary>
    private sealed record LiveLists(IReadOnlyList<int> ItemIds, IReadOnlyList<int> FoodCodex, IReadOnlyDictionary<int, float> Satiety);
}
