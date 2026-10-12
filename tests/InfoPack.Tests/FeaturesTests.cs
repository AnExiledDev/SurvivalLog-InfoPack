using System.Text.Json;
using InfoPack.Core;
using Xunit;

namespace InfoPack.Tests;

public class FeaturesTests
{
    private static readonly Features AllOff = new(false, false, false, false, false);

    [Fact]
    public void The_page_reads_each_toggle_under_its_own_flag()
    {
        var flags = JsonDocument.Parse(new Features(true, false, true, false, true).ToJson()).RootElement;

        Assert.True(flags.GetProperty("counts").GetBoolean());
        Assert.False(flags.GetProperty("stats").GetBoolean());
        Assert.True(flags.GetProperty("brew").GetBoolean());
        Assert.False(flags.GetProperty("scrap").GetBoolean());
        Assert.True(flags.GetProperty("codex").GetBoolean());
    }

    [Fact]
    public void With_every_toggle_off_the_mod_has_nothing_to_do()
    {
        Assert.False(AllOff.IsAnyOn);
        Assert.True((AllOff with { ScrapOnly = true }).IsAnyOn);
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
