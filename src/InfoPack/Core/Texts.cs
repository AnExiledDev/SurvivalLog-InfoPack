using System.Collections.Generic;

namespace InfoPack.Core;

/// <summary>One piece of on-screen text in the two languages the game ships.</summary>
public readonly record struct Phrase(string En, string Zh)
{
    public string Pick(bool isChinese) => isChinese ? Zh : En;
}

/// <summary>
/// The words the page script puts on screen. {0}, {1} and so on are filled in by the page: the home count, the brew
/// tier and how many tiers there are, the brew panel's score, tier names and where each tier starts, or a tooltip
/// line's number, time or name. The Chinese uses the game's own words where it has them.
/// </summary>
public static class Texts
{
    public static readonly Phrase Home = new("At home: {0}", "家中共有：{0}");

    public static readonly Phrase BrewQuality = new("Brew quality: {0} of {1}", "酿造品质：第 {0} 档，共 {1} 档");

    public static readonly Phrase ScrapOnly = new("Only useful as scrap", "只适合回收");

    public static readonly Phrase ScrapChip = new("Scrap", "回收");

    public static readonly Phrase BrewScore = new("Quality {0}: {1} from {2}, {3} from {4}", "品质 {0}：{2} 起为{1}，{4} 起为{3}");

    /// <summary>The breakdown's name for the player's bag when the game has none to give; the game's own word.</summary>
    public static readonly Phrase Inventory = new("Inventory", "背包");

    /// <summary>The breakdown's name for the workbench drawer when the game has none to give.</summary>
    public static readonly Phrase WorkbenchDrawer = new("Workbench Drawer", "工作台抽屉");

    /// <summary>The breakdown's name for a storage the game has no name for; {0} is its number.</summary>
    public static readonly Phrase UnnamedStorage = new("Storage {0}", "储物家具 {0}");

    /// <summary>Each phrase under the key the page script reads it by.</summary>
    public static IReadOnlyList<KeyValuePair<string, Phrase>> ByPageKey { get; } = new[]
    {
        Key("home", Home),
        Key("brew", BrewQuality),
        Key("scrap", ScrapOnly),
        Key("scrapChip", ScrapChip),
        Key("brewScore", BrewScore),

        Key("secEffects", new("Effects", "效果")),
        Key("secFresh", new("Freshness", "保质期")),
        Key("secHome", new("At home", "家中")),
        Key("secUsedIn", new("Used in", "用途")),
        Key("secGrow", new("Growing", "种植")),
        Key("secValue", new("Value", "价值")),
        Key("secMadeFrom", new("Made from", "获取方式")),
        Key("secBreaks", new("Breaks down into", "可分解为")),
        Key("secFuel", new("Fuel", "燃料")),
        Key("secFoundIn", new("Found in", "搜寻来源")),
        Key("secAbout", new("About", "描述")),

        Key("statSatiety", new("Satiety", "饱腹")),
        Key("statMood", new("Mood", "心态")),
        Key("statEnergy", new("Energy", "精力")),
        Key("statHealth", new("Health", "健康")),
        Key("statLife", new("Life", "生命")),

        Key("freshLeft", new("{0} left", "剩余 {0}")),
        Key("freshBase", new("Keeps {0} when fresh", "新鲜时保质期 {0}")),
        Key("freshRots", new("Rots into {0}", "腐烂成 {0}")),
        Key("spoiled", new("Spoiled", "已腐败")),

        Key("homeCount", new("{0} at home", "家中共有 {0}")),
        Key("where", new("{0}: {1}", "{0}：{1}")),
        Key("whereNone", new("None stored at home", "家中没有存放")),

        Key("makeNow", new("can make {0} now", "现可制作 {0}")),
        Key("useBench", new("Workbench", "工作台")),
        Key("useCook", new("Cooking", "烹饪")),
        Key("useBrew", new("Can be brewed", "可酿造")),
        Key("more", new("+{0} more", "另有 {0} 项")),

        Key("growTime", new("Grows in {0}", "生长时间 {0}")),
        Key("growHarvest", new("Harvest within {0}", "收获窗口 {0}")),
        Key("growLight", new("Light: {0}", "光照需求：{0}")),
        Key("lightLow", new("low", "低")),
        Key("lightMid", new("medium", "中")),
        Key("lightHigh", new("high", "高")),
        Key("growCold", new("Cold resistant", "抗寒")),
        Key("growYield", new("Yields {0}", "产出 {0}")),
        Key("growSeed", new("{0} chance of a seed", "{0} 概率得到种子")),

        Key("valueTrade", new("Trade value {0}", "交易价值 {0}")),
        Key("valueWeight", new("Weight {0}", "重量 {0}")),
        Key("valuePerKg", new("{0} trade value per kg", "每千克交易价值 {0}")),
        Key("satietyPerKg", new("{0} satiety per kg", "每千克饱腹 {0}")),

        Key("makeBench", new("Workbench: {0}", "工作台：{0}")),
        Key("makeCook", new("Cooking: {0}", "烹饪：{0}")),
        Key("makePlant", new("Harvest of {0}", "收获自 {0}")),
        Key("makeShred", new("Shredding {0}", "粉碎 {0}")),

        Key("fuelBurn", new("Burn value {0}", "燃烧值 {0}")),
        Key("hintMore", new("{0}: more", "{0}：更多")),
        Key("hintWhere", new("{0}: where", "{0}：存放位置")),

        Key("wantWish", new("You're craving this", "正想吃这个")),
        Key("wantAid", new("Relief request needs {0} more", "救助委托还需 {0} 个")),
        Key("untried", new("Never tried", "从没尝过")),
        Key("useRobot", new("Robot module, level {0}", "机器人模块，{0} 级")),
        Key("useBait", new("Trap bait: {0}", "陷阱诱饵：{0}")),
        Key("rate", new("-{0} per hour, empty in about {1}", "每小时 -{0}，约 {1} 后耗尽")),
        Key("forecast", new("Food at home lasts you about {0}", "家中食物约够你吃 {0}")),

        Key("dayShort", new("{0}d", "{0}天")),
        Key("hourShort", new("{0}h", "{0}小时")),
        Key("minuteShort", new("{0}m", "{0}分钟")),
        Key("kg", new("{0} kg", "{0} 千克")),
        Key("g", new("{0} g", "{0} 克")),
    };

    private static KeyValuePair<string, Phrase> Key(string pageKey, Phrase phrase) => new(pageKey, phrase);
}
