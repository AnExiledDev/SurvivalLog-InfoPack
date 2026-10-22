using System;
using System.Diagnostics;
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

    private Features _features;
    private readonly TipKeys _keys;
    private readonly WebPage _page;
    private readonly GameTableReader _tables;
    private readonly GameTextReader _text;
    private readonly Action<string> _log;
    private readonly CountRefresh _counts = new(CheckSeconds);
    private readonly FactsSchedule _factsSchedule = new(CheckSeconds);
    private readonly KeySync _keySync = new();
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
        _log($"facts built from {tables.Items.Count} items ({_facts.Length} characters, language {key.Language ?? "unknown"}) in {timer.ElapsedMilliseconds} ms");
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
}
