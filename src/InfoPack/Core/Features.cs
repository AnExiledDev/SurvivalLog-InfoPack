namespace InfoPack.Core;

/// <summary>Which pieces the player left on. Each one is its own config setting and can be turned off alone.</summary>
public sealed record Features(
    bool BaseCounts,
    bool TradeStats,
    bool AlcoholQuality,
    bool ScrapOnly,
    bool CookingCodex,
    bool PlanningBadges)
{
    /// <summary>Spoil badges on backpack and storage slots.</summary>
    public bool SpoilBadges { get; init; } = true;

    /// <summary>Tooltips are moved back inside the window when they would run past its edge.</summary>
    public bool KeepOnScreen { get; init; } = true;

    public TipLayout Tip { get; init; } = TipLayout.Default;

    /// <summary>Tooltip lines and a backpack star for what your craving and accepted relief requests want.</summary>
    public bool Wanted { get; init; } = true;

    /// <summary>A tooltip line on food and drink you have never had.</summary>
    public bool NeverTried { get; init; } = true;

    /// <summary>How fast each HUD stat drops, and when it runs out, in the stat bar's tooltip.</summary>
    public bool StatRates { get; init; } = true;

    /// <summary>How many days the food at home lasts, in the satiety bar's tooltip.</summary>
    public bool FoodForecast { get; init; } = true;

    /// <summary>The stat drain is measured for the rates and for the forecast, which uses the satiety drain.</summary>
    public bool NeedsDrain => StatRates || FoodForecast;

    /// <summary>Anything that reads the save as it plays, beyond the home counts.</summary>
    public bool NeedsLive => Wanted || NeverTried || NeedsDrain;

    /// <summary>Every piece is drawn by the page script, so with all of them off the mod has nothing to do.</summary>
    public bool IsAnyOn =>
        BaseCounts || TradeStats || AlcoholQuality || ScrapOnly || CookingCodex || PlanningBadges
        || SpoilBadges || KeepOnScreen || Tip.ShowsAnySection || NeedsLive;

    /// <summary>The flags the page script reads to decide what to draw.</summary>
    public string ToJson() =>
        "{\"counts\":" + Flag(BaseCounts)
        + ",\"stats\":" + Flag(TradeStats)
        + ",\"brew\":" + Flag(AlcoholQuality)
        + ",\"scrap\":" + Flag(ScrapOnly)
        + ",\"codex\":" + Flag(CookingCodex)
        + ",\"plan\":" + Flag(PlanningBadges)
        + ",\"badges\":" + Flag(SpoilBadges)
        + ",\"onScreen\":" + Flag(KeepOnScreen)
        + ",\"wanted\":" + Flag(Wanted)
        + ",\"untried\":" + Flag(NeverTried)
        + ",\"rates\":" + Flag(StatRates)
        + ",\"forecast\":" + Flag(FoodForecast)
        + ",\"tip\":" + Tip.ToJson()
        + "}";

    private static string Flag(bool isOn) => isOn ? "true" : "false";
}
