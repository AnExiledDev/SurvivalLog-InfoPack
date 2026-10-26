using System;
using System.Collections.Generic;
using System.Globalization;

namespace InfoPack.Core;

/// <summary>
/// How long the food at home keeps the player fed: the satiety every food item at home gives, against the satiety the
/// player loses per day. Spoiling, cooking and other mouths are left out, so it is a rough number of days.
/// </summary>
public static class FoodForecast
{
    private const float HoursPerDay = 24f;
    private const int FoodCategory = 1;

    /// <summary>The satiety one of each food item gives (ValueDisplay1), for every food item that gives any; the first row of an id wins.</summary>
    public static Dictionary<int, float> SatietyById(IEnumerable<ItemRow> items)
    {
        var satiety = new Dictionary<int, float>();

        foreach (var item in items)
        {
            if (item.Category == FoodCategory && item.Stats.Count > 0 && item.Stats[0] > 0f) satiety.TryAdd(item.Id, item.Stats[0]);
        }

        return satiety;
    }

    /// <summary>
    /// The satiety all of these items give together; items that are not food count for nothing. Summed in double and
    /// turned back to float, as a float sum always was here, with no allocation, since it runs on every check.
    /// </summary>
    public static float Satiety(Dictionary<int, int> counts, IReadOnlyDictionary<int, float> satietyById)
    {
        var sum = 0d;

        foreach (var pair in counts)
        {
            sum += satietyById.TryGetValue(pair.Key, out var each) ? each * pair.Value : 0f;
        }

        return (float)sum;
    }

    /// <summary>The days that satiety lasts at the drain; null while the drain is not known or too small to measure.</summary>
    public static float? Days(float satiety, float? drainPerHour) =>
        drainPerHour is > StatDrain.MinimumDrop ? satiety / (drainPerHour.Value * HoursPerDay) : null;

    /// <summary>{"rates":{...},"food":days}, with "food" left out when there is no forecast.</summary>
    public static string StatsJson(string rates, float? days) =>
        "{\"rates\":" + rates + (days is { } value ? ",\"food\":" + Math.Round(value, 1).ToString(CultureInfo.InvariantCulture) : "") + "}";
}
