using System.Collections.Generic;
using InfoPack.Core;
using Xunit;
using static InfoPack.Tests.Rows;

namespace InfoPack.Tests;

public class ScrapRuleTests
{
    private const int Wire = 501;
    private const int Gear = 502;
    private const int Battery = 101;
    private const int Bread = 701;

    private static readonly Dictionary<int, IReadOnlyList<Yield>> BatteryShredsToTwoWire = new()
    {
        [Battery] = new[] { new Yield(Wire, 2) },
    };

    [Fact]
    public void A_shreddable_item_with_no_other_use_is_marked_as_scrap()
    {
        var tables = Tables(new[] { Junk(Battery, tradeValue: 4), Plain(Wire, tradeValue: 2) }, shredYields: BatteryShredsToTwoWire);

        Assert.Equal(new[] { Battery }, ScrapRule.ScrapOnly(tables));
    }

    public static TheoryData<string> OtherUses() => new()
    {
        "edible",
        "usable",
        "fuel",
        "brewable",
        "seed",
        "trap",
        "furniture",
    };

    [Theory]
    [MemberData(nameof(OtherUses))]
    public void An_item_with_any_other_use_is_never_marked(string use)
    {
        var item = use switch
        {
            "edible" => Junk(Battery) with { IsEdible = true },
            "usable" => Junk(Battery) with { IsUsable = true },
            "fuel" => Junk(Battery) with { IsBurnable = true },
            "brewable" => Junk(Battery) with { CanBrew = true },
            "seed" => Junk(Battery) with { IsSeed = true },
            "trap" => Junk(Battery) with { IsTrap = true },
            _ => Junk(Battery) with { PlacesFurniture = true },
        };

        Assert.False(ScrapRule.IsScrapOnly(item, isRecipeMaterial: false, scrapValue: 100));
    }

    [Fact]
    public void An_item_that_cannot_be_shredded_is_never_marked()
    {
        Assert.False(ScrapRule.IsScrapOnly(Junk(Battery) with { IsShreddable = false }, isRecipeMaterial: false, scrapValue: 100));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(Electrical)]
    [InlineData(DailyGoods)]
    [InlineData(Material)]
    public void Junk_categories_can_be_marked(int category)
    {
        Assert.True(ScrapRule.IsScrapOnly(Junk(Battery) with { Category = category }, isRecipeMaterial: false, scrapValue: 1));
    }

    [Theory]
    [InlineData(Food)]
    [InlineData(Weapon)]
    [InlineData(3)]
    public void Food_gear_and_other_categories_are_never_marked(int category)
    {
        Assert.False(ScrapRule.IsScrapOnly(Junk(Battery) with { Category = category }, isRecipeMaterial: false, scrapValue: 100));
    }

    [Fact]
    public void An_item_worth_more_in_trade_than_its_scrap_is_not_marked()
    {
        Assert.False(ScrapRule.IsScrapOnly(Junk(Battery, tradeValue: 5), isRecipeMaterial: false, scrapValue: 4));
        Assert.True(ScrapRule.IsScrapOnly(Junk(Battery, tradeValue: 4), isRecipeMaterial: false, scrapValue: 4));
    }

    [Fact]
    public void A_workbench_material_is_not_marked()
    {
        var tables = Tables(
            new[] { Junk(Battery), Plain(Wire, tradeValue: 2) },
            production: new[] { new ProductionRecipe(new[] { Gear, Battery, Battery }, IsShredOnly: false) },
            shredYields: BatteryShredsToTwoWire);

        Assert.Empty(ScrapRule.ScrapOnly(tables));
    }

    [Fact]
    public void A_named_cooking_ingredient_is_not_marked()
    {
        var tables = Tables(
            new[] { Junk(Battery), Plain(Wire, tradeValue: 2) },
            cooking: new[] { Recipe(900, normal: Bread, ingredients: new[] { Battery }) },
            shredYields: BatteryShredsToTwoWire);

        Assert.Empty(ScrapRule.ScrapOnly(tables));
    }

    [Fact]
    public void Being_the_input_of_its_own_shred_recipe_does_not_count_as_a_use()
    {
        var tables = Tables(
            new[] { Junk(Battery), Plain(Wire, tradeValue: 2) },
            production: new[] { new ProductionRecipe(new[] { Battery }, IsShredOnly: true) },
            shredYields: BatteryShredsToTwoWire);

        Assert.Equal(new[] { Battery }, ScrapRule.ScrapOnly(tables));
    }

    [Fact]
    public void Scrap_value_is_each_yields_trade_value_times_its_count()
    {
        var tradeValues = new Dictionary<int, int> { [Wire] = 3, [Gear] = 5 };

        Assert.Equal(11, ScrapRule.ScrapValue(new[] { new Yield(Wire, 2), new Yield(Gear, 1) }, tradeValues));
    }

    [Fact]
    public void Unknown_and_empty_yields_are_worth_nothing()
    {
        var tradeValues = new Dictionary<int, int> { [Wire] = 3 };

        Assert.Equal(0, ScrapRule.ScrapValue(new[] { new Yield(999, 4), new Yield(Wire, 0), new Yield(Wire, -2) }, tradeValues));
    }

    [Fact]
    public void An_item_that_shreds_into_nothing_is_only_marked_when_it_is_worth_nothing()
    {
        var tables = Tables(new[] { Junk(Battery, tradeValue: 1), Junk(Gear, tradeValue: 0) });

        Assert.Equal(new[] { Gear }, ScrapRule.ScrapOnly(tables));
    }

    [Fact]
    public void An_item_a_relief_request_asks_for_is_not_scrap()
    {
        var tables = Tables(new[] { Junk(Battery, tradeValue: 4), Plain(Wire, tradeValue: 2) }, shredYields: BatteryShredsToTwoWire) with
        {
            AidDemandItems = new HashSet<int> { Battery },
        };

        Assert.Empty(ScrapRule.ScrapOnly(tables));
    }
}
