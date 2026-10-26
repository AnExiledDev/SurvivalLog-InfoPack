using System.Collections.Generic;
using System.Text.Json;
using InfoPack.Core;
using Xunit;

namespace InfoPack.Tests;

public class WantedTests
{
    private const int Rope = 301;
    private const int Cable = 302;
    private const int Nails = 303;
    private const int Bread = 701;

    private static bool SameItem(int demandItem, int item) => demandItem == item;

    [Fact]
    public void An_item_a_request_names_needs_what_is_still_unpaid()
    {
        var needs = Wanted.AidNeeds(new[] { new AidDemand(AidDemand.ItemKind, Rope, Need: 5, Paid: 2) }, new[] { Rope, Cable }, SameItem);

        Assert.Equal(new Dictionary<int, int> { [Rope] = 3 }, needs);
    }

    [Fact]
    public void Two_requests_for_the_same_item_add_up()
    {
        var demands = new[] { new AidDemand(AidDemand.ItemKind, Rope, 4, 0), new AidDemand(AidDemand.ItemKind, Rope, 2, 1) };

        Assert.Equal(5, Wanted.AidNeeds(demands, new[] { Rope }, SameItem)[Rope]);
    }

    [Fact]
    public void A_fully_paid_demand_wants_nothing_more()
    {
        var needs = Wanted.AidNeeds(new[] { new AidDemand(AidDemand.ItemKind, Rope, 3, 3), new AidDemand(AidDemand.ItemKind, Nails, 2, 5) }, new[] { Rope, Nails }, SameItem);

        Assert.Empty(needs);
    }

    [Fact]
    public void A_demand_for_an_amount_of_a_category_marks_no_item()
    {
        var needs = Wanted.AidNeeds(new[] { new AidDemand(Kind: 1, ItemId: Rope, Need: 10, Paid: 0) }, new[] { Rope, Bread }, (_, _) => true);

        Assert.Empty(needs);
    }

    [Fact]
    public void The_game_decides_which_items_answer_a_demand()
    {
        // A demand for rope that the game also fills with cable.
        static bool RopeTakesCable(int demandItem, int item) => demandItem == item || (demandItem == Rope && item == Cable);

        var needs = Wanted.AidNeeds(new[] { new AidDemand(AidDemand.ItemKind, Rope, 2, 0) }, new[] { Rope, Cable, Nails }, RopeTakesCable);

        Assert.Equal(new Dictionary<int, int> { [Rope] = 2, [Cable] = 2 }, needs);
    }

    [Fact]
    public void The_page_gets_the_craving_the_requests_and_the_untried_foods_by_id()
    {
        var json = Wanted.ToJson(new[] { Bread }, new Dictionary<int, int> { [Rope] = 3 }, new[] { 702 });
        var root = JsonDocument.Parse(json).RootElement;

        Assert.Equal(Bread, root.GetProperty("wish")[0].GetInt32());
        Assert.Equal(3, root.GetProperty("aid").GetProperty("301").GetInt32());
        Assert.Equal(702, root.GetProperty("untried")[0].GetInt32());
    }

    [Fact]
    public void The_same_state_always_gives_the_same_text_so_the_page_is_not_resent()
    {
        var once = Wanted.ToJson(new[] { 2, 1 }, new Dictionary<int, int> { [9] = 1, [3] = 2 }, new[] { 5, 4 });
        var again = Wanted.ToJson(new[] { 1, 2 }, new Dictionary<int, int> { [3] = 2, [9] = 1 }, new[] { 4, 5 });

        Assert.Equal(once, again);
        Assert.Equal("{\"wish\":[],\"aid\":{},\"untried\":[]}", Wanted.ToJson(new int[0], new Dictionary<int, int>(), new int[0]));
    }
}
