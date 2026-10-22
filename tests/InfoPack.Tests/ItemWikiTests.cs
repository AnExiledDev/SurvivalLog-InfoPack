using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using InfoPack.Core;
using Xunit;
using static InfoPack.Tests.Rows;

namespace InfoPack.Tests;

public class ItemWikiTests
{
    private const int Flour = 10;
    private const int Water = 11;
    private const int Seed = 50;
    private const int Wheat = 51;
    private const int Bread = 300;
    private const int Pie = 400;
    private const int BurntPie = 401;
    private const int Mould = 900;
    private const int Gear = 20001;
    private const int Screw = 20003;
    private const int Radio = 20501;

    /// <summary>An item with nothing set, so each test shows exactly the fields it gives.</summary>
    private static ItemRow Item(int id) => Plain(id, tradeValue: 0);

    private static JsonElement Wiki(GameTables tables, FakeText? text = null) =>
        JsonDocument.Parse(WikiJson.Wiki(ItemWiki.Build(tables, text ?? new FakeText()))).RootElement;

    private static JsonElement Entry(JsonElement wiki, int id) => wiki.GetProperty(id.ToString(CultureInfo.InvariantCulture));

    private static string Field(JsonElement wiki, int id, string field) => Entry(wiki, id).GetProperty(field).GetRawText();

    private static string[] Fields(JsonElement wiki, int id) => Entry(wiki, id).EnumerateObject().Select(member => member.Name).ToArray();

    private static bool HasEntry(JsonElement wiki, int id) => wiki.TryGetProperty(id.ToString(CultureInfo.InvariantCulture), out _);

    [Fact]
    public void Effects_are_rounded_like_the_stat_line_and_left_out_when_the_item_changes_nothing()
    {
        var wiki = Wiki(Tables(new[] { Edible(Pie, 2.04f, 0.36f, -0.04f, 0f, -1f), Item(Flour) with { Weight = 500 } }));

        Assert.Equal("[2,0.4,0,0,-1]", Field(wiki, Pie, "fx"));
        Assert.Equal(new[] { "wt" }, Fields(wiki, Flour));
    }

    [Fact]
    public void An_item_that_spoils_gets_its_shelf_life_and_what_it_rots_into_and_one_that_never_spoils_gets_neither()
    {
        var wiki = Wiki(Tables(new[]
        {
            Item(Bread) with { ShelfLifeDays = 3, RotProduct = Mould },
            Item(Flour) with { ShelfLifeDays = -1, RotProduct = Mould, Weight = 500 },
        }));

        Assert.Equal(new[] { "life", "rot" }, Fields(wiki, Bread));
        Assert.Equal("3", Field(wiki, Bread, "life"));
        Assert.Equal("900", Field(wiki, Bread, "rot"));
        Assert.Equal(new[] { "wt" }, Fields(wiki, Flour));
    }

    [Fact]
    public void Weight_and_trade_value_are_sent_only_when_set()
    {
        var wiki = Wiki(Tables(new[] { Item(Flour) with { Weight = 500, TradeValue = 2 }, Item(Water) with { TradeValue = 1 } }));

        Assert.Equal("500", Field(wiki, Flour, "wt"));
        Assert.Equal("2", Field(wiki, Flour, "tv"));
        Assert.Equal(new[] { "tv" }, Fields(wiki, Water));
    }

    [Fact]
    public void An_item_with_nothing_to_say_gets_no_entry()
    {
        Assert.Equal("{}", WikiJson.Wiki(ItemWiki.Build(Tables(new[] { Item(Flour) }), new FakeText())));
    }

    [Fact]
    public void Uses_list_workbench_recipes_then_cooking_then_brewing()
    {
        var wiki = Wiki(Tables(
            new[] { Item(Flour) with { CanBrew = true }, Item(Water), Item(Bread), Item(Pie) },
            production: new[] { Bench(Bread, Flour, Water, Flour) },
            cooking: new[] { Recipe(1, normal: 0, good: Pie, ingredients: new[] { Flour, Flour }) }));

        Assert.Equal("[{\"k\":\"bench\",\"out\":300,\"need\":[[10,2],[11,1]]},{\"k\":\"cook\",\"out\":400},{\"k\":\"brew\",\"s\":0,\"m\":0}]", Field(wiki, Flour, "use"));
        Assert.Equal("[{\"k\":\"bench\",\"out\":300,\"need\":[[10,2],[11,1]]}]", Field(wiki, Water, "use"));
    }

    [Fact]
    public void Shredding_and_recipes_the_workbench_does_not_list_are_not_uses()
    {
        var wiki = Wiki(Tables(
            new[] { Item(Radio) with { Weight = 900 }, Item(Gear) },
            production: new[]
            {
                new ProductionRecipe(new[] { Radio }, IsShredOnly: true, Gear),
                new ProductionRecipe(new[] { Radio }, IsShredOnly: false, Gear, IsListed: false),
            }));

        Assert.Equal(new[] { "wt" }, Fields(wiki, Radio));
        Assert.False(HasEntry(wiki, Gear));
    }

    [Fact]
    public void Two_recipe_rows_that_read_the_same_show_once()
    {
        var wiki = Wiki(Tables(
            new[] { Item(Flour), Item(Bread), Item(Pie) },
            production: new[] { Bench(Bread, Flour), Bench(Bread, Flour) },
            cooking: new[] { Recipe(1, normal: Pie, ingredients: new[] { Flour }), Recipe(2, normal: Pie, ingredients: new[] { Flour }) }));

        Assert.Equal("[{\"k\":\"bench\",\"out\":300,\"need\":[[10,1]]},{\"k\":\"cook\",\"out\":400}]", Field(wiki, Flour, "use"));
        Assert.Equal("[{\"k\":\"bench\",\"need\":[[10,1]]}]", Field(wiki, Bread, "make"));
        Assert.Equal("[{\"k\":\"cook\",\"need\":[[10,1]]}]", Field(wiki, Pie, "make"));
    }

    [Fact]
    public void Dishes_an_ingredient_goes_into_are_listed_by_the_total_of_their_stats_best_first()
    {
        var wiki = Wiki(Tables(
            new[] { Item(Flour), Edible(Bread, 10f, 0f, 0f, 0f, 2f), Edible(Pie, 20f, 5f, 0f, 0f, 0f), Edible(BurntPie, 3f, -6f, 0f, 0f, 0f), Item(Mould) },
            cooking: new[]
            {
                Recipe(1, normal: BurntPie, ingredients: new[] { Flour }),
                Recipe(2, normal: Mould, ingredients: new[] { Flour }),
                Recipe(3, normal: Bread, ingredients: new[] { Flour }),
                Recipe(4, normal: Pie, ingredients: new[] { Flour }),
            }));

        Assert.Equal(
            "[{\"k\":\"cook\",\"out\":400},{\"k\":\"cook\",\"out\":300},{\"k\":\"cook\",\"out\":900},{\"k\":\"cook\",\"out\":401}]",
            Field(wiki, Flour, "use"));
    }

    [Fact]
    public void Made_from_lists_workbench_cooking_harvest_and_shredding_in_that_order()
    {
        var wiki = Wiki(Tables(
            new[] { Item(Flour), Item(Water), Item(Seed) with { PlantId = 7 }, Item(Wheat), Item(Radio) },
            production: new[] { Bench(Wheat, Flour) },
            cooking: new[] { Recipe(2, normal: Wheat, ingredients: new[] { Water }) with { CookTime = 600 } },
            shredYields: new Dictionary<int, IReadOnlyList<Yield>> { [Radio] = new[] { new Yield(Wheat, 2) } },
            plants: new[] { new PlantRow(7, 64800, 259200, 1, 0, new[] { Wheat, Seed }, 0.5f) }));

        Assert.Equal(
            "[{\"k\":\"bench\",\"need\":[[10,1]]},{\"k\":\"cook\",\"need\":[[11,1]],\"t\":600},{\"k\":\"plant\",\"from\":50},{\"k\":\"shred\",\"from\":20501,\"n\":2}]",
            Field(wiki, Wheat, "make"));
    }

    [Fact]
    public void A_plant_that_gives_its_own_seed_back_is_not_listed_as_made_from_itself()
    {
        var wiki = Wiki(Tables(
            new[] { Item(Seed) with { PlantId = 7 }, Item(Wheat) },
            plants: new[] { new PlantRow(7, 64800, 259200, 1, 0, new[] { Wheat, Seed }, 0.5f) }));

        Assert.Equal(new[] { "grow" }, Fields(wiki, Seed));
    }

    [Fact]
    public void Every_dish_a_recipe_can_give_is_made_by_it_and_a_food_tag_recipe_names_no_ingredients()
    {
        var wiki = Wiki(Tables(new[] { Item(Pie), Item(BurntPie) }, cooking: new[] { Recipe(3, normal: Pie, fail: BurntPie) }));

        Assert.Equal("[{\"k\":\"cook\"}]", Field(wiki, Pie, "make"));
        Assert.Equal("[{\"k\":\"cook\"}]", Field(wiki, BurntPie, "make"));
    }

    [Fact]
    public void Breaking_down_cutting_and_burning_come_from_the_items_own_row()
    {
        var wiki = Wiki(Tables(
            new[] { Item(Radio) with { CutProduct = Gear, IsBurnable = true, BurnValue = 30 }, Item(Gear) with { BurnValue = 30 }, Item(Screw) },
            shredYields: new Dictionary<int, IReadOnlyList<Yield>> { [Radio] = new[] { new Yield(Screw, 1), new Yield(Gear, 2) } }));

        Assert.Equal("[[20003,1],[20001,2]]", Field(wiki, Radio, "shred"));
        Assert.Equal("20001", Field(wiki, Radio, "cut"));
        Assert.Equal("30", Field(wiki, Radio, "burn"));
        Assert.Equal(new[] { "make" }, Fields(wiki, Gear));
    }

    [Fact]
    public void A_seed_carries_its_plant_row()
    {
        var wiki = Wiki(Tables(
            new[] { Item(Seed) with { PlantId = 7 }, Item(Wheat) },
            plants: new[] { new PlantRow(7, 64800, 259200, 1, 0, new[] { Wheat }, 0.3f) }));

        Assert.Equal("{\"t\":64800,\"h\":259200,\"light\":1,\"cold\":0,\"gain\":[51],\"seed\":0.3}", Field(wiki, Seed, "grow"));
        Assert.Equal("[{\"k\":\"plant\",\"from\":50}]", Field(wiki, Wheat, "make"));
    }

    [Fact]
    public void A_seed_whose_plant_row_is_missing_grows_nothing_and_makes_nothing()
    {
        var wiki = Wiki(Tables(
            new[] { Item(Seed) with { PlantId = 8, Weight = 10 }, Item(Wheat) },
            plants: new[] { new PlantRow(7, 64800, 259200, 1, 0, new[] { Wheat }, 0.3f) }));

        Assert.Equal(new[] { "wt" }, Fields(wiki, Seed));
        Assert.False(HasEntry(wiki, Wheat));
    }

    [Fact]
    public void Furniture_sharing_a_name_is_one_source_placed_where_its_rows_that_drop_the_item_stand()
    {
        var text = new FakeText()
            .NamesFurniture(100, "Shelf")
            .NamesFurniture(101, "Shelf")
            .NamesFurniture(103, "Crate")
            .NamesPlace(2, "Market")
            .NamesPlace(3, "School")
            .NamesPlace(5, "Market");

        var wiki = Wiki(
            Tables(
                new[] { Item(Flour), Item(Water) },
                lootFurniture: new[] { new LootFurniture(100, new[] { 1, 2 }), new LootFurniture(101, new[] { 1 }), new LootFurniture(102, new[] { 1 }), new LootFurniture(103, new[] { 3 }) },
                lootGroups: new Dictionary<int, IReadOnlyList<int>> { [1] = new[] { Flour }, [2] = new[] { Flour, Water }, [3] = new[] { Flour } },
                placements: new[] { new Placement(100, 5), new Placement(101, 2), new Placement(101, 5), new Placement(100, 3), new Placement(100, 9) }),
            text);

        Assert.Equal("[{\"src\":\"Shelf\",\"at\":[\"Market\",\"School\"]},{\"src\":\"Crate\"}]", Field(wiki, Flour, "found"));
        Assert.Equal("[{\"src\":\"Shelf\",\"at\":[\"School\",\"Market\"]}]", Field(wiki, Water, "found"));
    }

    [Fact]
    public void Found_in_keeps_the_eight_sources_standing_in_the_most_places()
    {
        var text = new FakeText().NamesPlace(1, "North").NamesPlace(2, "South").NamesPlace(3, "East");
        var furniture = Enumerable.Range(0, 10).Select(i => new LootFurniture(200 + i, new[] { 1 })).ToList();
        var placements = new List<Placement> { new(209, 1), new(209, 2), new(209, 3), new(205, 1), new(205, 2) };

        foreach (var i in Enumerable.Range(0, 10)) text.NamesFurniture(200 + i, "F" + i);

        placements.AddRange(new[] { 202, 203, 204, 206, 207, 208 }.Select(id => new Placement(id, 1)));

        var wiki = Wiki(
            Tables(new[] { Item(Flour) }, lootFurniture: furniture, lootGroups: new Dictionary<int, IReadOnlyList<int>> { [1] = new[] { Flour } }, placements: placements),
            text);

        var sources = Entry(wiki, Flour).GetProperty("found").EnumerateArray().Select(found => found.GetProperty("src").GetString()).ToArray();

        Assert.Equal(new[] { "F9", "F5", "F2", "F3", "F4", "F6", "F7", "F8" }, sources);
    }

    [Fact]
    public void Items_name_every_entry_and_every_item_an_entry_points_at_and_nothing_else()
    {
        var tables = Tables(
            new[] { Item(Flour) with { Icon = "../../Res/Food/flour.png" }, Item(Bread), Item(Water) },
            production: new[] { Bench(Bread, Flour, Mould) });

        var text = new FakeText().Names(Flour, "Flour");
        var items = JsonDocument.Parse(WikiJson.Items(tables, text, ItemWiki.Build(tables, text))).RootElement;

        Assert.Equal(new[] { "10", "300" }, items.EnumerateObject().Select(member => member.Name).ToArray());
        Assert.Equal("[\"Flour\",\"../../Res/Food/flour.png\"]", items.GetProperty("10").GetRawText());
        Assert.Equal("[\"\",\"\"]", items.GetProperty("300").GetRawText());
    }

    [Fact]
    public void The_same_tables_in_any_order_give_the_same_text()
    {
        var text = new FakeText().NamesFurniture(100, "Shelf").NamesFurniture(101, "Crate").NamesPlace(1, "Market").Names(Flour, "Flour");

        Assert.Equal(Facts(Shuffled(false), text), Facts(Shuffled(true), text));
    }

    private static string Facts(GameTables tables, FakeText text)
    {
        var wiki = ItemWiki.Build(tables, text);

        return WikiJson.Items(tables, text, wiki) + WikiJson.Wiki(wiki);
    }

    private static GameTables Shuffled(bool isReversed)
    {
        IEnumerable<T> Order<T>(IEnumerable<T> rows) => isReversed ? rows.Reverse() : rows;

        return Tables(
            Order(new[] { Item(Flour) with { CanBrew = true }, Item(Water), Item(Bread), Item(Pie), Item(BurntPie) }),
            production: Order(new[] { Bench(Bread, Water, Flour), Bench(Pie, Flour), Bench(Bread, Flour, Flour) }),
            cooking: Order(new[] { Recipe(1, normal: Pie, fail: BurntPie, ingredients: new[] { Flour }), Recipe(2, normal: Bread, fail: BurntPie, ingredients: new[] { Water }) }),
            lootFurniture: Order(new[] { new LootFurniture(101, new[] { 1 }), new LootFurniture(100, new[] { 1 }) }),
            lootGroups: new Dictionary<int, IReadOnlyList<int>> { [1] = new[] { Flour, Water } },
            placements: Order(new[] { new Placement(100, 1), new Placement(101, 1) }));
    }
}
