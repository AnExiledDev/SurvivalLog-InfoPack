using System.Collections.Generic;
using System.Text.Json;
using InfoPack.Core;
using Xunit;

namespace InfoPack.Tests;

public class StatDrainTests
{
    private const int Satiety = 1;
    private const int Mood = 2;
    private const int Hour = 3600;

    private static StatDrain Drain() => new(windowSeconds: 4 * Hour, minimumSeconds: Hour / 2);

    private static Dictionary<int, float> At(float satiety, float mood = 50f) => new() { [Satiety] = satiety, [Mood] = mood };

    [Fact]
    public void A_steady_drop_reads_as_its_rate_per_game_hour()
    {
        var drain = Drain();

        drain.Add(0, At(80f));
        drain.Add(Hour, At(77f));
        drain.Add(2 * Hour, At(74f));

        Assert.Equal(3f, drain.PerHour(Satiety));
    }

    [Fact]
    public void Eating_in_between_does_not_hide_the_drain()
    {
        var drain = Drain();

        drain.Add(0, At(50f));
        drain.Add(Hour, At(46f));
        drain.Add(Hour + 60, At(90f));
        drain.Add(2 * Hour + 60, At(86f));

        var perHour = drain.PerHour(Satiety);

        Assert.NotNull(perHour);
        Assert.InRange(perHour.Value, 3.9f, 4.0f);
    }

    [Fact]
    public void No_rate_is_given_before_enough_game_time_has_passed()
    {
        var drain = Drain();

        drain.Add(0, At(80f));
        drain.Add(Hour / 4, At(79f));

        Assert.Null(drain.PerHour(Satiety));
        Assert.Equal("{}", drain.ToJson(new[] { Satiety }));
    }

    [Fact]
    public void A_clock_that_runs_backwards_starts_the_measurement_over()
    {
        var drain = Drain();

        drain.Add(10 * Hour, At(80f));
        drain.Add(11 * Hour, At(70f));
        drain.Add(2 * Hour, At(60f));

        Assert.Null(drain.PerHour(Satiety));
        Assert.Equal(60f, drain.Latest(Satiety));
    }

    [Fact]
    public void A_paused_clock_adds_nothing()
    {
        var drain = Drain();

        drain.Add(0, At(80f));
        drain.Add(Hour, At(78f));
        drain.Add(Hour, At(10f));

        Assert.Equal(2f, drain.PerHour(Satiety));
        Assert.Equal(78f, drain.Latest(Satiety));
    }

    [Fact]
    public void Only_the_last_few_game_hours_count()
    {
        var drain = Drain();

        // A fast drop long ago, then a slow one for the whole window.
        drain.Add(0, At(100f));
        drain.Add(Hour, At(80f));

        for (var hour = 2; hour <= 10; hour++) drain.Add(hour * Hour, At(80f - (hour - 1)));

        var perHour = drain.PerHour(Satiety);

        Assert.NotNull(perHour);
        Assert.InRange(perHour.Value, 0.99f, 1.01f);
    }

    [Fact]
    public void The_page_gets_the_rate_and_the_hours_left_for_each_dropping_stat()
    {
        var drain = Drain();

        drain.Add(0, At(80f, mood: 50f));
        drain.Add(2 * Hour, At(76f, mood: 50f));

        var root = JsonDocument.Parse(drain.ToJson(new[] { Satiety, Mood })).RootElement;
        var satiety = root.GetProperty("1");

        Assert.Equal(2f, satiety[0].GetSingle());
        Assert.Equal(38f, satiety[1].GetSingle());
        Assert.False(root.TryGetProperty("2", out _));
    }

    [Fact]
    public void A_new_world_forgets_the_old_readings()
    {
        var drain = Drain();

        drain.Add(0, At(80f));
        drain.Add(Hour, At(70f));
        drain.Reset();

        Assert.Null(drain.PerHour(Satiety));
        Assert.Null(drain.Latest(Satiety));
    }

    [Fact]
    public void The_version_moves_only_when_the_readings_change()
    {
        var drain = Drain();
        var start = drain.Version;

        drain.Add(0, At(80f));

        var first = drain.Version;

        drain.Add(0, At(10f));

        Assert.NotEqual(start, first);
        // A paused clock measured nothing new.
        Assert.Equal(first, drain.Version);

        drain.Add(Hour, At(78f));

        var second = drain.Version;

        drain.Reset();

        Assert.NotEqual(first, second);
        Assert.NotEqual(second, drain.Version);
    }

    [Fact]
    public void A_reading_is_kept_as_it_was_when_the_caller_reuses_its_dictionary()
    {
        var drain = Drain();
        var values = At(80f);

        drain.Add(0, values);
        values[Satiety] = 10f;
        drain.Add(Hour, At(78f));

        Assert.Equal(2f, drain.PerHour(Satiety));
    }

    [Fact]
    public void A_stat_from_a_dropped_reading_does_not_turn_up_in_a_later_one()
    {
        var drain = Drain();

        drain.Add(10 * Hour, At(80f, mood: 50f));
        drain.Add(11 * Hour, At(78f, mood: 40f));
        // A loaded save: both readings are handed back for reuse.
        drain.Add(0, new Dictionary<int, float> { [Satiety] = 90f });
        drain.Add(Hour, new Dictionary<int, float> { [Satiety] = 87f });

        Assert.Null(drain.Latest(Mood));
        Assert.Equal(3f, drain.PerHour(Satiety));
    }
}
