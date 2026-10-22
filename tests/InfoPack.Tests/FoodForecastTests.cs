using System.Collections.Generic;
using System.Text.Json;
using InfoPack.Core;
using Xunit;
using static InfoPack.Tests.Rows;

namespace InfoPack.Tests;

public class FoodForecastTests
{
    private const int Bread = 701;
    private const int Jerky = 702;
    private const int Water = 703;
    private const int Rope = 301;

    [Fact]
    public void Only_food_that_fills_you_up_counts()
    {
        var items = new[] { Edible(Bread, 20f), Edible(Jerky, 35f, -2f), Edible(Water, 0f, 5f), Plain(Rope, Material, 1, 10f) };

        Assert.Equal(new Dictionary<int, float> { [Bread] = 20f, [Jerky] = 35f }, FoodForecast.SatietyById(items));
    }

    [Fact]
    public void The_food_at_home_adds_up_by_count()
    {
        var satiety = new Dictionary<int, float> { [Bread] = 20f, [Jerky] = 35f };
        var counts = new Dictionary<int, int> { [Bread] = 3, [Jerky] = 2, [Rope] = 50 };

        Assert.Equal(130f, FoodForecast.Satiety(counts, satiety));
    }

    [Fact]
    public void Days_are_the_satiety_over_a_day_of_drain()
    {
        Assert.Equal(2.5f, FoodForecast.Days(150f, drainPerHour: 2.5f));
    }

    [Fact]
    public void There_is_no_forecast_until_the_drain_is_known()
    {
        Assert.Null(FoodForecast.Days(150f, null));
        Assert.Null(FoodForecast.Days(150f, 0f));
    }

    [Fact]
    public void The_page_gets_the_rates_and_the_days_when_there_is_a_forecast()
    {
        var root = JsonDocument.Parse(FoodForecast.StatsJson("{\"1\":[2,38]}", 2.46f)).RootElement;

        Assert.Equal(2, root.GetProperty("rates").GetProperty("1")[0].GetInt32());
        Assert.Equal(2.5, root.GetProperty("food").GetDouble());
        Assert.Equal("{\"rates\":{}}", FoodForecast.StatsJson("{}", null));
    }
}
