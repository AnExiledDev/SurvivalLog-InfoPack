using InfoPack.Core;
using Xunit;

namespace InfoPack.Tests;

public class LootedOutlineTests
{
    [Fact]
    public void A_container_searched_down_to_nothing_is_shown_with_no_search_colour()
    {
        // The game sends 3 for searched-and-empty (ExploreSearchStateManager.ExploreSearchState), and
        // AgentModel.Furniture_ShowSearchState gives 0 no outline bit, the same as Furniture_HideSearchState.
        Assert.Equal(0, LootedOutline.ShownState(3));
    }

    [Fact]
    public void Containers_the_player_still_needs_keep_their_search_colour()
    {
        Assert.Equal(1, LootedOutline.ShownState(1));
        Assert.Equal(2, LootedOutline.ShownState(2));
    }

    [Fact]
    public void A_container_with_no_search_state_stays_without_one()
    {
        Assert.Equal(0, LootedOutline.ShownState(0));
    }

    [Fact]
    public void A_state_the_game_adds_later_is_shown_as_the_game_sent_it()
    {
        Assert.Equal(4, LootedOutline.ShownState(4));
        Assert.Equal(-1, LootedOutline.ShownState(-1));
    }
}
