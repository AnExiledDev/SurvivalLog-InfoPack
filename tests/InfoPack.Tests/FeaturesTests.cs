using System.Text.Json;
using InfoPack.Core;
using Xunit;

namespace InfoPack.Tests;

public class FeaturesTests
{
    private static readonly Features AllOff = new(false, false, false, false, false, false, false);

    [Fact]
    public void The_page_reads_each_toggle_under_its_own_flag()
    {
        var flags = JsonDocument.Parse(new Features(true, false, true, false, true, true, true).ToJson()).RootElement;

        Assert.True(flags.GetProperty("counts").GetBoolean());
        Assert.False(flags.GetProperty("stats").GetBoolean());
        Assert.True(flags.GetProperty("brew").GetBoolean());
        Assert.False(flags.GetProperty("scrap").GetBoolean());
        Assert.True(flags.GetProperty("codex").GetBoolean());
        Assert.True(flags.GetProperty("plan").GetBoolean());
    }

    [Fact]
    public void Turning_planning_badges_off_tells_the_page_to_leave_the_cards_alone()
    {
        var flags = JsonDocument.Parse((AllOff with { CookingCodex = true }).ToJson()).RootElement;

        Assert.False(flags.GetProperty("plan").GetBoolean());
    }

    [Fact]
    public void With_every_toggle_off_the_mod_has_nothing_to_do()
    {
        Assert.False(AllOff.IsAnyOn);
        Assert.True((AllOff with { ScrapOnly = true }).IsAnyOn);
        Assert.True((AllOff with { PlanningBadges = true }).IsAnyOn);
    }

    [Fact]
    public void Hiding_looted_highlights_alone_still_runs_the_mod_but_needs_no_page()
    {
        var onlyLooted = AllOff with { HideLootedHighlights = true };

        Assert.True(onlyLooted.IsAnyOn);
        Assert.False(onlyLooted.IsAnyPageOn);
    }

    [Fact]
    public void Every_tooltip_piece_needs_the_page()
    {
        Assert.False(AllOff.IsAnyPageOn);
        Assert.True((AllOff with { BaseCounts = true }).IsAnyPageOn);
        Assert.True((AllOff with { TradeStats = true }).IsAnyPageOn);
        Assert.True((AllOff with { AlcoholQuality = true }).IsAnyPageOn);
        Assert.True((AllOff with { ScrapOnly = true }).IsAnyPageOn);
        Assert.True((AllOff with { CookingCodex = true }).IsAnyPageOn);
        Assert.True((AllOff with { PlanningBadges = true }).IsAnyPageOn);
    }

    [Fact]
    public void Stat_lines_are_needed_by_the_trade_tooltip_and_by_the_cooking_codex()
    {
        Assert.False(AllOff.NeedsStats);
        Assert.False((AllOff with { BaseCounts = true, AlcoholQuality = true, ScrapOnly = true }).NeedsStats);
        Assert.True((AllOff with { TradeStats = true }).NeedsStats);
        Assert.True((AllOff with { CookingCodex = true }).NeedsStats);
    }
}
