using System;
using System.Collections.Generic;
using InfoPack.Core;
using Xunit;

namespace InfoPack.Tests;

public class UntriedFoodsTests
{
    private const int Bread = 701;
    private const int Stew = 702;
    private const int Jerky = 703;

    /// <summary>The game's codex as the check sees it, counting every question asked.</summary>
    private sealed class Codex
    {
        public readonly HashSet<int> Eaten = new();

        public int Questions { get; private set; }

        public int UnlockedCount => Eaten.Count;

        public bool IsUnlocked(int id)
        {
            Questions++;

            return Eaten.Contains(id);
        }
    }

    [Fact]
    public void Foods_never_eaten_come_out_once_each_in_id_order()
    {
        var foods = new UntriedFoods();
        var codex = new Codex();

        codex.Eaten.Add(Bread);
        foods.Check(new[] { Jerky, Bread, Stew, Jerky }, codex.UnlockedCount, codex.IsUnlocked);

        Assert.Equal(new[] { Stew, Jerky }, foods.Items);
    }

    [Fact]
    public void A_check_where_the_codex_count_stood_still_asks_the_game_nothing()
    {
        var list = new[] { Bread, Stew, Jerky };
        var foods = new UntriedFoods();
        var codex = new Codex();

        foods.Check(list, codex.UnlockedCount, codex.IsUnlocked);

        var asked = codex.Questions;
        var version = foods.Version;

        foods.Check(list, codex.UnlockedCount, codex.IsUnlocked);
        foods.Check(list, codex.UnlockedCount, codex.IsUnlocked);

        Assert.Equal(asked, codex.Questions);
        Assert.Equal(version, foods.Version);
    }

    [Fact]
    public void A_food_eaten_since_the_last_check_is_dropped_and_moves_the_version()
    {
        var list = new[] { Bread, Stew, Jerky };
        var foods = new UntriedFoods();
        var codex = new Codex();

        foods.Check(list, codex.UnlockedCount, codex.IsUnlocked);

        var version = foods.Version;

        codex.Eaten.Add(Stew);
        foods.Check(list, codex.UnlockedCount, codex.IsUnlocked);

        Assert.Equal(new[] { Bread, Jerky }, foods.Items);
        Assert.NotEqual(version, foods.Version);
    }

    [Fact]
    public void A_food_found_eaten_is_not_asked_about_again()
    {
        var list = new[] { Bread, Stew, Jerky };
        var foods = new UntriedFoods();
        var codex = new Codex();

        codex.Eaten.Add(Stew);
        foods.Check(list, codex.UnlockedCount, codex.IsUnlocked);

        var asked = codex.Questions;

        codex.Eaten.Add(Bread);
        foods.Check(list, codex.UnlockedCount, codex.IsUnlocked);

        // Bread and Jerky only: Stew is off the list for good.
        Assert.Equal(asked + 2, codex.Questions);
        Assert.Equal(new[] { Jerky }, foods.Items);
    }

    [Fact]
    public void A_new_world_starts_the_list_over()
    {
        var list = new[] { Bread, Stew };
        var foods = new UntriedFoods();
        var codex = new Codex();

        codex.Eaten.Add(Bread);
        foods.Check(list, codex.UnlockedCount, codex.IsUnlocked);

        var version = foods.Version;

        foods.Reset();

        Assert.Empty(foods.Items);
        Assert.NotEqual(version, foods.Version);

        // The new save has eaten nothing, and its codex count happens to match the old one's.
        var fresh = new Codex();

        fresh.Eaten.Add(999);
        foods.Check(list, fresh.UnlockedCount, fresh.IsUnlocked);

        Assert.Equal(new[] { Bread, Stew }, foods.Items);
    }

    [Fact]
    public void New_facts_start_the_list_over_from_the_new_foods()
    {
        var foods = new UntriedFoods();
        var codex = new Codex();

        foods.Check(new[] { Bread }, codex.UnlockedCount, codex.IsUnlocked);
        foods.Check(new[] { Bread, Jerky }, codex.UnlockedCount, codex.IsUnlocked);

        Assert.Equal(new[] { Bread, Jerky }, foods.Items);
    }

    [Fact]
    public void A_check_that_failed_part_way_is_done_again_in_full()
    {
        var list = new[] { Bread, Stew, Jerky };
        var foods = new UntriedFoods();
        var codex = new Codex();

        foods.Check(list, codex.UnlockedCount, codex.IsUnlocked);
        codex.Eaten.Add(Bread);

        Assert.Throws<InvalidOperationException>(() => foods.Check(list, codex.UnlockedCount, id => id == Stew ? throw new InvalidOperationException() : codex.IsUnlocked(id)));

        foods.Check(list, codex.UnlockedCount, codex.IsUnlocked);

        Assert.Equal(new[] { Stew, Jerky }, foods.Items);
    }
}
