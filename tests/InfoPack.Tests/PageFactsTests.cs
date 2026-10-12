using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using InfoPack.Core;
using Xunit;
using static InfoPack.Tests.Rows;

namespace InfoPack.Tests;

public class PageFactsTests
{
    private const int Battery = 101;
    private const int Wire = 501;
    private const int Rice = 601;
    private const int Salt = 602;
    private const int Stew = 701;
    private const int GoodStew = 702;
    private const int Ale = 15505;

    private static readonly Features AllOn = new(true, true, true, true, true, true);
    private static readonly Features AllOff = new(false, false, false, false, false, false);

    private static GameTables World() => Tables(
        new[] { Junk(Battery, tradeValue: 0), Plain(Wire), Edible(Rice, 5f, 1f), Plain(Salt), Edible(Stew, 20f, 3f, 0f, 0f, -1f) },
        cooking: new[] { Recipe(900, normal: Stew, good: GoodStew, ingredients: new[] { Salt }) },
        brewProducts: new IReadOnlyList<int>[] { new[] { Ale, 15504, 15506 } });

    private static FakeText Text() => new FakeText()
        .Describes(Battery, "A dead battery.")
        .Describes(Wire, "Some wire.")
        .Describes(Rice, "  Plain rice.  ")
        .Describes(Salt, "Salt, \"fine\".")
        .Describes(Stew, "A warm stew.")
        .Describes(GoodStew, "   ");

    private static JsonElement Build(Features features) => JsonDocument.Parse(PageFacts.Build(World(), Text(), features)).RootElement;

    private static string[] Keys(JsonElement element) => element.EnumerateObject().Select(member => member.Name).ToArray();

    [Fact]
    public void With_every_piece_on_the_page_gets_every_kind_of_fact()
    {
        Assert.Equal(new[] { "stats", "desc", "dish", "brew", "scrap" }, Keys(Build(AllOn)));
    }

    [Fact]
    public void A_piece_turned_off_sends_nothing_for_it()
    {
        Assert.Empty(Keys(Build(AllOff)));
        Assert.Equal(new[] { "scrap" }, Keys(Build(AllOff with { ScrapOnly = true })));
        Assert.Equal(new[] { "brew" }, Keys(Build(AllOff with { AlcoholQuality = true })));
        Assert.Equal(new[] { "stats", "desc", "dish" }, Keys(Build(AllOff with { CookingCodex = true })));
    }

    [Fact]
    public void Trade_stats_alone_still_gets_the_stat_lines()
    {
        Assert.Equal(new[] { "stats" }, Keys(Build(AllOff with { TradeStats = true })));
    }

    [Fact]
    public void Base_counts_need_no_facts_because_counts_travel_on_their_own()
    {
        Assert.Empty(Keys(Build(AllOff with { BaseCounts = true })));
    }

    [Fact]
    public void Each_item_that_changes_a_stat_gets_its_line_in_the_games_wording()
    {
        var stats = Build(AllOn).GetProperty("stats");

        Assert.Equal(new[] { "601", "701" }, Keys(stats));
        Assert.Equal("Satiety +20, Mood +3, Life -1", stats.GetProperty("701").GetString());
    }

    [Fact]
    public void Descriptions_cover_food_ingredients_and_dishes_and_leave_the_rest_of_the_table_out()
    {
        var descriptions = Build(AllOn).GetProperty("desc");

        Assert.Equal(new[] { "601", "602", "701" }, Keys(descriptions));
        Assert.Equal("Plain rice.", descriptions.GetProperty("601").GetString());
        Assert.Equal("Salt, \"fine\".", descriptions.GetProperty("602").GetString());
    }

    [Fact]
    public void A_recipe_stands_for_its_normal_dish()
    {
        Assert.Equal(Stew, Build(AllOn).GetProperty("dish").GetProperty("900").GetInt32());
    }

    [Fact]
    public void A_recipe_with_no_normal_dish_falls_back_to_the_next_result_it_names()
    {
        var dishes = PageFacts.Dishes(new[]
        {
            Recipe(1, normal: 0, good: GoodStew, perfect: 703),
            Recipe(2, normal: 0, good: 0, perfect: 703, fail: 704),
            Recipe(3, normal: 0, fail: 704),
        });

        Assert.Equal(new SortedDictionary<int, int> { [1] = GoodStew, [2] = 703, [3] = 704 }, dishes);
    }

    [Fact]
    public void A_recipe_that_names_no_dish_or_has_no_id_is_left_out()
    {
        Assert.Empty(PageFacts.Dishes(new[] { Recipe(1, normal: 0), Recipe(0, normal: Stew) }));
    }

    [Fact]
    public void Brew_products_carry_their_rank_and_ladder_size()
    {
        var brew = Build(AllOn).GetProperty("brew");

        Assert.Equal(new[] { "15504", "15505", "15506" }, Keys(brew));
        Assert.Equal(new[] { 1, 3 }, brew.GetProperty("15505").EnumerateArray().Select(value => value.GetInt32()).ToArray());
    }

    [Fact]
    public void The_scrap_list_names_the_marked_items()
    {
        var scrap = Build(AllOn).GetProperty("scrap").EnumerateArray().Select(value => value.GetInt32()).ToArray();

        Assert.Equal(new[] { Battery }, scrap);
    }

    [Fact]
    public void The_same_tables_always_give_the_same_text()
    {
        Assert.Equal(PageFacts.Build(World(), Text(), AllOn), PageFacts.Build(World(), Text(), AllOn));
    }

    [Fact]
    public void Counts_go_out_in_id_order_whatever_order_they_came_in()
    {
        Assert.Equal("{\"7\":1,\"40\":2,\"301\":5}", PageFacts.Counts(new Dictionary<int, int> { [301] = 5, [7] = 1, [40] = 2 }));
        Assert.Equal("{}", PageFacts.Counts(new Dictionary<int, int>()));
    }
}
