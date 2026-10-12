using InfoPack.Core;
using Xunit;

namespace InfoPack.Tests;

public class LootedOutlineTests
{
    [Fact]
    public void A_container_searched_down_to_nothing_loses_its_search_colour()
    {
        // The game sends 3 for searched-and-empty (ExploreSearchStateManager.ExploreSearchState).
        Assert.True(LootedOutline.ShouldClear(3));
    }

    [Fact]
    public void Containers_the_player_still_needs_keep_their_search_colour()
    {
        Assert.False(LootedOutline.ShouldClear(1));
        Assert.False(LootedOutline.ShouldClear(2));
    }

    [Fact]
    public void No_state_or_a_state_the_game_adds_later_is_left_to_the_game()
    {
        Assert.False(LootedOutline.ShouldClear(0));
        Assert.False(LootedOutline.ShouldClear(4));
        Assert.False(LootedOutline.ShouldClear(-1));
    }
}
