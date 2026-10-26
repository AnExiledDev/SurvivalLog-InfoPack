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
    private const int Energetic = 30651;
    private const int Airdrop = 31301;
    private const int Lounging = 31302;
    private const int Awakening = 10011;

    private static readonly Features AllOn = new(true, true, true, true, true, true);
    private static readonly Features AllOff = new(false, false, false, false, false, false);

    private static GameTables World() => Tables(
        new[] { Junk(Battery, tradeValue: 0), Plain(Wire), Edible(Rice, 5f, 1f), Plain(Salt), Edible(Stew, 20f, 3f, 0f, 0f, -1f) },
        cooking: new[] { Recipe(900, normal: Stew, good: GoodStew, ingredients: new[] { Salt }) },
        brewProducts: new IReadOnlyList<int>[] { new[] { Ale, 15504, 15506 } },
        talents: new[]
        {
            new TalentRow(Lounging, PlanBadges.PlanningCardType, 313020),
            new TalentRow(Energetic, PlanBadges.PlanningCardType, 306510),
            new TalentRow(Airdrop, PlanBadges.PlanningCardType, 313010),
            new TalentRow(Awakening, 1, 100110),
        },
        buffDurations: new Dictionary<int, float> { [306510] = 259200f, [313010] = 1f, [313020] = -1f, [100110] = 86400f });

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
        Assert.Equal(new[] { "desc", "dish", "brew", "scrap", "plan", "bait", "robot", "items", "wiki", "ui" }, Keys(Build(AllOn)));
    }

    [Fact]
    public void A_piece_turned_off_sends_nothing_for_it()
    {
        Assert.Equal(new[] { "desc", "bait", "robot", "items", "wiki", "ui" }, Keys(Build(AllOff)));
        Assert.Equal(new[] { "desc", "scrap", "bait", "robot", "items", "wiki", "ui" }, Keys(Build(AllOff with { ScrapOnly = true })));
        Assert.Equal(new[] { "desc", "brew", "bait", "robot", "items", "wiki", "ui" }, Keys(Build(AllOff with { AlcoholQuality = true })));
        Assert.Equal(new[] { "desc", "dish", "bait", "robot", "items", "wiki", "ui" }, Keys(Build(AllOff with { CookingCodex = true })));
        Assert.Equal(new[] { "desc", "plan", "bait", "robot", "items", "wiki", "ui" }, Keys(Build(AllOff with { PlanningBadges = true })));
    }

    [Fact]
    public void Trade_stats_need_no_facts_of_their_own_because_the_wiki_carries_the_effects()
    {
        Assert.Equal(new[] { "desc", "bait", "robot", "items", "wiki", "ui" }, Keys(Build(AllOff with { TradeStats = true })));
    }

    [Fact]
    public void Base_counts_need_no_facts_because_counts_travel_on_their_own()
    {
        Assert.Equal(new[] { "desc", "bait", "robot", "items", "wiki", "ui" }, Keys(Build(AllOff with { BaseCounts = true })));
    }

    [Fact]
    public void Every_tooltip_gets_the_item_wiki_with_every_piece_off()
    {
        var facts = Build(AllOff);

        Assert.Equal(new[] { 20f, 3f, 0f, 0f, -1f }, facts.GetProperty("wiki").GetProperty("701").GetProperty("fx").EnumerateArray().Select(value => value.GetSingle()).ToArray());
        Assert.Equal(JsonValueKind.Array, facts.GetProperty("items").GetProperty("701").ValueKind);
    }

    [Fact]
    public void Descriptions_cover_every_item_that_has_one()
    {
        var descriptions = Build(AllOn).GetProperty("desc");

        Assert.Equal(new[] { "101", "501", "601", "602", "701" }, Keys(descriptions));
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
    public void Planning_badges_go_to_timed_and_one_off_cards_in_id_order()
    {
        var plan = Build(AllOn).GetProperty("plan");

        Assert.Equal(new[] { "30651", "31301" }, Keys(plan));
        Assert.Equal("3d", plan.GetProperty("30651").GetString());
        Assert.Equal("1x", plan.GetProperty("31301").GetString());
    }

    [Fact]
    public void The_same_tables_always_give_the_same_text()
    {
        Assert.Equal(PageFacts.Build(World(), Text(), AllOn), PageFacts.Build(World(), Text(), AllOn));
    }

    [Fact]
    public void Bait_shows_its_kind_in_the_players_language_and_skips_a_kind_with_no_text()
    {
        var text = new FakeText().Localises("Bait_Meat", " Meat ").Localises("Bait_Blank", "  ");
        var bait = new Dictionary<int, string> { [11] = "Bait_Meat", [12] = "Bait_Meat", [13] = "Bait_Blank", [14] = "Bait_Missing" };

        Assert.Equal(new Dictionary<int, string> { [11] = "Meat", [12] = "Meat" }, PageFacts.BaitKinds(bait, text));
    }

    [Fact]
    public void Counts_go_out_in_id_order_whatever_order_they_came_in()
    {
        Assert.Equal("{\"7\":1,\"40\":2,\"301\":5}", PageFacts.Counts(new Dictionary<int, int> { [301] = 5, [7] = 1, [40] = 2 }));
        Assert.Equal("{}", PageFacts.Counts(new Dictionary<int, int>()));
    }

    [Fact]
    public void The_page_words_follow_the_language_the_game_shows()
    {
        var english = JsonDocument.Parse(PageFacts.Build(World(), Text(), AllOff)).RootElement.GetProperty("ui");
        var chinese = JsonDocument.Parse(PageFacts.Build(World(), new FakeText { Chinese = true }, AllOff)).RootElement.GetProperty("ui");

        Assert.Equal(Texts.BrewQuality.En, english.GetProperty("brew").GetString());
        Assert.Equal(Texts.BrewQuality.Zh, chinese.GetProperty("brew").GetString());
        Assert.Equal(Texts.ScrapChip.Zh, chinese.GetProperty("scrapChip").GetString());
    }

    [Fact]
    public void Every_page_word_is_sent_under_its_own_key()
    {
        var ui = JsonDocument.Parse(PageFacts.Ui(false)).RootElement;

        Assert.Equal(Texts.ByPageKey.Select(pair => pair.Key), Keys(ui));
        Assert.All(Texts.ByPageKey, pair => Assert.Equal(pair.Value.En, ui.GetProperty(pair.Key).GetString()));
    }

    [Fact]
    public void Every_phrase_has_both_languages_and_the_same_placeholders()
    {
        foreach (var pair in Texts.ByPageKey)
        {
            Assert.False(string.IsNullOrWhiteSpace(pair.Value.En));
            Assert.False(string.IsNullOrWhiteSpace(pair.Value.Zh));
            Assert.NotEqual(pair.Value.En, pair.Value.Zh);

            for (var slot = 0; slot < 5; slot++)
            {
                var token = "{" + slot + "}";

                Assert.Equal(pair.Value.En.Contains(token), pair.Value.Zh.Contains(token));
            }
        }
    }
}
