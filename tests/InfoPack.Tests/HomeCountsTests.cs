using System.Collections.Generic;
using InfoPack.Core;
using Xunit;

namespace InfoPack.Tests;

public class HomeCountsTests
{
    private const long Bag = 1;
    private const long Fridge = 2;
    private const long Drawer = 3;
    private const long Trader = 99;

    private static Dictionary<int, int> Count(IHomeItems home)
    {
        var tally = new HomeTally();

        tally.Count(home);

        return tally.Counts;
    }

    [Fact]
    public void An_item_spread_over_the_bag_and_home_storages_is_counted_as_one_total()
    {
        var home = new FakeHome(Bag, Fridge, Drawer)
            .Holds(Bag, 101, 2)
            .Holds(Fridge, 101, 5)
            .Holds(Drawer, 101, 1)
            .Holds(Fridge, 202, 3);

        Assert.Equal(new Dictionary<int, int> { [101] = 8, [202] = 3 }, Count(home));
    }

    [Fact]
    public void Storages_away_from_home_are_not_counted()
    {
        var home = new FakeHome(Bag).Holds(Bag, 101, 1).Holds(Trader, 101, 40);

        Assert.Equal(1, Count(home)[101]);
    }

    [Fact]
    public void A_storage_named_twice_is_counted_once()
    {
        var home = new FakeHome(Bag, Fridge, Fridge).Holds(Fridge, 101, 4);

        Assert.Equal(4, Count(home)[101]);
    }

    [Fact]
    public void A_stale_cache_entry_that_now_belongs_elsewhere_is_not_counted()
    {
        var home = new FakeHome(Bag, Fridge).Holds(Fridge, 101, 4).HoldsStale(Fridge, ownerNow: Trader, 101, 9);

        Assert.Equal(4, Count(home)[101]);
    }

    [Fact]
    public void Empty_stacks_and_stacks_with_no_item_add_nothing()
    {
        var home = new FakeHome(Bag).Holds(Bag, 101, 0).Holds(Bag, 0, 5).Holds(Bag, 202, -1);

        Assert.Empty(Count(home));
    }

    [Fact]
    public void A_home_that_did_not_change_keeps_its_version_so_no_message_is_built()
    {
        var home = new FakeHome(Bag, Fridge).Holds(Bag, 101, 2).Holds(Fridge, 202, 3);
        var tally = new HomeTally();

        Assert.True(tally.Count(home));

        var version = tally.Version;

        Assert.False(tally.Count(home));
        Assert.False(tally.Count(home));
        Assert.Equal(version, tally.Version);
        Assert.Equal(new Dictionary<int, int> { [101] = 2, [202] = 3 }, tally.Counts);
    }

    [Fact]
    public void A_stack_added_or_taken_away_is_seen_at_the_next_count()
    {
        var home = new FakeHome(Bag, Fridge).Holds(Bag, 101, 2);
        var tally = new HomeTally();

        tally.Count(home);

        var first = tally.Version;

        home.Holds(Fridge, 101, 1);

        Assert.True(tally.Count(home));
        Assert.NotEqual(first, tally.Version);
        Assert.Equal(3, tally.Counts[101]);

        var second = tally.Version;

        home.Empties(Bag, 101).Empties(Fridge, 101);

        Assert.True(tally.Count(home));
        Assert.NotEqual(second, tally.Version);
        Assert.Empty(tally.Counts);
    }

    [Fact]
    public void The_same_total_made_of_different_items_is_still_a_change()
    {
        var home = new FakeHome(Bag).Holds(Bag, 101, 2);
        var tally = new HomeTally();

        tally.Count(home);
        home.Empties(Bag, 101).Holds(Bag, 202, 2);

        Assert.True(tally.Count(home));
        Assert.Equal(new Dictionary<int, int> { [202] = 2 }, tally.Counts);
    }

    [Fact]
    public void Every_storage_is_read_once_per_count()
    {
        var home = new FakeHome(Bag, Fridge, Fridge, Drawer);

        new HomeTally().Count(home);

        Assert.Equal(3, home.Reads);
    }
}
