using InfoPack.Core;
using Xunit;

namespace InfoPack.Tests;

public class LootedOutlineTests
{
    [Fact]
    public void A_container_searched_down_to_nothing_loses_its_search_colour()
    {
        Assert.True(LootedOutline.ShouldClear((int)SearchState.Empty));
    }

    [Fact]
    public void Containers_the_player_still_needs_keep_their_search_colour()
    {
        Assert.False(LootedOutline.ShouldClear((int)SearchState.Unsearched));
        Assert.False(LootedOutline.ShouldClear((int)SearchState.HasItems));
    }

    [Fact]
    public void No_state_or_a_state_the_game_adds_later_is_left_to_the_game()
    {
        Assert.False(LootedOutline.ShouldClear((int)SearchState.None));
        Assert.False(LootedOutline.ShouldClear(4));
        Assert.False(LootedOutline.ShouldClear(-1));
    }

    [Fact]
    public void The_states_are_numbered_as_the_game_sends_them()
    {
        // The game's outline handler maps 1, 2 and 3 to its unsearched, has-items and empty colours.
        Assert.Equal(1, (int)SearchState.Unsearched);
        Assert.Equal(2, (int)SearchState.HasItems);
        Assert.Equal(3, (int)SearchState.Empty);
    }
}
