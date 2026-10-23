using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using InfoPack.Core;
using UnityEngine;

namespace InfoPack.Adapters;

/// <summary>
/// The "Tooltip" settings: when each section of the Info Pack block shows, their order, the two keys, keeping tooltips
/// on screen and the spoil badges. The order and the per-section choices are tagged as advanced for the Mod Manager.
/// </summary>
internal sealed class TooltipConfig
{
    private const string Section = "Tooltip";
    private const string Advanced = "ModManager.Advanced";

    private readonly ConfigEntry<string> _order;
    private readonly IReadOnlyDictionary<TipSection, ConfigEntry<string>> _modes;

    public TooltipConfig(ConfigFile config)
    {
        _modes = TipSections.DefaultOrder.ToDictionary(section => section, section => BindMode(config, section));
        _order = config.Bind(Section, "Order", TipOrder.DefaultText, new ConfigDescription(
            "The order of the sections in the Info Pack part of item tooltips, as section names separated by commas. Names are the ones these settings use: Effects, Fresh, Notes, Home, UsedIn, Grow, Value, MadeFrom, Breaks, Fuel, FoundIn, About. A section left out keeps its usual place after the ones named; an unknown or repeated name is ignored.\n" +
            "物品提示中 Info Pack 部分各栏的顺序，用英文逗号分隔栏目名称。名称与这些设置的名称相同：Effects、Fresh、Notes、Home、UsedIn、Grow、Value、MadeFrom、Breaks、Fuel、FoundIn、About。没写到的栏目按原有顺序排在后面；不认识或重复的名称会被忽略。",
            null, Advanced));
        DetailKey = config.Bind(Section, "DetailKey", KeyCode.LeftShift,
            "Hold this key over an item to see the sections set to Detail, and every row of long lists. The tooltip shows a hint naming the key when there is more to see. Either side of Shift, Ctrl or Alt works.\n" +
            "在物品上按住此键，可查看设为 Detail 的栏目以及长列表的全部内容。有更多内容时，提示底部会显示此键的名称。Shift、Ctrl、Alt 左右两侧都可以。");
        WhereKey = config.Bind(Section, "WhereKey", KeyCode.LeftControl,
            "Hold this key over an item to see which storages at home hold it and how many each one has, under the home count. Either side of Shift, Ctrl or Alt works.\n" +
            "在物品上按住此键，会在家中数量下方列出家里哪些储物家具放着这件物品，以及各有多少。Shift、Ctrl、Alt 左右两侧都可以。");
        KeepOnScreen = config.Bind(Section, "KeepOnScreen", true,
            "When on (the default), a tooltip that would run past an edge of the window, such as a tall one near the bottom, is moved back inside it.\n" +
            "开启时（默认），会超出窗口边缘的物品提示（例如靠近底部的长提示）会被移回窗口内。");
        SpoilBadges = config.Bind(Section, "SpoilBadges", true,
            "When on (the default), food in backpack and storage slots gets a small coloured corner when it is about to spoil: red when it spoils within a day, amber when it spoils within three days and half its shelf life is gone.\n" +
            "开启时（默认），背包和储物格子里快要腐败的食物会显示一个彩色小角标：一天内腐败为红色，三天内腐败且保质期已过半为橙色。");
    }

    public ConfigEntry<KeyCode> DetailKey { get; }

    public ConfigEntry<KeyCode> WhereKey { get; }

    public ConfigEntry<bool> KeepOnScreen { get; }

    public ConfigEntry<bool> SpoilBadges { get; }

    /// <summary>Every tooltip setting, for the live-change watch.</summary>
    public ConfigEntryBase[] Entries() =>
        _modes.Values.Cast<ConfigEntryBase>().Concat(new ConfigEntryBase[] { _order, DetailKey, WhereKey, KeepOnScreen, SpoilBadges }).ToArray();

    public TipLayout Layout() =>
        TipLayout.From(
            TipOrder.Parse(_order.Value),
            section => SectionModes.Parse(_modes[section].Value, section),
            TipKeys.Display(DetailKey.Value),
            TipKeys.Display(WhereKey.Value));

    private static ConfigEntry<string> BindMode(ConfigFile config, TipSection section)
    {
        var shown = Shows(section);
        var defaultMode = TipSections.DefaultMode(section).ToString();

        return config.Bind(Section, section.ToString(), defaultMode, new ConfigDescription(
            $"When item tooltips show {shown.En}. Always: every time. Detail: only while DetailKey is held. Off: never. {defaultMode} by default.\n" +
            $"物品提示何时显示{shown.Zh}。Always：一直显示。Detail：仅在按住 DetailKey 时显示。Off：不显示。默认为 {defaultMode}。",
            new AcceptableValueList<string>(SectionModes.Choices), Advanced));
    }

    /// <summary>What a section holds, for its setting's description.</summary>
    private static Phrase Shows(TipSection section) => section switch
    {
        TipSection.Effects => new("the Effects section: what the item does to satiety, mood, energy, health and life",
            "「效果」一栏：物品对饱腹、心态、精力、健康和生命的影响"),
        TipSection.Fresh => new("the Freshness section: time left before the item spoils, how long it keeps when fresh and what it rots into",
            "「保质期」一栏：距离腐败的剩余时间、新鲜时的保质期，以及会腐烂成什么"),
        TipSection.Notes => new("Info Pack's short notes, like 'Only useful as scrap' and a brewed drink's quality",
            "Info Pack 的简短提示，例如「只适合回收」和酒的酿造品质"),
        TipSection.Home => new("the At home section: how many you have at home, with the per-storage list while WhereKey is held. Needs BaseCounts on",
            "「家中」一栏：家里一共有多少，按住 WhereKey 时列出各储物家具里的数量。需要开启 BaseCounts"),
        TipSection.UsedIn => new("the Used in section: workbench recipes, dishes and brewing that use the item",
            "「用途」一栏：用到这件物品的工作台配方、菜肴和酿造"),
        TipSection.Grow => new("the Growing section for seeds: grow time, harvest window, light need, cold resistance, yield and seed chance",
            "种子的「种植」一栏：生长时间、收获窗口、光照需求、抗寒能力、产出和得到种子的概率"),
        TipSection.Value => new("the Value section: trade value, weight, trade value per kg and satiety per kg",
            "「价值」一栏：交易价值、重量、每千克交易价值和每千克饱腹"),
        TipSection.MadeFrom => new("the Made from section: the recipes, plants and shredding that give the item",
            "「获取方式」一栏：能得到这件物品的配方、作物和粉碎"),
        TipSection.Breaks => new("the Breaks down into section: what shredding, cutting or rotting turns the item into",
            "「可分解为」一栏：粉碎、切割或腐烂后会变成什么"),
        TipSection.Fuel => new("the Fuel section: the item's burn value as fuel, and how long one burns in a cooking pot, a heater and a fuel generator",
            "「燃料」一栏：作为燃料的燃烧值，以及一个在灶具、暖炉和燃料发电机里能烧多久"),
        TipSection.FoundIn => new("the Found in section: the furniture the item can turn up in while searching, and where that furniture stands",
            "「搜寻来源」一栏：搜寻时可能找到这件物品的家具，以及这些家具所在的地点"),
        TipSection.About => new("the About section: the item's description",
            "「描述」一栏：物品的描述"),
        _ => new(section.ToString(), section.ToString()),
    };
}
