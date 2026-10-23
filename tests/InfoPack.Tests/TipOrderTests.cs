using System.Linq;
using InfoPack.Core;
using Xunit;
using static InfoPack.Core.TipSection;

namespace InfoPack.Tests;

public class TipOrderTests
{
    private static readonly TipSection[] Default = { Effects, Fresh, Notes, Home, UsedIn, Grow, Value, MadeFrom, Breaks, Fuel, FoundIn, About };

    [Fact]
    public void The_default_order_is_the_one_the_tooltip_is_designed_around()
    {
        Assert.Equal(Default, TipSections.DefaultOrder);
        Assert.Equal(Default, TipOrder.Parse(TipOrder.DefaultText));
    }

    [Fact]
    public void Sections_named_first_come_first_and_the_rest_keep_their_usual_places()
    {
        var order = TipOrder.Parse("About, Value");

        Assert.Equal(new[] { About, Value, Effects, Fresh, Notes, Home, UsedIn, Grow, MadeFrom, Breaks, Fuel, FoundIn }, order);
    }

    [Fact]
    public void Typing_is_forgiven_case_and_spaces_do_not_matter()
    {
        Assert.Equal(TipOrder.Parse("about,value"), TipOrder.Parse("  ABOUT ,   Value  "));
        Assert.Equal(new[] { UsedIn, FoundIn }, TipOrder.Parse("usedin,FOUNDIN").Take(2));
    }

    [Fact]
    public void An_unknown_or_repeated_name_is_passed_over()
    {
        var order = TipOrder.Parse("Fuel, Smell, fuel, , About, Fuel");

        Assert.Equal(new[] { Fuel, About, Effects }, order.Take(3));
        Assert.Equal(Default.Length, order.Count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(",,,")]
    [InlineData("nonsense")]
    public void A_blank_or_useless_setting_gives_the_default_order(string? text)
    {
        Assert.Equal(Default, TipOrder.Parse(text));
    }

    [Fact]
    public void Every_section_is_always_listed_exactly_once()
    {
        var order = TipOrder.Parse("Grow, grow, About, Effects, Effects");

        Assert.Equal(Default.Length, order.Count);
        Assert.Equal(Default.OrderBy(section => section), order.OrderBy(section => section));
    }

    [Fact]
    public void The_order_setting_and_the_page_name_every_section_alike()
    {
        Assert.All(Default, section => Assert.True(TipSections.TryParse(TipSections.Key(section), out var parsed) && parsed == section));
        Assert.All(Default, section => Assert.True(TipSections.TryParse(section.ToString(), out var parsed) && parsed == section));
    }

    [Fact]
    public void Short_tooltips_hold_the_everyday_sections_and_the_rest_wait_for_the_detail_key()
    {
        var always = Default.Where(section => TipSections.DefaultMode(section) == SectionMode.Always);
        var detail = Default.Where(section => TipSections.DefaultMode(section) == SectionMode.Detail);

        Assert.Equal(new[] { Effects, Fresh }, always);
        Assert.Equal(new[] { Notes, Home, UsedIn, Grow, Value, MadeFrom, Breaks, Fuel, FoundIn, About }, detail);
    }

    [Theory]
    [InlineData("Always", SectionMode.Always)]
    [InlineData(" detail ", SectionMode.Detail)]
    [InlineData("OFF", SectionMode.Off)]
    public void A_section_setting_reads_as_its_mode_whatever_the_case(string value, SectionMode mode)
    {
        Assert.Equal(mode, SectionModes.Parse(value, Value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Sometimes")]
    public void A_section_setting_the_mod_cannot_read_keeps_that_section_at_its_default(string? value)
    {
        Assert.Equal(SectionMode.Always, SectionModes.Parse(value, Effects));
        Assert.Equal(SectionMode.Detail, SectionModes.Parse(value, About));
    }

    [Fact]
    public void Every_choice_a_section_setting_offers_reads_back_as_a_mode()
    {
        Assert.Equal(new[] { SectionMode.Always, SectionMode.Detail, SectionMode.Off }, SectionModes.Choices.Select(choice => SectionModes.Parse(choice, About)));
    }
}
