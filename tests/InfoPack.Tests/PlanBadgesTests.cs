using System.Collections.Generic;
using InfoPack.Core;
using Xunit;

namespace InfoPack.Tests;

public class PlanBadgesTests
{
    private const int Card = PlanBadges.PlanningCardType;

    [Fact]
    public void A_buff_that_runs_for_a_while_makes_a_timed_card()
    {
        Assert.Equal(new PlanDuration.Timed(259200), PlanBadges.Classify(259200f));
        Assert.Equal(new PlanDuration.Timed(21600), PlanBadges.Classify(21600f));
    }

    [Fact]
    public void A_buff_of_one_second_is_the_games_mark_for_a_one_off_card()
    {
        Assert.Equal(new PlanDuration.OneOff(), PlanBadges.Classify(1f));
    }

    [Fact]
    public void A_buff_that_never_ends_or_no_buff_at_all_makes_a_permanent_card()
    {
        Assert.Equal(new PlanDuration.Permanent(), PlanBadges.Classify(-1f));
        Assert.Equal(new PlanDuration.Permanent(), PlanBadges.Classify(null));
        Assert.Equal(new PlanDuration.Permanent(), PlanBadges.Classify(0f));
    }

    [Fact]
    public void A_duration_that_is_not_a_number_is_not_shown_as_a_length()
    {
        Assert.Equal(new PlanDuration.Permanent(), PlanBadges.Classify(float.PositiveInfinity));
        Assert.Equal(new PlanDuration.Permanent(), PlanBadges.Classify(float.NaN));
    }

    [Fact]
    public void Timed_cards_show_their_length_one_off_cards_show_1x_and_permanent_cards_show_nothing()
    {
        Assert.Equal("3d", PlanBadges.Badge(new PlanDuration.Timed(259200)));
        Assert.Equal("1x", PlanBadges.Badge(new PlanDuration.OneOff()));
        Assert.Null(PlanBadges.Badge(new PlanDuration.Permanent()));
    }

    [Theory]
    [InlineData(432000d, "5d")]
    [InlineData(259200d, "3d")]
    [InlineData(172800d, "2d")]
    [InlineData(86400d, "24h")]
    [InlineData(129600d, "36h")]
    [InlineData(21600d, "6h")]
    [InlineData(3600d, "1h")]
    [InlineData(5400d, "90m")]
    [InlineData(1800d, "30m")]
    [InlineData(30d, "1m")]
    public void Lengths_read_in_days_from_two_days_then_whole_hours_then_minutes(double seconds, string badge)
    {
        Assert.Equal(badge, PlanBadges.Length(seconds));
    }

    [Fact]
    public void A_partial_minute_rounds_up_so_a_card_never_looks_shorter_than_it_lasts()
    {
        Assert.Equal("2m", PlanBadges.Length(61));
    }

    [Fact]
    public void Only_planning_cards_with_a_timed_or_one_off_buff_get_a_badge()
    {
        var badges = PlanBadges.Map(
            new[]
            {
                new TalentRow(31411, Card, 314110),
                new TalentRow(30651, Card, 306510),
                new TalentRow(31501, Card, 315010),
                new TalentRow(31601, Card, 0),
                new TalentRow(31700, Card, 317000),
                new TalentRow(31800, Card, 999999),
                new TalentRow(10011, 1, 100110),
            },
            new Dictionary<int, float>
            {
                [314110] = 21600f,
                [306510] = 259200f,
                [315010] = 1f,
                [317000] = -1f,
                [100110] = 86400f,
            });

        Assert.Equal(new SortedDictionary<int, string> { [30651] = "3d", [31411] = "6h", [31501] = "1x" }, badges);
    }

    [Fact]
    public void A_card_whose_buff_id_is_zero_gets_no_badge_even_if_the_buff_table_has_a_row_zero()
    {
        var badges = PlanBadges.Map(new[] { new TalentRow(31601, Card, 0) }, new Dictionary<int, float> { [0] = 3600f });

        Assert.Empty(badges);
    }

    [Fact]
    public void With_no_buff_table_no_card_gets_a_badge()
    {
        Assert.Empty(PlanBadges.Map(new[] { new TalentRow(30651, Card, 306510) }, new Dictionary<int, float>()));
    }
}
