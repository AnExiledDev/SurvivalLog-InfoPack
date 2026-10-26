using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace InfoPack.Core;

/// <summary>
/// The stat drop rates and the food forecast as the page shows them, rounded the way the message rounds them. Taken on
/// every check without building any text; the message is built only when a shown number changed, so a check whose
/// numbers come out as before costs no string and sends nothing.
/// </summary>
public sealed class StatsPrint
{
    private readonly int[] _stats;
    private readonly bool[] _isDropping;
    private readonly double[] _perHour;
    private readonly double[] _hoursLeft;
    private float? _days;
    private double? _shownDays;
    private bool _isKnown;

    public StatsPrint(IEnumerable<int> stats)
    {
        _stats = stats.Distinct().OrderBy(stat => stat).ToArray();
        _isDropping = new bool[_stats.Length];
        _perHour = new double[_stats.Length];
        _hoursLeft = new double[_stats.Length];
    }

    /// <summary>Takes the numbers again. True when the page does not hold them yet: one changed, or nothing was sent since <see cref="Forget"/>.</summary>
    /// <param name="withRates">False leaves every rate out, as when the rate lines are turned off.</param>
    /// <param name="days">The food forecast in days, or null for none.</param>
    public bool Update(StatDrain drain, bool withRates, float? days)
    {
        var isChanged = !_isKnown;

        for (var i = 0; i < _stats.Length; i++)
        {
            var perHour = 0f;
            var hoursLeft = 0f;
            var isDropping = withRates && drain.Rate(_stats[i], out perHour, out hoursLeft);
            var shownRate = isDropping ? StatDrain.Rounded(perHour) : 0d;
            var shownLeft = isDropping ? StatDrain.Rounded(hoursLeft) : 0d;

            if (isDropping != _isDropping[i] || shownRate != _perHour[i] || shownLeft != _hoursLeft[i]) isChanged = true;

            _isDropping[i] = isDropping;
            _perHour[i] = shownRate;
            _hoursLeft[i] = shownLeft;
        }

        double? shownDays = days is { } value ? Math.Round(value, 1) : null;

        if (shownDays != _shownDays) isChanged = true;

        _days = days;
        _shownDays = shownDays;
        _isKnown = true;

        return isChanged;
    }

    /// <summary>{"rates":{stat:[dropPerHour,hoursLeft]},"food":days}, the same text the drain and the forecast give.</summary>
    public string Json()
    {
        var rates = new StringBuilder("{");

        for (var i = 0; i < _stats.Length; i++)
        {
            if (!_isDropping[i]) continue;

            if (rates.Length > 1) rates.Append(',');

            rates.Append('"').Append(_stats[i].ToString(CultureInfo.InvariantCulture)).Append("\":[")
                .Append(_perHour[i].ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(_hoursLeft[i].ToString(CultureInfo.InvariantCulture)).Append(']');
        }

        return FoodForecast.StatsJson(rates.Append('}').ToString(), _days);
    }

    /// <summary>The page holds nothing (a reload) or a setting changed what it shows: the next update counts as a change.</summary>
    public void Forget() => _isKnown = false;
}
