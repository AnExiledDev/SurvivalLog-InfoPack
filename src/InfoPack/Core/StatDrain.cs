using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace InfoPack.Core;

/// <summary>
/// How fast the player's stats drop, measured against the game clock. Readings are kept for a stretch of game time and
/// only the drops between them count, so eating or sleeping in that stretch does not hide the drain. A clock that runs
/// backwards (a save was loaded) starts the measurement over, and a paused clock adds nothing.
/// </summary>
public sealed class StatDrain
{
    private const float SecondsPerHour = 3600f;
    private const int MaxReadings = 4000;

    private readonly int _windowSeconds;
    private readonly int _minimumSeconds;
    private readonly List<Reading> _readings = new();

    /// <param name="windowSeconds">Game seconds of readings kept.</param>
    /// <param name="minimumSeconds">Game seconds the readings must span before a rate is given.</param>
    public StatDrain(int windowSeconds, int minimumSeconds)
    {
        _windowSeconds = windowSeconds;
        _minimumSeconds = minimumSeconds;
    }

    /// <summary>The player's stats at a moment of game time, by the game's AttrName number.</summary>
    public void Add(int gameSeconds, IReadOnlyDictionary<int, float> values)
    {
        if (_readings.Count > 0)
        {
            var last = _readings[_readings.Count - 1].Seconds;

            if (gameSeconds < last) _readings.Clear();
            else if (gameSeconds == last) return;
        }

        _readings.Add(new Reading(gameSeconds, new Dictionary<int, float>(values.ToDictionary(pair => pair.Key, pair => pair.Value))));
        DropOld(gameSeconds);
    }

    /// <summary>A new world was loaded: nothing measured so far belongs to it.</summary>
    public void Reset() => _readings.Clear();

    /// <summary>The stat's drop per game hour, 0 or more; null until the readings span long enough.</summary>
    public float? PerHour(int stat)
    {
        if (_readings.Count < 2) return null;

        var span = _readings[_readings.Count - 1].Seconds - _readings[0].Seconds;

        if (span < _minimumSeconds) return null;

        var dropped = 0f;

        for (var i = 1; i < _readings.Count; i++)
        {
            if (_readings[i - 1].Values.TryGetValue(stat, out var before) && _readings[i].Values.TryGetValue(stat, out var after) && after < before)
            {
                dropped += before - after;
            }
        }

        return dropped / (span / SecondsPerHour);
    }

    /// <summary>The stat's latest value, or null before any reading.</summary>
    public float? Latest(int stat) =>
        _readings.Count > 0 && _readings[_readings.Count - 1].Values.TryGetValue(stat, out var value) ? value : null;

    /// <summary>
    /// {stat:[dropPerHour,hoursLeft]} for every stat with a measured drop. hoursLeft is how long the latest value lasts
    /// at that rate. A stat that is not dropping is left out.
    /// </summary>
    public string ToJson(IEnumerable<int> stats)
    {
        var members = new List<string>();

        foreach (var stat in stats.Distinct().OrderBy(stat => stat))
        {
            var perHour = PerHour(stat);
            var latest = Latest(stat);

            if (perHour is not > MinimumDrop || latest is null) continue;

            members.Add("\"" + stat.ToString(CultureInfo.InvariantCulture) + "\":[" + Number(perHour.Value) + "," + Number(Math.Max(0f, latest.Value) / perHour.Value) + "]");
        }

        return "{" + string.Join(",", members) + "}";
    }

    /// <summary>Below this per hour a stat reads as steady: rounding noise, not a drain.</summary>
    public const float MinimumDrop = 0.05f;

    private void DropOld(int now)
    {
        // The newest reading before the window stays, so the window is always fully covered.
        while (_readings.Count > 2 && (now - _readings[1].Seconds >= _windowSeconds || _readings.Count > MaxReadings)) _readings.RemoveAt(0);
    }

    private static string Number(float value) => Math.Round(value, 2).ToString(CultureInfo.InvariantCulture);

    private readonly record struct Reading(int Seconds, Dictionary<int, float> Values);
}
