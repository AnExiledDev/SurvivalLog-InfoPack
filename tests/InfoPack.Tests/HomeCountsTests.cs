using System.Collections.Generic;
using System.Linq;
using InfoPack.Core;
using Xunit;

namespace InfoPack.Tests;

public class HomeCountsTests
{
    private const long Bag = 1;
    private const long Fridge = 2;
    private const long Drawer = 3;
    private const long Trader = 99;

    [Fact]
    public void An_item_spread_over_the_bag_and_home_storages_is_counted_as_one_total()
    {
        var home = new FakeHome(Bag, Fridge, Drawer)
            .Holds(Bag, 101, 2)
            .Holds(Fridge, 101, 5)
            .Holds(Drawer, 101, 1)
            .Holds(Fridge, 202, 3);

        var counts = HomeCounts.Count(home);

        Assert.Equal(new SortedDictionary<int, int> { [101] = 8, [202] = 3 }, counts);
    }

    [Fact]
    public void Storages_away_from_home_are_not_counted()
    {
        var home = new FakeHome(Bag).Holds(Bag, 101, 1).Holds(Trader, 101, 40);

        Assert.Equal(1, HomeCounts.Count(home)[101]);
    }

    [Fact]
    public void A_storage_named_twice_is_counted_once()
    {
        var home = new FakeHome(Bag, Fridge, Fridge).Holds(Fridge, 101, 4);

        Assert.Equal(4, HomeCounts.Count(home)[101]);
    }

    [Fact]
    public void A_stale_cache_entry_that_now_belongs_elsewhere_is_not_counted()
    {
        var home = new FakeHome(Bag, Fridge).Holds(Fridge, 101, 4).HoldsStale(Fridge, ownerNow: Trader, 101, 9);

        Assert.Equal(4, HomeCounts.Count(home)[101]);
    }

    [Fact]
    public void Empty_stacks_and_stacks_with_no_item_add_nothing()
    {
        var home = new FakeHome(Bag).Holds(Bag, 101, 0).Holds(Bag, 0, 5).Holds(Bag, 202, -1);

        Assert.Empty(HomeCounts.Count(home));
    }

    [Fact]
    public void Counts_come_out_in_id_order_so_the_same_home_always_gives_the_same_message()
    {
        var home = new FakeHome(Bag).Holds(Bag, 303, 1).Holds(Bag, 101, 1).Holds(Bag, 202, 1);

        Assert.Equal(new[] { 101, 202, 303 }, HomeCounts.Count(home).Keys.ToArray());
    }
}
