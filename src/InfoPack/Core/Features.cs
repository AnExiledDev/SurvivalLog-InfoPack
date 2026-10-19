namespace InfoPack.Core;

/// <summary>Which pieces the player left on. Each one is its own config toggle and can be turned off alone.</summary>
public sealed record Features(
    bool BaseCounts,
    bool TradeStats,
    bool AlcoholQuality,
    bool ScrapOnly,
    bool CookingCodex,
    bool PlanningBadges)
{
    /// <summary>Every piece is drawn by the page script, so with all of them off the mod has nothing to do.</summary>
    public bool IsAnyOn => BaseCounts || TradeStats || AlcoholQuality || ScrapOnly || CookingCodex || PlanningBadges;

    /// <summary>The stat line is shown by two pieces: the trade tooltip and the cooking codex section.</summary>
    public bool NeedsStats => TradeStats || CookingCodex;

    /// <summary>The flags the page script reads to decide what to draw.</summary>
    public string ToJson() =>
        "{\"counts\":" + Flag(BaseCounts)
        + ",\"stats\":" + Flag(TradeStats)
        + ",\"brew\":" + Flag(AlcoholQuality)
        + ",\"scrap\":" + Flag(ScrapOnly)
        + ",\"codex\":" + Flag(CookingCodex)
        + ",\"plan\":" + Flag(PlanningBadges)
        + "}";

    private static string Flag(bool isOn) => isOn ? "true" : "false";
}
