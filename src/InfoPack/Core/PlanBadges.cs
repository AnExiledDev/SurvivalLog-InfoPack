using System;
using System.Collections.Generic;
using System.Globalization;

namespace InfoPack.Core;

/// <summary>How long a Survival Planning card's effect lasts, read from its buff's BuffDuring (game seconds).</summary>
public abstract record PlanDuration
{
    private PlanDuration()
    {
    }

    /// <summary>The effect wears off after this many game seconds.</summary>
    public sealed record Timed(double Seconds) : PlanDuration;

    /// <summary>The effect happens once, when the card is bought: an airdrop, a heal. The game marks it BuffDuring 1.</summary>
    public sealed record OneOff : PlanDuration;

    /// <summary>The effect stays: BuffDuring -1, or a card with no buff at all.</summary>
    public sealed record Permanent : PlanDuration;
}

/// <summary>
/// The badge on a Survival Planning card's picture, so timed and one-off cards stand out from the ones that stay.
/// Permanent cards get no badge.
/// </summary>
public static class PlanBadges
{
    /// <summary>The Config_Talent Type of the cards the Survival Planning screen offers.</summary>
    public const int PlanningCardType = 2;

    /// <summary>The badge of a one-off card. The page script styles a badge with this text as one-off.</summary>
    public const string OneOffBadge = "1x";

    private const float OneOffDuring = 1f;
    private const double SecondsPerMinute = 60;
    private const double SecondsPerHour = 3600;
    private const double SecondsPerDay = 86400;

    /// <summary>
    /// The badge of every planning card that has one, by talent id. A card whose buff is missing from the buff table
    /// is taken as permanent, so it gets no badge rather than a wrong one.
    /// </summary>
    public static SortedDictionary<int, string> Map(IEnumerable<TalentRow> talents, IReadOnlyDictionary<int, float> buffDurations)
    {
        var badges = new SortedDictionary<int, string>();

        foreach (var talent in talents)
        {
            if (talent.Id <= 0 || talent.Type != PlanningCardType) continue;

            float? during = talent.BuffId > 0 && buffDurations.TryGetValue(talent.BuffId, out var found) ? found : null;
            var badge = Badge(Classify(during));

            if (badge is not null) badges[talent.Id] = badge;
        }

        return badges;
    }

    /// <param name="buffDuring">The buff's BuffDuring, or null when the card has no buff.</param>
    public static PlanDuration Classify(float? buffDuring) => buffDuring switch
    {
        float seconds when seconds > OneOffDuring && float.IsFinite(seconds) => new PlanDuration.Timed(seconds),
        OneOffDuring => new PlanDuration.OneOff(),
        _ => new PlanDuration.Permanent(),
    };

    /// <summary>"3d", "24h" or "90m" for a timed card, "1x" for a one-off card, null for a permanent one.</summary>
    public static string? Badge(PlanDuration duration) => duration switch
    {
        PlanDuration.Timed timed => Length(timed.Seconds),
        PlanDuration.OneOff => OneOffBadge,
        PlanDuration.Permanent => null,
        _ => throw new InvalidOperationException("unhandled plan duration " + duration),
    };

    /// <summary>
    /// Whole days from two days up ("3d"), else whole hours ("24h", "36h"), else minutes rounded up ("90m"). A single
    /// day reads as "24h", the way the game's own card text words it.
    /// </summary>
    public static string Length(double seconds)
    {
        if (seconds >= 2 * SecondsPerDay && seconds % SecondsPerDay == 0) return Whole(seconds / SecondsPerDay) + "d";
        if (seconds >= SecondsPerHour && seconds % SecondsPerHour == 0) return Whole(seconds / SecondsPerHour) + "h";

        return Whole(Math.Ceiling(seconds / SecondsPerMinute)) + "m";
    }

    private static string Whole(double value) => value.ToString("0", CultureInfo.InvariantCulture);
}
