using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InfoPack.Core;
using Xunit;
using static InfoPack.Tests.Rows;

namespace InfoPack.Tests;

public class FactsBuildTests
{
    private const int Battery = 101;
    private const int Rice = 601;
    private const int Bread = 602;

    private static readonly Features AllOn = new(true, true, true, true, true, true);

    private static GameTables World() => Tables(new[]
    {
        Junk(Battery),
        Edible(Rice, 5f, 1f) with { InFoodCodex = true },
        Edible(Bread, 20f) with { InFoodCodex = true },
        Edible(Bread, 99f) with { InFoodCodex = true },
    });

    private static FakeText Text() => new FakeText().Describes(Battery, "A dead battery.").Names(Rice, "Rice");

    /// <summary>Runs the build at once, on the caller's thread.</summary>
    private static Task<BuiltFacts> AtOnce(Func<BuiltFacts> work) => Task.FromResult(work());

    [Fact]
    public void The_steps_end_with_the_facts_the_game_text_gives()
    {
        var tables = World();
        BuiltFacts? built = null;

        foreach (var step in FactsBuild.Steps(tables, new TextSnapshot(), Text(), AllOn, AtOnce, facts => built = facts)) { }

        Assert.NotNull(built);
        Assert.Equal(PageFacts.Build(tables, Text(), AllOn), built!.Json);
    }

    [Fact]
    public void A_build_still_running_elsewhere_waits_a_slice_at_a_time_and_then_finishes()
    {
        var tables = World();
        var worker = new TaskCompletionSource<BuiltFacts>();
        Func<BuiltFacts>? handed = null;
        BuiltFacts? built = null;
        var job = new SlicedJob(FactsBuild.Steps(tables, new TextSnapshot(), Text(), AllOn, work =>
        {
            handed = work;

            return worker.Task;
        }, facts => built = facts));

        Assert.False(job.Advance(() => false));
        Assert.False(job.Advance(() => false));
        Assert.NotNull(handed);
        Assert.Null(built);

        worker.SetResult(handed!());

        Assert.True(job.Advance(() => false));
        Assert.Equal(PageFacts.Build(tables, Text(), AllOn), built!.Json);
    }

    [Fact]
    public void A_build_that_failed_elsewhere_fails_the_step_that_finds_it()
    {
        var steps = FactsBuild.Steps(World(), new TextSnapshot(), Text(), AllOn, work => Task.FromException<BuiltFacts>(new InvalidOperationException("bad row")), facts => { });

        var error = Assert.Throws<InvalidOperationException>(() => { foreach (var step in steps) { } });

        Assert.Equal("bad row", error.Message);
    }

    [Fact]
    public void Text_already_copied_is_not_read_from_the_game_again()
    {
        var tables = World();
        var copy = new TextSnapshot();

        foreach (var step in FactsBuild.Steps(tables, copy, Text(), AllOn, AtOnce, facts => { })) { }

        BuiltFacts? built = null;
        var steps = FactsBuild.Steps(tables, copy, new NoText(), AllOn, AtOnce, facts => built = facts).Count();

        // A settings change builds again straight from the copy, with no step spent on the text.
        Assert.Equal(0, steps);
        Assert.Equal(PageFacts.Build(tables, Text(), AllOn), built!.Json);
    }

    /// <summary>Game text that must not be read.</summary>
    private sealed class NoText : IGameText
    {
        public string? Description(int itemId) => throw new InvalidOperationException("read the game text");

        public string? ItemName(int itemId) => throw new InvalidOperationException("read the game text");

        public string? FurnitureName(int furnitureId) => throw new InvalidOperationException("read the game text");

        public string? PlaceName(int placeId) => throw new InvalidOperationException("read the game text");

        public string? LocalText(string key) => throw new InvalidOperationException("read the game text");

        public bool IsChinese() => throw new InvalidOperationException("read the game text");
    }

    [Fact]
    public void The_live_lists_hold_each_item_once_and_the_codex_foods()
    {
        var lists = LiveLists.From(World().Items);

        Assert.Equal(new[] { Battery, Rice, Bread }, lists.ItemIds);
        Assert.Equal(new[] { Rice, Bread }, lists.FoodCodex);
        Assert.Equal(new Dictionary<int, float> { [Rice] = 5f, [Bread] = 20f }, lists.Satiety);
    }
}
