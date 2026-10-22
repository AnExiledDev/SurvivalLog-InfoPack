using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using InfoPack.Core;
using Xunit;
using static InfoPack.Tests.Rows;

namespace InfoPack.Tests;

public class BrewMixTests
{
    private const int Poor = 15505;
    private const int Normal = 15504;
    private const int Strong = 15506;
    private const int Item = 2001;

    private static readonly int[] Barrel = { Poor, Normal, Strong };

    private static ItemRow Brewable(float satiety, float mood, int useTimes = 1) =>
        Plain(Item, Food, 1, satiety, mood, 0f, 0f, 0f) with { CanBrew = true, UseTimes = useTimes };

    private static ItemUse.Brew Mix(ItemRow item, BrewRules? rules = null, IReadOnlyList<int>? products = null) =>
        BrewMix.Of(item, rules ?? BrewRules.Default, products ?? Barrel);

    [Fact]
    public void Quality_is_mood_over_satiety_and_picks_the_drink_its_tier_makes()
    {
        var brew = Mix(Brewable(10f, 1f));

        Assert.Equal(10, brew.Satiety, 3);
        Assert.Equal(1, brew.Mood, 3);
        Assert.Equal(0.1, brew.Alone!.Quality, 3);
        Assert.Equal(Normal, brew.Alone.Product);
    }

    [Theory]
    [InlineData(20f, -3f, Poor)]
    [InlineData(10f, 0f, Normal)]
    [InlineData(10f, 4.99f, Normal)]
    [InlineData(10f, 5f, Strong)]
    [InlineData(2f, 12f, Strong)]
    public void A_tier_starts_at_its_threshold(float satiety, float mood, int drink)
    {
        Assert.Equal(drink, Mix(Brewable(satiety, mood)).Alone!.Product);
    }

    [Fact]
    public void Every_use_of_the_item_counts_toward_satiety_and_mood()
    {
        var brew = Mix(Brewable(20f, -3f, useTimes: 10));

        Assert.Equal(200, brew.Satiety, 3);
        Assert.Equal(-30, brew.Mood, 3);
        Assert.Equal(-0.15, brew.Alone!.Quality, 3);
        Assert.Equal(1, brew.Alone.ItemsPerBottle);
    }

    [Fact]
    public void An_item_with_no_uses_set_counts_once()
    {
        Assert.Equal(10, Mix(Brewable(10f, 1f, useTimes: 0)).Satiety, 3);
    }

    [Theory]
    [InlineData(10f, 3)]
    [InlineData(12.5f, 2)]
    [InlineData(2f, 13)]
    [InlineData(40f, 1)]
    public void Items_per_bottle_is_the_fewest_whose_satiety_fills_one(float satiety, int count)
    {
        Assert.Equal(count, Mix(Brewable(satiety, 0f)).Alone!.ItemsPerBottle);
    }

    [Fact]
    public void A_bigger_or_smaller_bottle_from_a_talent_changes_the_count()
    {
        Assert.Equal(2, Mix(Brewable(10f, 0f), new BrewRules(0f, 0.5f, 20f)).Alone!.ItemsPerBottle);
    }

    [Fact]
    public void An_item_that_adds_no_satiety_cannot_brew_alone_but_still_shows_its_mood()
    {
        var brew = Mix(Brewable(-5f, 4f));

        Assert.Equal(0, brew.Satiety, 3);
        Assert.Equal(4, brew.Mood, 3);
        Assert.Null(brew.Alone);
    }

    [Fact]
    public void Lowered_thresholds_from_a_talent_lift_a_mix_into_the_next_drink()
    {
        Assert.Equal(Normal, Mix(Brewable(20f, -3f), new BrewRules(-0.2f, 0.3f, 25f)).Alone!.Product);
    }

    [Fact]
    public void Thresholds_out_of_order_fall_back_to_the_games_own()
    {
        Assert.Equal(Strong, Mix(Brewable(10f, 6f), new BrewRules(0.7f, 0.7f, 25f)).Alone!.Product);
    }

    [Fact]
    public void A_barrel_with_fewer_drinks_gives_its_best_for_a_higher_tier_and_none_when_empty()
    {
        Assert.Equal(Normal, Mix(Brewable(2f, 12f), products: new[] { Poor, Normal }).Alone!.Product);
        Assert.Equal(0, Mix(Brewable(2f, 12f), products: new int[0]).Alone!.Product);
    }

    [Fact]
    public void The_wiki_sends_the_brew_numbers_and_names_the_drink()
    {
        var tables = Tables(new[] { Brewable(7.5f, -4f, useTimes: 7), Plain(Poor, tradeValue: 0) }, brewProducts: new[] { Barrel });
        var wiki = ItemWiki.Build(tables, new FakeText());
        var use = JsonDocument.Parse(WikiJson.Wiki(wiki)).RootElement.GetProperty(Item.ToString(CultureInfo.InvariantCulture)).GetProperty("use");

        Assert.Equal("[{\"k\":\"brew\",\"s\":52.5,\"m\":-28,\"q\":-0.53,\"n\":1,\"out\":15505}]", use.GetRawText());
        Assert.Contains(Poor, wiki[Item].ReferencedItems());
    }

    [Fact]
    public void The_wiki_sends_only_satiety_and_mood_for_an_item_that_cannot_brew_alone()
    {
        var wiki = ItemWiki.Build(Tables(new[] { Brewable(0f, 2f) }, brewProducts: new[] { Barrel }), new FakeText());
        var use = JsonDocument.Parse(WikiJson.Wiki(wiki)).RootElement.GetProperty(Item.ToString(CultureInfo.InvariantCulture)).GetProperty("use");

        Assert.Equal("[{\"k\":\"brew\",\"s\":0,\"m\":2}]", use.GetRawText());
        Assert.Empty(wiki[Item].ReferencedItems().Where(id => id == Poor || id == Normal || id == Strong));
    }
}
