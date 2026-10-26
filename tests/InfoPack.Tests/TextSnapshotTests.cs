using System.Collections.Generic;
using System.Linq;
using InfoPack.Core;
using Xunit;
using static InfoPack.Tests.Rows;

namespace InfoPack.Tests;

public class TextSnapshotTests
{
    private const int Battery = 101;
    private const int Rice = 601;
    private const int Crate = 40;
    private const int Shelf = 41;
    private const int Market = 7;

    private static readonly Features AllOn = new(true, true, true, true, true, true);

    private static GameTables World() => Tables(
        new[] { Junk(Battery), Edible(Rice, 5f, 1f) },
        lootFurniture: new[] { new LootFurniture(Crate, new[] { 1 }), new LootFurniture(Shelf, new[] { 2 }) },
        lootGroups: new Dictionary<int, IReadOnlyList<int>> { [1] = new[] { Battery }, [2] = new[] { Rice } },
        placements: new[] { new Placement(Crate, Market), new Placement(Shelf, 0) }) with
    {
        TrapBait = new Dictionary<int, string> { [Rice] = "bait_grain", [Battery] = "bait_grain" },
    };

    private static FakeText Text() => new FakeText { Chinese = true }
        .Describes(Battery, "A dead battery.")
        .Describes(Rice, "Plain rice.")
        .Names(Battery, "Battery")
        .Names(Rice, "Rice")
        .NamesFurniture(Crate, "Crate")
        .NamesFurniture(Shelf, "Shelf")
        .NamesPlace(Market, "Market")
        .Localises("bait_grain", "Grain");

    /// <summary>The game's text, counting each lookup.</summary>
    private sealed class Counted : IGameText
    {
        private readonly IGameText _text;

        public Counted(IGameText text)
        {
            _text = text;
        }

        public int Lookups { get; private set; }

        public string? Description(int itemId) => Count(_text.Description(itemId));

        public string? ItemName(int itemId) => Count(_text.ItemName(itemId));

        public string? FurnitureName(int furnitureId) => Count(_text.FurnitureName(furnitureId));

        public string? PlaceName(int placeId) => Count(_text.PlaceName(placeId));

        public string? LocalText(string key) => Count(_text.LocalText(key));

        public bool IsChinese() => _text.IsChinese();

        private string? Count(string? text)
        {
            Lookups++;

            return text;
        }
    }

    [Fact]
    public void Facts_built_from_the_copy_are_the_facts_built_from_the_game_text()
    {
        var tables = World();
        var copy = new TextSnapshot();

        foreach (var step in copy.Fill(tables, Text())) { }

        Assert.True(copy.IsFilled);
        Assert.Equal(PageFacts.Build(tables, Text(), AllOn), PageFacts.Build(tables, copy, AllOn));
    }

    [Fact]
    public void Each_step_reads_one_item_or_one_name()
    {
        var tables = World();
        var text = new Counted(Text());
        var copy = new TextSnapshot();
        var lookupsPerStep = new List<int>();
        var before = 0;

        foreach (var step in copy.Fill(tables, text))
        {
            lookupsPerStep.Add(text.Lookups - before);
            before = text.Lookups;
        }

        // An item's name and description, then each furniture kind, the one place (place 0 is no place) and the one
        // bait text two items share.
        Assert.Equal(new[] { 2, 2, 1, 1, 1, 1 }, lookupsPerStep);
    }

    [Fact]
    public void A_fill_cut_short_carries_on_without_asking_again()
    {
        var tables = World();
        var text = new Counted(Text());
        var copy = new TextSnapshot();

        foreach (var step in copy.Fill(tables, text).Take(3)) { }

        Assert.False(copy.IsFilled);

        var asked = text.Lookups;

        foreach (var step in copy.Fill(tables, text)) { }

        var all = new Counted(Text());

        foreach (var step in new TextSnapshot().Fill(tables, all)) { }

        Assert.True(copy.IsFilled);
        Assert.Equal(all.Lookups, text.Lookups);
        Assert.True(asked > 0);
    }

    [Fact]
    public void A_new_world_or_language_copies_the_text_afresh()
    {
        var tables = World();
        var copies = new TextCopies();
        var english = new FactsKey(World: 1, Language: "en");
        var first = copies.For(english, tables);

        Assert.Same(first, copies.For(english, tables));
        Assert.NotSame(first, copies.For(english with { Language = "zh" }, tables));
        Assert.NotSame(copies.For(english with { Language = "zh" }, tables), copies.For(english with { World = 2, Language = "zh" }, tables));
    }

    [Fact]
    public void New_tables_copy_the_text_afresh()
    {
        var copies = new TextCopies();
        var key = new FactsKey(World: 1, Language: "en");
        var first = copies.For(key, World());

        Assert.NotSame(first, copies.For(key, World()));
    }
}
