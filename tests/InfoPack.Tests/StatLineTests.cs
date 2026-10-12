using InfoPack.Core;
using Xunit;

namespace InfoPack.Tests;

public class StatLineTests
{
    private static readonly StatFormat[] Formats =
    {
        new("Satiety +{0}", "Satiety {0}"),
        new("Mood +{0}", "Mood {0}"),
        new("Energy +{0}", "Energy {0}"),
        new("Health +{0}", "Health {0}"),
        new("Life +{0}", "Life {0}"),
    };

    [Fact]
    public void Every_stat_the_item_changes_is_named_in_the_games_order()
    {
        Assert.Equal("Satiety +12, Energy +3, Life +1", StatLine.Describe(Formats, new[] { 12f, 0f, 3f, 0f, 1f }));
    }

    [Fact]
    public void A_loss_uses_the_loss_wording_and_keeps_its_minus_sign()
    {
        Assert.Equal("Mood +6, Energy +3, Life -8", StatLine.Describe(Formats, new[] { 0f, 6f, 3f, 0f, -8f }));
    }

    [Fact]
    public void Fractions_show_one_decimal_and_whole_numbers_none()
    {
        Assert.Equal("Satiety +2.5, Mood +1", StatLine.Describe(Formats, new[] { 2.5f, 1.0f, 0f, 0f, 0f }));
        Assert.Equal("Satiety +0.3", StatLine.Describe(Formats, new[] { 0.333f, 0f, 0f, 0f, 0f }));
    }

    [Fact]
    public void A_change_too_small_to_show_is_left_out_rather_than_printed_as_zero()
    {
        Assert.Null(StatLine.Describe(Formats, new[] { 0.04f, -0.04f, 0f, 0f, 0f }));
    }

    [Fact]
    public void An_item_that_changes_nothing_has_no_line()
    {
        Assert.Null(StatLine.Describe(Formats, new[] { 0f, 0f, 0f, 0f, 0f }));
    }

    [Fact]
    public void Wording_without_a_placeholder_gets_the_number_after_it()
    {
        var formats = new[] { new StatFormat("Satiety", "Satiety") };

        Assert.Equal("Satiety 4", StatLine.Describe(formats, new[] { 4f }));
    }

    [Fact]
    public void Numbers_use_a_dot_whatever_the_players_culture()
    {
        var before = System.Globalization.CultureInfo.CurrentCulture;

        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");

            Assert.Equal("Satiety +2.5", StatLine.Describe(Formats, new[] { 2.5f }));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = before;
        }
    }
}
