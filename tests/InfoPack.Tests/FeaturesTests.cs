using System.Linq;
using System.Text.Json;
using InfoPack.Core;
using Xunit;

namespace InfoPack.Tests;

public class FeaturesTests
{
    private static readonly TipLayout NoSections = TipLayout.From(TipSections.DefaultOrder, _ => SectionMode.Off, "Shift", "Ctrl");

    private static readonly Features AllOff = new(false, false, false, false, false, false)
    {
        SpoilBadges = false,
        KeepOnScreen = false,
        Wanted = false,
        NeverTried = false,
        StatRates = false,
        FoodForecast = false,
        Tip = NoSections,
    };

    private static readonly string[] SectionKeys = { "effects", "fresh", "notes", "home", "usedin", "grow", "value", "madefrom", "breaks", "fuel", "foundin", "about" };

    [Fact]
    public void The_page_reads_each_toggle_under_its_own_flag()
    {
        var flags = JsonDocument.Parse(new Features(true, false, true, false, true, true).ToJson()).RootElement;

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
    public void Each_toggle_alone_runs_the_mod()
    {
        Assert.False(AllOff.IsAnyOn);
        Assert.True((AllOff with { BaseCounts = true }).IsAnyOn);
        Assert.True((AllOff with { TradeStats = true }).IsAnyOn);
        Assert.True((AllOff with { AlcoholQuality = true }).IsAnyOn);
        Assert.True((AllOff with { ScrapOnly = true }).IsAnyOn);
        Assert.True((AllOff with { CookingCodex = true }).IsAnyOn);
        Assert.True((AllOff with { PlanningBadges = true }).IsAnyOn);
        Assert.True((AllOff with { Wanted = true }).IsAnyOn);
        Assert.True((AllOff with { NeverTried = true }).IsAnyOn);
        Assert.True((AllOff with { StatRates = true }).IsAnyOn);
        Assert.True((AllOff with { FoodForecast = true }).IsAnyOn);
    }

    [Theory]
    [InlineData("wanted")]
    [InlineData("untried")]
    [InlineData("rates")]
    [InlineData("forecast")]
    public void Each_live_toggle_reaches_the_page_under_its_own_flag(string flag)
    {
        var on = flag switch
        {
            "wanted" => AllOff with { Wanted = true },
            "untried" => AllOff with { NeverTried = true },
            "rates" => AllOff with { StatRates = true },
            _ => AllOff with { FoodForecast = true },
        };
        var flags = JsonDocument.Parse(on.ToJson()).RootElement;

        foreach (var other in new[] { "wanted", "untried", "rates", "forecast" }) Assert.Equal(other == flag, flags.GetProperty(other).GetBoolean());
    }

    [Fact]
    public void Only_the_hud_toggles_measure_the_stats()
    {
        Assert.False(AllOff.NeedsLive);
        Assert.False((AllOff with { Wanted = true }).NeedsDrain);
        Assert.True((AllOff with { StatRates = true }).NeedsDrain);
        Assert.True((AllOff with { FoodForecast = true }).NeedsDrain);
        Assert.True((AllOff with { NeverTried = true }).NeedsLive);
    }

    [Fact]
    public void Spoil_badges_keeping_tooltips_on_screen_or_any_shown_section_each_run_the_mod()
    {
        Assert.True((AllOff with { SpoilBadges = true }).IsAnyOn);
        Assert.True((AllOff with { KeepOnScreen = true }).IsAnyOn);
        Assert.True((AllOff with { Tip = TipLayout.From(TipSections.DefaultOrder, section => section == TipSection.About ? SectionMode.Detail : SectionMode.Off, "Shift", "Ctrl") }).IsAnyOn);
    }

    [Fact]
    public void The_page_gets_every_flag_it_reads_and_the_tooltip_layout()
    {
        var flags = JsonDocument.Parse(new Features(true, true, true, true, true, true).ToJson()).RootElement;

        Assert.Equal(new[] { "counts", "stats", "brew", "scrap", "codex", "plan", "badges", "onScreen", "wanted", "untried", "rates", "forecast", "tip" }, flags.EnumerateObject().Select(member => member.Name));
        Assert.Equal(new[] { "order", "modes", "detailKey", "whereKey" }, flags.GetProperty("tip").EnumerateObject().Select(member => member.Name));
    }

    [Fact]
    public void Spoil_badges_and_keeping_tooltips_on_screen_are_their_own_flags()
    {
        var flags = JsonDocument.Parse((AllOff with { KeepOnScreen = true }).ToJson()).RootElement;

        Assert.False(flags.GetProperty("badges").GetBoolean());
        Assert.True(flags.GetProperty("onScreen").GetBoolean());
    }

    [Fact]
    public void Out_of_the_box_badges_are_on_tooltips_stay_on_screen_and_sections_follow_the_designed_layout()
    {
        var flags = JsonDocument.Parse(new Features(true, true, true, true, true, true).ToJson()).RootElement;
        var tip = flags.GetProperty("tip");

        Assert.True(flags.GetProperty("badges").GetBoolean());
        Assert.True(flags.GetProperty("onScreen").GetBoolean());
        Assert.Equal(SectionKeys, tip.GetProperty("order").EnumerateArray().Select(key => key.GetString()));
        Assert.Equal("always", tip.GetProperty("modes").GetProperty("fresh").GetString());
        Assert.Equal("detail", tip.GetProperty("modes").GetProperty("usedin").GetString());
        Assert.Equal("detail", tip.GetProperty("modes").GetProperty("value").GetString());
        Assert.Equal("Shift", tip.GetProperty("detailKey").GetString());
        Assert.Equal("Ctrl", tip.GetProperty("whereKey").GetString());
    }

    [Fact]
    public void The_page_gets_every_section_in_the_players_order_with_the_players_modes()
    {
        var layout = TipLayout.From(TipOrder.Parse("about, fuel"), section => section == TipSection.Fuel ? SectionMode.Off : SectionMode.Always, "Alt", "F");
        var tip = JsonDocument.Parse((AllOff with { Tip = layout }).ToJson()).RootElement.GetProperty("tip");
        var modes = tip.GetProperty("modes");

        Assert.Equal(new[] { "about", "fuel", "effects" }, tip.GetProperty("order").EnumerateArray().Select(key => key.GetString()).Take(3));
        Assert.Equal(SectionKeys.Length, tip.GetProperty("order").GetArrayLength());
        Assert.Equal(SectionKeys, modes.EnumerateObject().Select(member => member.Name));
        Assert.Equal("off", modes.GetProperty("fuel").GetString());
        Assert.Equal("always", modes.GetProperty("about").GetString());
        Assert.Equal("Alt", tip.GetProperty("detailKey").GetString());
        Assert.Equal("F", tip.GetProperty("whereKey").GetString());
    }

    [Fact]
    public void An_order_that_leaves_sections_out_still_reaches_the_page_whole()
    {
        var layout = TipLayout.From(new[] { TipSection.Grow, TipSection.Grow }, TipSections.DefaultMode, "Shift", "Ctrl");

        Assert.Equal(new[] { TipSection.Grow, TipSection.Effects }, layout.Order.Take(2));
        Assert.Equal(SectionKeys.Length, layout.Order.Count);
    }
}
