using System;
using System.Diagnostics;
using GameCore.HotUpdate.Battle.Logic;
using InfoPack.Core;
using UnityEngine;

namespace InfoPack.Adapters;

/// <summary>
/// The mod's game side, run once a frame. It keeps the page script installed, answers its messages, sends it the facts
/// once per loaded world and language and the home counts whenever they change. It only reads; nothing here changes the game.
/// </summary>
internal sealed class InfoSession
{
    private const float CheckSeconds = 2f;

    private readonly Features _features;
    private readonly WebPage _page;
    private readonly GameTableReader _tables;
    private readonly GameTextReader _text;
    private readonly Action<string> _log;
    private readonly CountRefresh _counts = new(CheckSeconds);
    private readonly FactsSchedule _factsSchedule = new(CheckSeconds);
    private string? _facts;
    private bool _isPageReady;
    private bool _hasSentFacts;

    public InfoSession(Features features, WebPage page, GameTableReader tables, GameTextReader text, Action<string> log)
    {
        _features = features;
        _page = page;
        _tables = tables;
        _text = text;
        _log = log;
    }

    public void Update()
    {
        var now = Time.unscaledTime;

        _page.KeepInstalled(now);
        TakePageCommands();

        if (!_isPageReady) return;

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
