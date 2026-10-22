using System;
using System.Collections.Generic;
using System.Linq;

namespace InfoPack.Core;

/// <summary>
/// How the Info Pack block in a tooltip is laid out: the section order, when each section shows, and the names of the
/// two keys the page puts in its hints. Built only through <see cref="From"/>, so the order always holds every section
/// once and every section has a mode.
/// </summary>
public sealed class TipLayout
{
    public const string DefaultDetailKey = "Shift";
    public const string DefaultWhereKey = "Ctrl";

    private readonly SectionMode[] _modes;

    private TipLayout(IReadOnlyList<TipSection> order, SectionMode[] modes, string detailKey, string whereKey)
    {
        Order = order;
        _modes = modes;
        DetailKey = detailKey;
        WhereKey = whereKey;
    }

    public static TipLayout Default { get; } = From(TipSections.DefaultOrder, TipSections.DefaultMode, DefaultDetailKey, DefaultWhereKey);

    public IReadOnlyList<TipSection> Order { get; }

    /// <summary>The detail key's name as the "Shift: more" hint shows it.</summary>
    public string DetailKey { get; }

    /// <summary>The breakdown key's name as the "Ctrl: where" hint shows it.</summary>
    public string WhereKey { get; }

    /// <summary>A missing section in <paramref name="order"/> keeps its default place after the ones given.</summary>
    public static TipLayout From(IEnumerable<TipSection> order, Func<TipSection, SectionMode> modeOf, string detailKey, string whereKey) =>
        new(TipOrder.Complete(order), TipSections.DefaultOrder.Select(modeOf).ToArray(), detailKey, whereKey);

    public SectionMode Mode(TipSection section) => _modes[(int)section];

    /// <summary>With every section off the tooltip block draws nothing.</summary>
    public bool ShowsAnySection => _modes.Any(mode => mode != SectionMode.Off);

    /// <summary>The "tip" member of the features the page reads.</summary>
    public string ToJson() =>
        "{\"order\":[" + string.Join(",", Order.Select(section => JsText.Literal(TipSections.Key(section)))) + "]"
        + ",\"modes\":{" + string.Join(",", TipSections.DefaultOrder.Select(section => JsText.Literal(TipSections.Key(section)) + ":" + JsText.Literal(SectionModes.Word(Mode(section))))) + "}"
        + ",\"detailKey\":" + JsText.Literal(DetailKey)
        + ",\"whereKey\":" + JsText.Literal(WhereKey)
        + "}";
}
