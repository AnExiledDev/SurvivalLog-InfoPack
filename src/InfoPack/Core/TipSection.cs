using System;
using System.Collections.Generic;

namespace InfoPack.Core;

/// <summary>One part of the Info Pack block in an item tooltip. Declared in the default order.</summary>
public enum TipSection
{
    Effects,
    Fresh,
    Notes,
    Home,
    UsedIn,
    Grow,
    Value,
    MadeFrom,
    Breaks,
    Fuel,
    FoundIn,
    About,
}

/// <summary>When a section is drawn: always, only while the detail key is held, or never.</summary>
public enum SectionMode
{
    Always,
    Detail,
    Off,
}

public static class TipSections
{
    /// <summary>Every section, in the default order.</summary>
    public static IReadOnlyList<TipSection> DefaultOrder { get; } = (TipSection[])Enum.GetValues(typeof(TipSection));

    /// <summary>The key the page script and the order setting name the section by.</summary>
    public static string Key(TipSection section) => section switch
    {
        TipSection.Effects => "effects",
        TipSection.Fresh => "fresh",
        TipSection.Notes => "notes",
        TipSection.Home => "home",
        TipSection.UsedIn => "usedin",
        TipSection.Grow => "grow",
        TipSection.Value => "value",
        TipSection.MadeFrom => "madefrom",
        TipSection.Breaks => "breaks",
        TipSection.Fuel => "fuel",
        TipSection.FoundIn => "foundin",
        TipSection.About => "about",
        _ => throw new ArgumentOutOfRangeException(nameof(section), section, null),
    };

    /// <summary>What a short tooltip shows; the rest waits for the detail key.</summary>
    public static SectionMode DefaultMode(TipSection section) => section switch
    {
        TipSection.Effects or TipSection.Fresh or TipSection.Notes or TipSection.Home or TipSection.UsedIn or TipSection.Grow => SectionMode.Always,
        TipSection.Value or TipSection.MadeFrom or TipSection.Breaks or TipSection.Fuel or TipSection.FoundIn or TipSection.About => SectionMode.Detail,
        _ => throw new ArgumentOutOfRangeException(nameof(section), section, null),
    };

    /// <summary>A section by its key, ignoring case and surrounding spaces.</summary>
    public static bool TryParse(string? key, out TipSection section)
    {
        var trimmed = key?.Trim() ?? "";

        foreach (var candidate in DefaultOrder)
        {
            if (!string.Equals(Key(candidate), trimmed, StringComparison.OrdinalIgnoreCase)) continue;

            section = candidate;

            return true;
        }

        section = default;

        return false;
    }
}

public static class SectionModes
{
    /// <summary>The choices a section's setting offers, as the config file writes them.</summary>
    public static readonly string[] Choices = { "Always", "Detail", "Off" };

    /// <summary>The setting's value as a mode; anything else gets the section's default.</summary>
    public static SectionMode Parse(string? value, TipSection section)
    {
        var trimmed = value?.Trim() ?? "";

        foreach (SectionMode mode in Enum.GetValues(typeof(SectionMode)))
        {
            if (string.Equals(mode.ToString(), trimmed, StringComparison.OrdinalIgnoreCase)) return mode;
        }

        return TipSections.DefaultMode(section);
    }

    /// <summary>The word the page script reads.</summary>
    public static string Word(SectionMode mode) => mode switch
    {
        SectionMode.Always => "always",
        SectionMode.Detail => "detail",
        SectionMode.Off => "off",
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
    };
}
