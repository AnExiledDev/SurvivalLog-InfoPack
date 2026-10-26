using System.Collections.Generic;
using InfoPack.Core;
using Xunit;

namespace InfoPack.Tests;

public class StatsPrintTests
{
    private const int Satiety = 1;
    private const int Mood = 2;
    private const int Hour = 3600;
    private static readonly int[] Shown = { Satiety, Mood };

    private static StatDrain Drain() => new(windowSeconds: 4 * Hour, minimumSeconds: Hour / 2);

    private static Dictionary<int, float> At(float satiety, float mood = 50f) => new() { [Satiety] = satiety, [Mood] = mood };

    private static StatDrain Dropping()
    {
        var drain = Drain();

        drain.Add(0, At(80f, mood: 60f));
        drain.Add(2 * Hour, At(76f, mood: 50f));

        return drain;
    }

    [Fact]
    public void The_first_numbers_are_always_sent()
    {
        Assert.True(new StatsPrint(Shown).Update(Drain(), withRates: true, days: null));
    }

    [Fact]
    public void Numbers_that_came_out_the_same_send_nothing()
    {
        var drain = Dropping();
        var print = new StatsPrint(Shown);

        print.Update(drain, true, 3.04f);

        Assert.False(print.Update(drain, true, 3.04f));
        // The page shows a tenth of a day, so a smaller move is no change to it.
        Assert.False(print.Update(drain, true, 3.01f));
    }

    [Fact]
    public void A_rate_that_moved_is_sent()
    {
        var drain = Dropping();
        var print = new StatsPrint(Shown);

        print.Update(drain, true, null);
        drain.Add(3 * Hour, At(70f, mood: 50f));

        Assert.True(print.Update(drain, true, null));
    }

    [Fact]
    public void A_forecast_that_moved_or_went_away_is_sent()
    {
        var drain = Dropping();
        var print = new StatsPrint(Shown);

        print.Update(drain, true, 3f);

        Assert.True(print.Update(drain, true, 2.5f));
        Assert.True(print.Update(drain, true, null));
    }

    [Fact]
    public void Turning_the_rates_off_is_a_change()
    {
        var drain = Dropping();
        var print = new StatsPrint(Shown);

        print.Update(drain, true, null);

        Assert.True(print.Update(drain, false, null));
        Assert.Equal("{\"rates\":{}}", print.Json());
    }

    [Fact]
    public void The_message_is_the_one_the_drain_and_the_forecast_give()
    {
        var drain = Dropping();
        var print = new StatsPrint(new[] { Mood, Satiety, Mood });

        print.Update(drain, true, 3.04f);

        Assert.Equal(FoodForecast.StatsJson(drain.ToJson(Shown), 3.04f), print.Json());
    }

    [Fact]
    public void A_reloaded_page_gets_the_same_numbers_again()
    {
        var drain = Dropping();
        var print = new StatsPrint(Shown);

        print.Update(drain, true, 3f);
        print.Forget();

        Assert.True(print.Update(drain, true, 3f));
        Assert.False(print.Update(drain, true, 3f));
    }

    [Fact]
    public void A_new_world_drops_the_rates_and_says_so()
    {
        var drain = Dropping();
        var print = new StatsPrint(Shown);

        print.Update(drain, true, null);
        drain.Reset();

        Assert.True(print.Update(drain, true, null));
        Assert.Equal("{\"rates\":{}}", print.Json());
    }
}
