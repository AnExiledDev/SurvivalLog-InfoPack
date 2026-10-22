using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using InfoPack.Core;
using Xunit;

namespace InfoPack.Tests;

public class WhereKeptTests
{
    private const long Bag = 1;
    private const long Fridge = 2;
    private const long Shelf = 3;
    private const long OtherShelf = 4;
    private const long Drawer = 5;
    private const long Trader = 99;
    private const int Rice = 101;
    private const int Salt = 202;

    private static readonly Dictionary<long, string> Names = new()
    {
        [Bag] = "Inventory",
        [Fridge] = "Fridge",
        [Shelf] = "Shelf",
        [OtherShelf] = "Shelf",
        [Drawer] = "Workbench Drawer",
        [Trader] = "Trader",
    };

    private static IReadOnlyList<KeptIn> Rows(IHomeItems home, int configId) => WhereKept.Rows(home, storageId => Names[storageId], configId);

    [Fact]
    public void Only_the_storages_holding_the_item_are_listed_biggest_first()
    {
        var home = new FakeHome(Bag, Fridge, Shelf, Drawer)
            .Holds(Bag, Rice, 3)
            .Holds(Fridge, Rice, 6)
            .Holds(Shelf, Salt, 9)
            .Holds(Drawer, Rice, 1);

        Assert.Equal(new[] { new KeptIn("Fridge", 6), new KeptIn("Inventory", 3), new KeptIn("Workbench Drawer", 1) }, Rows(home, Rice));
    }

    [Fact]
    public void The_rows_add_up_to_the_home_count()
    {
        var home = new FakeHome(Bag, Fridge, Fridge, Drawer)
            .Holds(Bag, Rice, 3)
            .Holds(Fridge, Rice, 6)
            .Holds(Fridge, Rice, 2)
            .HoldsStale(Fridge, ownerNow: Trader, Rice, 40)
            .Holds(Trader, Rice, 12)
            .Holds(Drawer, Rice, 0);

        Assert.Equal(HomeCounts.Count(home)[Rice], Rows(home, Rice).Sum(row => row.Count));
        Assert.Equal(new[] { new KeptIn("Fridge", 8), new KeptIn("Inventory", 3) }, Rows(home, Rice));
    }

    [Fact]
    public void Storages_holding_the_same_number_are_listed_by_name()
    {
        var home = new FakeHome(Fridge, Drawer, Bag).Holds(Fridge, Rice, 2).Holds(Drawer, Rice, 2).Holds(Bag, Rice, 2);

        Assert.Equal(new[] { "Fridge", "Inventory", "Workbench Drawer" }, Rows(home, Rice).Select(row => row.Storage));
    }

    [Fact]
    public void Storages_that_share_a_name_are_numbered_so_the_player_can_tell_them_apart()
    {
        var home = new FakeHome(Shelf, OtherShelf).Holds(Shelf, Rice, 1).Holds(OtherShelf, Rice, 5);

        Assert.Equal(new[] { new KeptIn("Shelf 2", 5), new KeptIn("Shelf", 1) }, Rows(home, Rice));
    }

    [Fact]
    public void A_storage_keeps_its_number_whichever_item_is_hovered()
    {
        var home = new FakeHome(Shelf, OtherShelf).Holds(Shelf, Salt, 1).Holds(OtherShelf, Rice, 5);

        Assert.Equal(new[] { new KeptIn("Shelf 2", 5) }, Rows(home, Rice));
    }

    [Fact]
    public void An_item_nobody_at_home_holds_gives_an_empty_list()
    {
        var home = new FakeHome(Bag, Fridge).Holds(Fridge, Salt, 4);

        Assert.Empty(Rows(home, Rice));
        Assert.Equal("[]", WhereKept.ToJson(Rows(home, Rice)));
    }

    [Fact]
    public void The_page_gets_each_row_as_a_name_and_a_number()
    {
        var rows = JsonDocument.Parse(WhereKept.ToJson(new[] { new KeptIn("Fridge", 6), new KeptIn("Inventory", 3) })).RootElement;

        Assert.Equal(2, rows.GetArrayLength());
        Assert.Equal("Fridge", rows[0][0].GetString());
        Assert.Equal(6, rows[0][1].GetInt32());
        Assert.Equal("Inventory", rows[1][0].GetString());
        Assert.Equal(3, rows[1][1].GetInt32());
    }

    [Theory]
    [InlineData("Mum's \"good\" box")]
    [InlineData("</script><b>")]
    [InlineData("Box \\ 2\nend")]
    public void A_storage_name_reaches_the_page_unchanged_and_cannot_break_out_of_its_string(string name)
    {
        var json = WhereKept.ToJson(new[] { new KeptIn(name, 1) });

        Assert.Equal(name, JsonDocument.Parse(json).RootElement[0][0].GetString());
        Assert.DoesNotContain("<", json);
    }
}
