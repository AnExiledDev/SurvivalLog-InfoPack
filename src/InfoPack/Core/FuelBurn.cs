using System;
using System.Collections.Generic;
using System.Linq;

namespace InfoPack.Core;

/// <summary>
/// How fast the game's fuel burners use fuel, as the tables have them when the world loads. Each list holds the
/// FuelRate of every burner of that kind that takes fuel; a saving is the fuel-saving talent's ratio (0 for none).
/// </summary>
/// <param name="Cooking">Config_FurnitureCook.FuelRate: burn value used per game second of cooking.</param>
/// <param name="CookingSaving">Buff/AE_FuelSavingMultiplier, which the cooking pot takes off its rate.</param>
/// <param name="Heaters">Config_FurnitureElectrical.FuelRate of the fuel heaters: burn value per 600 game seconds.</param>
/// <param name="Generators">Config_FurnitureElectrical.FuelRate of the fuel generators: burn value per 600 game seconds.</param>
/// <param name="GeneratorSaving">Buff/AE_GeneratorFuelSaving, which a generator takes off its rate.</param>
public sealed record FuelRates(
    IReadOnlyList<float> Cooking,
    float CookingSaving,
    IReadOnlyList<float> Heaters,
    IReadOnlyList<float> Generators,
    float GeneratorSaving)
{
    public static readonly FuelRates None = new(Array.Empty<float>(), 0f, Array.Empty<float>(), Array.Empty<float>(), 0f);
}

/// <summary>The shortest and longest an item burns across the burners of one kind, in game seconds.</summary>
public sealed record BurnSpan(int Shortest, int Longest);

/// <summary>How long one item burns in each kind of burner; null for a kind the game has no fuel burner of.</summary>
public sealed record BurnTimes(BurnSpan? Cooking, BurnSpan? Heater, BurnSpan? Generator);

/// <summary>How long a fuel item lasts, worked out the way the burners use it up.</summary>
public static class FuelBurn
{
    /// <summary>A heater or generator's FuelRate is the burn value it uses every 600 game seconds (GeneratorFuelComponent.GetRemainingGameSeconds).</summary>
    public const int ElectricalSeconds = 600;

    /// <summary>
    /// A cooking pot uses its rate every game second it cooks (CookingComponent.UpdateFuelFromProgress); a heater uses
    /// its rate every 600 game seconds; a generator the same, less its talent saving (PowerManager.ConsumeGeneratorFuel).
    /// Null when the item gives no fuel or no burner takes any.
    /// </summary>
    public static BurnTimes? Of(int burnValue, FuelRates rates)
    {
        if (burnValue <= 0) return null;

        var times = new BurnTimes(
            Span(burnValue, rates.Cooking, rates.CookingSaving, 1),
            Span(burnValue, rates.Heaters, 0f, ElectricalSeconds),
            Span(burnValue, rates.Generators, rates.GeneratorSaving, ElectricalSeconds));

        return times.Cooking is null && times.Heater is null && times.Generator is null ? null : times;
    }

    private static BurnSpan? Span(int burnValue, IReadOnlyList<float> rates, float saving, int secondsPerRate)
    {
        var seconds = rates
            .Select(rate => rate * (1f - saving))
            .Where(rate => rate > 0f)
            .Select(rate => (int)Math.Round(burnValue / (double)rate * secondsPerRate, MidpointRounding.AwayFromZero))
            .ToList();

        return seconds.Count == 0 ? null : new BurnSpan(seconds.Min(), seconds.Max());
    }
}
