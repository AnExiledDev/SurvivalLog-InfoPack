using InfoPack.Core;
using Xunit;

namespace InfoPack.Tests;

public class LootedOutlineTests
{
    [Fact]
    public void A_container_searched_down_to_nothing_loses_its_search_colour()
    {
        // The game reports 3 for searched-and-empty (ExploreSearchStateManager.ExploreSearchState).
        Assert.False(LootedOutline.ShowsSearchColour(3));
    }

    [Fact]
    public void Containers_the_player_still_needs_keep_their_search_colour()
    {
        Assert.True(LootedOutline.ShowsSearchColour(1));
        Assert.True(LootedOutline.ShowsSearchColour(2));
    }

    [Fact]
    public void A_container_with_no_search_state_is_left_to_the_game()
    {
        Assert.True(LootedOutline.ShowsSearchColour(0));
    }

    [Fact]
    public void A_state_the_game_adds_later_is_left_to_the_game()
    {
        Assert.True(LootedOutline.ShowsSearchColour(4));
        Assert.True(LootedOutline.ShowsSearchColour(-1));
    }
}
