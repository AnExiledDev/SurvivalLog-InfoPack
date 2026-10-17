using System.Collections.Generic;

namespace InfoPack.Core;

/// <summary>One piece of on-screen text in the two languages the game ships.</summary>
public readonly record struct Phrase(string En, string Zh)
{
    public string Pick(bool isChinese) => isChinese ? Zh : En;
}

/// <summary>
/// The words the page script puts on screen. {0}, {1} and so on are filled in by the page: the home count, the brew
/// tier and how many tiers there are, or the brew panel's score, tier names and where each tier starts.
/// </summary>
public static class Texts
{
    public static readonly Phrase Home = new("At home: {0}", "家中共有：{0}");

    public static readonly Phrase BrewQuality = new("Brew quality: {0} of {1}", "酿造品质：第 {0} 档，共 {1} 档");

    public static readonly Phrase ScrapOnly = new("Only useful as scrap", "只适合回收");

    public static readonly Phrase ScrapChip = new("Scrap", "回收");

    public static readonly Phrase BrewScore = new("Quality {0}: {1} from {2}, {3} from {4}", "品质 {0}：{2} 起为{1}，{4} 起为{3}");

    /// <summary>Each phrase under the key the page script reads it by.</summary>
    public static IReadOnlyList<KeyValuePair<string, Phrase>> ByPageKey { get; } = new[]
    {
        new KeyValuePair<string, Phrase>("home", Home),
        new KeyValuePair<string, Phrase>("brew", BrewQuality),
        new KeyValuePair<string, Phrase>("scrap", ScrapOnly),
        new KeyValuePair<string, Phrase>("scrapChip", ScrapChip),
        new KeyValuePair<string, Phrase>("brewScore", BrewScore),
    };
}
