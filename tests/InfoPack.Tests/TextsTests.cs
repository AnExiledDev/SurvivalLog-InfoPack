using System.Linq;
using System.Text.RegularExpressions;
using InfoPack.Core;
using Xunit;

namespace InfoPack.Tests;

public class TextsTests
{
    /// <summary>The phrase keys the tooltip page asks for, each with the English the page falls back to.</summary>
    public static TheoryData<string, string> PagePhrases() => new()
    {
        { "home", "At home: {0}" },
        { "brew", "Brew quality: {0} of {1}" },
        { "scrap", "Only useful as scrap" },
        { "scrapChip", "Scrap" },
        { "brewScore", "Quality {0}: {1} from {2}, {3} from {4}" },
        { "secEffects", "Effects" },
        { "secFresh", "Freshness" },
        { "secHome", "At home" },
        { "secUsedIn", "Used in" },
        { "secGrow", "Growing" },
        { "secValue", "Value" },
        { "secMadeFrom", "Made from" },
        { "secBreaks", "Breaks down into" },
        { "secFuel", "Fuel" },
        { "secFoundIn", "Found in" },
        { "secAbout", "About" },
        { "statSatiety", "Satiety" },
        { "statMood", "Mood" },
        { "statEnergy", "Energy" },
        { "statHealth", "Health" },
        { "statLife", "Life" },
        { "freshLeft", "{0} left" },
        { "freshBase", "Keeps {0} when fresh" },
        { "freshRots", "Rots into {0}" },
        { "spoiled", "Spoiled" },
        { "homeCount", "{0} at home" },
        { "where", "{0}: {1}" },
        { "whereNone", "None stored at home" },
        { "makeNow", "can make {0} now" },
        { "useBench", "Workbench" },
        { "useCook", "Cooking" },
        { "useBrew", "Brewed alone, quality {0}" },
        { "useBrewMix", "In a barrel: {0} satiety, {1} mood each; {2} per bottle" },
        { "useBrewMood", "In a barrel: {0} mood each, no satiety" },
        { "more", "+{0} more" },
        { "growTime", "Grows in {0}" },
        { "growHarvest", "Harvest within {0}" },
        { "growLight", "Light: {0}" },
        { "lightLow", "low" },
        { "lightMid", "medium" },
        { "lightHigh", "high" },
        { "growCold", "Cold resistant" },
        { "growYield", "Yields {0}" },
        { "growSeed", "{0} chance of a seed" },
        { "valueTrade", "Trade value {0}" },
        { "valueWeight", "Weight {0}" },
        { "valuePerKg", "{0} trade value per kg" },
        { "satietyPerKg", "{0} satiety per kg" },
        { "makeBench", "Workbench: {0}" },
        { "makeCook", "Cooking: {0}" },
        { "makePlant", "Harvest of {0}" },
        { "makeShred", "Shredding {0}" },
        { "fuelBurn", "Burn value {0}" },
        { "hintMore", "{0}: more" },
        { "hintWhere", "{0}: where" },
        { "wantWish", "You're craving this" },
        { "wantAid", "Relief request needs {0} more" },
        { "untried", "Never tried" },
        { "useRobot", "Robot module, level {0}" },
        { "useBait", "Trap bait: {0}" },
        { "rate", "-{0} per hour, empty in about {1}" },
        { "forecast", "Food at home lasts you about {0}" },
        { "dayShort", "{0}d" },
        { "hourShort", "{0}h" },
        { "minuteShort", "{0}m" },
        { "kg", "{0} kg" },
        { "g", "{0} g" },
    };

    [Theory]
    [MemberData(nameof(PagePhrases))]
    public void Every_word_the_tooltip_asks_for_is_sent_in_the_english_the_page_falls_back_to(string key, string english)
    {
        var phrase = Assert.Single(Texts.ByPageKey, pair => pair.Key == key).Value;

        Assert.Equal(english, phrase.En);
    }

    [Theory]
    [MemberData(nameof(PagePhrases))]
    public void Every_word_the_tooltip_asks_for_has_chinese_with_the_same_blanks_to_fill(string key, string english)
    {
        var phrase = Texts.ByPageKey.Single(pair => pair.Key == key).Value;

        Assert.False(string.IsNullOrWhiteSpace(phrase.Zh));
        Assert.NotEqual(english, phrase.Zh);
        Assert.Equal(Blanks(english), Blanks(phrase.Zh));
    }

    [Fact]
    public void No_two_phrases_share_a_key_so_the_page_never_shows_the_wrong_one()
    {
        var keys = Texts.ByPageKey.Select(pair => pair.Key).ToList();

        Assert.Equal(keys.Count, keys.Distinct().Count());
    }

    [Fact]
    public void The_breakdown_falls_back_to_names_in_both_languages_for_storages_the_game_cannot_name()
    {
        Assert.All(new[] { Texts.Inventory, Texts.WorkbenchDrawer, Texts.UnnamedStorage }, phrase =>
        {
            Assert.False(string.IsNullOrWhiteSpace(phrase.En));
            Assert.False(string.IsNullOrWhiteSpace(phrase.Zh));
        });

        Assert.Equal(Blanks(Texts.UnnamedStorage.En), Blanks(Texts.UnnamedStorage.Zh));
        Assert.Equal(new[] { "{0}" }, Blanks(Texts.UnnamedStorage.En));
    }

    private static string[] Blanks(string text) =>
        Regex.Matches(text, @"\{\d+\}").Select(match => match.Value).OrderBy(blank => blank).ToArray();
}
