using System;
using System.Globalization;
using System.Text.Json;
using InfoPack.Core;
using Xunit;
using static InfoPack.Tests.Rows;

namespace InfoPack.Tests;

public class FuelBurnTests
{
    private const int Wood = 3001;
    private const int Pie = 400;
    private const int Flour = 10;

    private static FuelRates Rates(float[]? cooking = null, float cookingSaving = 0f, float[]? heaters = null, float[]? generators = null, float generatorSaving = 0f) =>
        new(cooking ?? Array.Empty<float>(), cookingSaving, heaters ?? Array.Empty<float>(), generators ?? Array.Empty<float>(), generatorSaving);

    [Fact]
    public void A_cooking_pot_uses_its_rate_every_game_second_of_cooking()
    {
        var times = FuelBurn.Of(3000, Rates(cooking: new[] { 0.45f }))!;

        Assert.Equal(new BurnSpan(6667, 6667), times.Cooking);
        Assert.Null(times.Heater);
        Assert.Null(times.Generator);
    }

    [Fact]
    public void A_heater_or_generator_uses_its_rate_every_600_game_seconds()
    {
        var times = FuelBurn.Of(3000, Rates(heaters: new[] { 5f }, generators: new[] { 10f }))!;

        Assert.Equal(new BurnSpan(360000, 360000), times.Heater);
        Assert.Equal(new BurnSpan(180000, 180000), times.Generator);
    }

    [Fact]
    public void Burners_of_one_kind_with_different_rates_give_the_shortest_and_longest()
    {
        var times = FuelBurn.Of(1200, Rates(heaters: new[] { 4f, 2f, 3f }))!;

        Assert.Equal(new BurnSpan(180000, 360000), times.Heater);
    }

    [Fact]
    public void Fuel_saving_talents_make_fuel_last_longer_in_pots_and_generators_but_not_heaters()
    {
        var times = FuelBurn.Of(1000, Rates(cooking: new[] { 0.5f }, cookingSaving: 0.2f, heaters: new[] { 1f }, generators: new[] { 1f }, generatorSaving: 0.5f))!;

        Assert.Equal(new BurnSpan(2500, 2500), times.Cooking);
        Assert.Equal(new BurnSpan(600000, 600000), times.Heater);
        Assert.Equal(new BurnSpan(1200000, 1200000), times.Generator);
    }

    [Fact]
    public void Nothing_burns_when_the_item_gives_no_fuel_or_no_burner_takes_any()
    {
        Assert.Null(FuelBurn.Of(0, Rates(cooking: new[] { 0.45f })));
        Assert.Null(FuelBurn.Of(3000, FuelRates.None));
        Assert.Null(FuelBurn.Of(3000, Rates(cooking: new[] { 0f }, heaters: new[] { -1f })));
    }

    [Fact]
    public void The_wiki_sends_burn_times_only_for_items_that_burn()
    {
        var tables = Tables(new[] { Plain(Wood) with { IsBurnable = true, BurnValue = 3000 }, Plain(Flour) with { BurnValue = 3000 } }) with
        {
            Fuel = Rates(cooking: new[] { 0.45f }, generators: new[] { 10f, 20f }),
        };
        var wiki = JsonDocument.Parse(WikiJson.Wiki(ItemWiki.Build(tables, new FakeText()))).RootElement;

        Assert.Equal("{\"c\":[6667,6667],\"g\":[90000,180000]}", wiki.GetProperty(Key(Wood)).GetProperty("bt").GetRawText());
        Assert.False(wiki.TryGetProperty(Key(Flour), out var flour) && flour.TryGetProperty("bt", out _));
    }

    [Fact]
    public void A_cooking_use_sends_the_recipe_cook_time()
    {
        var tables = Tables(
            new[] { Plain(Flour, tradeValue: 0), Plain(Pie, tradeValue: 0) },
            cooking: new[] { Recipe(1, normal: Pie, ingredients: new[] { Flour }) with { CookTime = 900 } });
        var wiki = JsonDocument.Parse(WikiJson.Wiki(ItemWiki.Build(tables, new FakeText()))).RootElement;

        Assert.Equal("[{\"k\":\"cook\",\"out\":400,\"t\":900}]", wiki.GetProperty(Key(Flour)).GetProperty("use").GetRawText());
    }

    private static string Key(int id) => id.ToString(CultureInfo.InvariantCulture);
}
