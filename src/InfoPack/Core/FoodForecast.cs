using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace InfoPack.Core;

/// <summary>
/// How long the food at home keeps the player fed: the satiety every food item at home gives, against the satiety the
/// player loses per day. Spoiling, cooking and other mouths are left out, so it is a rough number of days.
/// </summary>
public static class FoodForecast
{
    private const float HoursPerDay = 24f;
    private const int FoodCategory = 1;

    /// <summary>The satiety one of each food item gives (ValueDisplay1), for every food item that gives any.</summary>
    public static Dictionary<int, float> SatietyById(IEnumerable<ItemRow> items) =>
        items.Where(item => item.Category == FoodCategory && item.Stats.Count > 0 && item.Stats[0] > 0f)
            .GroupBy(item => item.Id)
            .ToDictionary(group => group.Key, group => group.First().Stats[0]);

    /// <summary>The satiety all of these items give together; items that are not food count for nothing.</summary>
    public static float Satiety(IReadOnlyDictionary<int, int> counts, IReadOnlyDictionary<int, float> satietyById) =>
        counts.Sum(pair => satietyById.TryGetValue(pair.Key, out var each) ? each * pair.Value : 0f);

    /// <summary>The days that satiety lasts at the drain; null while the drain is not known or too small to measure.</summary>
    public static float? Days(float satiety, float? drainPerHour) =>
        drainPerHour is > StatDrain.MinimumDrop ? satiety / (drainPerHour.Value * HoursPerDay) : null;

    /// <summary>{"rates":{...},"food":days}, with "food" left out when there is no forecast.</summary>
    public static string StatsJson(string rates, float? days) =>
        "{\"rates\":" + rates + (days is { } value ? ",\"food\":" + Math.Round(value, 1).ToString(CultureInfo.InvariantCulture) : "") + "}";
}
