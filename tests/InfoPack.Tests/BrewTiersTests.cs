using System.Collections.Generic;
using InfoPack.Core;
using Xunit;

namespace InfoPack.Tests;

public class BrewTiersTests
{
    private const int Poor = 15505;
    private const int Normal = 15504;
    private const int Strong = 15506;

    [Fact]
    public void A_barrels_products_are_ranked_lowest_first()
    {
        var tiers = BrewTiers.Map(new[] { new[] { Poor, Normal, Strong } });

        Assert.Equal(new BrewTier(1, 3), tiers[Poor]);
        Assert.Equal(new BrewTier(2, 3), tiers[Normal]);
        Assert.Equal(new BrewTier(3, 3), tiers[Strong]);
    }

    [Fact]
    public void A_single_product_is_no_ladder_and_gets_no_tier()
    {
        Assert.Empty(BrewTiers.Map(new[] { new[] { Poor } }));
    }

    [Fact]
    public void Two_barrels_that_agree_on_a_product_keep_its_tier()
    {
        var tiers = BrewTiers.Map(new[] { new[] { Poor, Normal, Strong }, new[] { Poor, Normal, Strong } });

        Assert.Equal(new BrewTier(2, 3), tiers[Normal]);
    }

    [Fact]
    public void A_product_two_barrels_rank_differently_is_left_out_rather_than_shown_wrong()
    {
        var tiers = BrewTiers.Map(new IReadOnlyList<int>[] { new[] { Poor, Normal, Strong }, new[] { Normal, Strong } });

        Assert.False(tiers.ContainsKey(Normal));
        Assert.False(tiers.ContainsKey(Strong));
        Assert.Equal(new BrewTier(1, 3), tiers[Poor]);
    }

    [Fact]
    public void Only_the_three_tiers_the_game_can_brew_are_ranked()
    {
        const int Unreachable = 15507;

        var tiers = BrewTiers.Map(new[] { new[] { Poor, Normal, Strong, Unreachable } });

        Assert.Equal(new BrewTier(1, 3), tiers[Poor]);
        Assert.Equal(new BrewTier(2, 3), tiers[Normal]);
        Assert.Equal(new BrewTier(3, 3), tiers[Strong]);
        Assert.False(tiers.ContainsKey(Unreachable));
    }

    [Fact]
    public void An_empty_slot_in_the_list_keeps_the_others_in_place()
    {
        var tiers = BrewTiers.Map(new[] { new[] { Poor, 0, Strong } });

        Assert.Equal(new BrewTier(3, 3), tiers[Strong]);
        Assert.False(tiers.ContainsKey(0));
    }
}
