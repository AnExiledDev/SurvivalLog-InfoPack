using InfoPack.Core;
using Xunit;

namespace InfoPack.Tests;

public class FactsScheduleTests
{
    private static readonly FactsKey English = new(World: 1, Language: "en");

    [Fact]
    public void The_first_look_happens_at_once()
    {
        Assert.True(new FactsSchedule(2f).IsDue(0f));
    }

    [Fact]
    public void An_idle_frame_between_looks_reads_nothing()
    {
        var schedule = new FactsSchedule(2f);

        schedule.Checked(10f);

        Assert.False(schedule.IsDue(10.016f));
        Assert.False(schedule.IsDue(11.9f));
        Assert.True(schedule.IsDue(12f));
    }

    [Fact]
    public void A_change_of_features_builds_the_facts_again_at_once_for_the_same_world()
    {
        var schedule = new FactsSchedule(2f);

        schedule.Building(English);
        schedule.Checked(10f);
        schedule.SettingsChanged();

        Assert.True(schedule.IsDue(10.016f));
        Assert.True(schedule.NeedsBuild(English));
    }

    [Fact]
    public void Facts_are_built_once_for_a_world_and_language()
    {
        var schedule = new FactsSchedule(2f);

        Assert.True(schedule.NeedsBuild(English));

        schedule.Building(English);

        Assert.False(schedule.NeedsBuild(English));
    }

    [Fact]
    public void A_new_save_rebuilds_the_facts()
    {
        var schedule = new FactsSchedule(2f);

        schedule.Building(English);

        Assert.True(schedule.NeedsBuild(English with { World = 2 }));
    }

    [Fact]
    public void A_language_change_rebuilds_the_descriptions_in_the_new_language()
    {
        var schedule = new FactsSchedule(2f);

        schedule.Building(English);

        Assert.True(schedule.NeedsBuild(English with { Language = "zh" }));
        Assert.True(schedule.NeedsBuild(English with { Language = null }));
    }

    [Fact]
    public void A_reloaded_page_gets_the_facts_without_waiting_for_the_next_look()
    {
        var schedule = new FactsSchedule(2f);

        schedule.Checked(10f);
        schedule.PageReloaded();

        Assert.True(schedule.IsDue(10.5f));
    }

    [Fact]
    public void Facts_built_before_the_player_exists_are_built_again_once_it_does()
    {
        var schedule = new FactsSchedule(2f);

        schedule.Building(English with { HasPlayer = false });

        Assert.False(schedule.NeedsBuild(English with { HasPlayer = false }));
        Assert.True(schedule.NeedsBuild(English));
    }
}
