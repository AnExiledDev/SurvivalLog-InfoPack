using System;
using System.Collections.Generic;
using System.Diagnostics;
using GameCore.HotUpdate.Battle.Logic;
using InfoPack.Core;

namespace InfoPack.Adapters;

/// <summary>
/// The mod's game side, run once a frame. It keeps the page script installed, answers its messages, sends it the facts
/// once per loaded world and language, the home counts whenever they change, and the detail and breakdown keys whenever
/// they are pressed or let go. It only reads; nothing here changes the game. A frame does at most one of the heavier
/// jobs (the install check and the facts, the home count, the live pieces), and a frame where nothing is due reads only
/// the keys.
/// </summary>
internal sealed class InfoSession
{
    private const float CheckSeconds = 2f;
    private const int DrainWindowSeconds = 4 * 3600;
    private const int DrainMinimumSeconds = 1800;
    private const int Satiety = 1;

    /// <summary>How long a frame may spend on the facts before the rest waits for the next frame.</summary>
    private static readonly long SliceTicks = Stopwatch.Frequency * 2 / 1000;

    private static readonly IReadOnlyCollection<int> NoItems = Array.Empty<int>();
    private static readonly IReadOnlyDictionary<int, int> NoNeeds = new SortedDictionary<int, int>();

    private Features _features;
    private readonly TipKeys _keys;
    private readonly WebPage _page;
    private readonly GameTableReader _tables;
    private readonly GameTextReader _text;
    private readonly Action<string> _log;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly CountRefresh _counts = new(CheckSeconds);
    private readonly FactsSchedule _factsSchedule = new(CheckSeconds);
    private readonly KeySync _keySync = new();
    private readonly LiveReader _live;
    private readonly CountRefresh _wanted = new(CheckSeconds);
    private readonly StatDrain _drain = new(DrainWindowSeconds, DrainMinimumSeconds);
    private readonly StatsPrint _statsPrint = new(LiveReader.HudStats);
    private readonly Dictionary<int, float> _statValues = new();
    private readonly HomeItems _home;
    private readonly HomeTally _tally = new();
    private readonly TextCopies _textCopies = new();
    private readonly Func<bool> _isSliceSpent;
    private long _sliceEndsAt;
    private SlicedJob? _factsJob;
    private LiveLists? _lists;
    private IReadOnlyList<ItemRow>? _listsItems;
    private long _liveWorld;
    private bool _isTallyFresh;
    private string? _facts;
    private string? _sentFacts;
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
        _home = new HomeItems(log);
        _isSliceSpent = () => Stopwatch.GetTimestamp() >= _sliceEndsAt;
    }

    /// <summary>
    /// Called on the main thread at start and after each change. The page gets the new flags at once (it clears what it
    /// drew and draws again), and the facts are built again for what is now on.
    /// </summary>
    public void UseFeatures(Features features)
    {
        _features = features;
        _factsSchedule.SettingsChanged();

        // Facts being built were built for the old settings.
        _factsJob = null;

        // What the live messages hold follows the settings, which no stamp shows.
        _wanted.Forget();
        _statsPrint.Forget();

        if (_isPageReady) _page.Features(features.ToJson());
    }

    public void Update()
    {
        var now = (float)_clock.Elapsed.TotalSeconds;
        var isBusy = _page.KeepInstalled(now);

        TakePageCommands();

        if (!_isPageReady) return;

        SendKeys();
        isBusy |= CheckFacts(now);
        SendFacts();

        // The home count and the live pieces each read the whole home, so they never share a frame, nor one that
        // already read the game for the install check or the facts.
        if (isBusy) return;

        if (!SendCounts(now)) SendLive(now);
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
                    _page.Instance(instance.ItemId, _home.ConfigOfInstance(instance.ItemId));
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
        _sentFacts = null;
        _counts.PageReloaded();
        _wanted.PageReloaded();
        _statsPrint.Forget();
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
            if (!_home.TryRead()) return;

            var isChinese = _text.IsChinese();
            var rows = WhereKept.Rows(_home, storageId => _home.StorageName(storageId, isChinese), configId);

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
    /// A world whose tables are not loaded yet is tried again at the next look. The build is spread over frames, a
    /// slice each, and its heavy middle runs on a worker. True when this frame read the game for them.
    /// </summary>
    private bool CheckFacts(float now)
    {
        if (_factsJob != null)
        {
            AdvanceFacts();

            return true;
        }

        if (!_factsSchedule.IsDue(now)) return false;

        _factsSchedule.Checked(now);

        var world = GameSingleton.Existing<BattleLogicWorld>();

        if (world == null) return true;

        var key = new FactsKey(world.Pointer.ToInt64(), _text.LanguageKey(), world._AgentManager?.GetLeadingRole() != null);

        if (!_factsSchedule.NeedsBuild(key) || !_tables.IsLoaded()) return true;

        // Marked as built before building, so a build that throws is reported once by the ticker and not retried at every look.
        _factsSchedule.Building(key);
        _factsJob = new SlicedJob(BuildFacts(key, _features, Stopwatch.StartNew()));
        AdvanceFacts();

        return true;
    }

    private void AdvanceFacts()
    {
        var job = _factsJob!;

        _sliceEndsAt = Stopwatch.GetTimestamp() + SliceTicks;

        try
        {
            if (job.Advance(_isSliceSpent) && _factsJob == job) _factsJob = null;
        }
        catch (Exception)
        {
            // Dropped, so the next look starts clean; the ticker reports the failure once.
            if (_factsJob == job) _factsJob = null;

            throw;
        }
    }

    private IEnumerable<bool> BuildFacts(FactsKey key, Features features, Stopwatch timer)
    {
        GameTables? tables = null;

        foreach (var step in _tables.Read(read => tables = read)) yield return step;

        if (tables == null) yield break;

        var text = _textCopies.For(key, tables);

        foreach (var step in FactsBuild.Steps(tables, text, _text, features, FactsBuild.OnWorker, built => Built(key, tables, built, timer))) yield return step;
    }

    private void Built(FactsKey key, GameTables tables, BuiltFacts built, Stopwatch timer)
    {
        _facts = built.Json;

        // Facts the page already holds are not sent again.
        if (built.Json != _sentFacts) _hasSentFacts = false;

        // The same tables give the same lists; keeping them keeps what the live pieces matched against them.
        if (_lists == null || !ReferenceEquals(_listsItems, tables.Items))
        {
            _lists = built.Lists;
            _listsItems = tables.Items;
        }

        _log($"facts built from {tables.Items.Count} items ({built.Json.Length} characters, language {key.Language ?? "unknown"}, player {(key.HasPlayer ? "found" : "not yet")}) in {timer.ElapsedMilliseconds} ms");
    }

    private void SendFacts()
    {
        if (_facts == null || _hasSentFacts) return;

        _page.Facts(_facts);
        _sentFacts = _facts;
        _hasSentFacts = true;
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

    /// <summary>The message is only built when one of the answers changed since the one the page holds.</summary>
    private void SendWanted(BattleLogicWorld world, LiveLists lists)
    {
        var wish = _features.Wanted ? _live.WishItems(world, lists.ItemIds) : NoItems;
        var aid = _features.Wanted ? _live.AidNeeds(world, lists.ItemIds) : NoNeeds;
        var untried = _features.NeverTried ? _live.Untried(world, lists.FoodCodex) : NoItems;
        var version = _live.Version;

        if (_wanted.Holds(version)) return;

        var json = Wanted.ToJson(wish, aid, untried);

        if (_wanted.ShouldSend(json, version)) _page.Wanted(json);
    }

    /// <summary>The message is only built when a number the page shows changed.</summary>
    private void SendStats(BattleLogicWorld world, LiveLists lists)
    {
        if (_live.Stats(world, _statValues) is not { } seconds) return;

        _drain.Add(seconds, _statValues);

        var days = _features.FoodForecast ? FoodDays(lists) : null;

        if (_statsPrint.Update(_drain, _features.StatRates, days)) _page.Stats(_statsPrint.Json());
    }

    private float? FoodDays(LiveLists lists)
    {
        // The count from the frame before is reused when there is one, so the home is not counted twice.
        var isFresh = _isTallyFresh;

        _isTallyFresh = false;

        if (!isFresh)
        {
            if (!_home.TryRead()) return null;

            _tally.Count(_home);
        }

        return FoodForecast.Days(FoodForecast.Satiety(_tally.Counts, lists.Satiety), _drain.PerHour(Satiety));
    }

    /// <summary>True when it counted this frame. The message is only built when the counts changed since the one the page holds.</summary>
    private bool SendCounts(float now)
    {
        if (!_features.BaseCounts || !_counts.IsDue(now)) return false;

        _counts.Counted(now);
        _isTallyFresh = false;

        if (!_home.TryRead()) return true;

        _tally.Count(_home);
        _isTallyFresh = true;

        if (_counts.Holds(_tally.Version)) return true;

        var counts = PageFacts.Counts(_tally.Counts);

        if (_counts.ShouldSend(counts, _tally.Version)) _page.Counts(counts);

        return true;
    }
}
