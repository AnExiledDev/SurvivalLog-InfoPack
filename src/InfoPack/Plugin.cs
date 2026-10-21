using System.IO;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using InfoPack.Adapters;
using InfoPack.Core;

namespace InfoPack;

[BepInPlugin(Guid, "Info Pack", Version)]
[BepInDependency(OptionalLive.ToolkitGuid, BepInDependency.DependencyFlags.SoftDependency)]
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.infopack";
    private const string Version = "0.4.1";

    public override void Load()
    {
        if (!OptionalLive.IsToolkitInstalled) Log.LogInfo(OptionalLive.MissingNote);

        var baseCounts = Config.Bind("Features", "BaseCounts", true,
            "When on (the default), item tooltips in the backpack, trade and cooking windows get an 'At home: N' line counting that item in your backpack, every storage at home and the workbench drawer. Turn it off if you also use the ItemTotals mod, so tooltips don't show two counts.\n" +
            "开启时（默认），背包、交易和烹饪界面的物品提示会多一行「家中共有：N」，显示这件物品在背包、家里所有储物家具和工作台抽屉里一共有多少。如果同时装了 ItemTotals 模组，请关闭此项，免得提示里出现两行数量。");
        var tradeStats = Config.Bind("Features", "TradeStats", true,
            "When on (the default), the tooltip of an item on the trader's shelf in the trade window shows what it does to satiety, mood, energy, health and life.\n" +
            "开启时（默认），交易界面里对方货架上物品的提示会显示它对饱腹、心态、精力、健康和生命的影响。");
        var alcoholQuality = Config.Bind("Features", "AlcoholQuality", true,
            "When on (the default), a brewed drink's tooltip shows its tier, like 'Brew quality: 2 of 3' (1 is the lowest). While you set up a brew, the brew panel also shows the batch's quality score and the score each better tier starts at.\n" +
            "开启时（默认），酿出的酒在提示里会显示档次，例如「酿造品质：第 2 档，共 3 档」（1 为最低档）。在酿酒桶准备开始酿造时，面板还会显示这一批的品质分数，以及更高档次各自从多少分起。");
        var scrapOnly = Config.Bind("Features", "ScrapOnly", true,
            "When on (the default), items only worth shredding get an 'Only useful as scrap' line in their tooltip, a 'Scrap' chip on their backpack slot and a 'Scrap' tag in the pickup list.\n" +
            "开启时（默认），只值得粉碎的物品会在提示里多一行「只适合回收」，在背包格子上显示「回收」角标，并在拾取列表里带上「回收」标记。");
        var cookingCodex = Config.Bind("Features", "CookingCodex", true,
            "When on (the default), hovering an ingredient in the cooking window adds its description and stats to the tooltip. Hovering a recipe does the same for the dish it makes.\n" +
            "开启时（默认），在烹饪界面悬停食材，提示里会加上它的描述和属性；悬停菜谱，则加上做出的菜肴的描述和属性。");
        var planningBadges = Config.Bind("Features", "PlanningBadges", true,
            "When on (the default), cards on the Survival Planning screen get a small badge on their picture. A timed card shows how long it lasts, like '3d' or '24h'. A card that takes effect once shows '1x'. Cards that last for good get no badge.\n" +
            "开启时（默认），生存规划界面的卡牌图片上会显示一个小角标。有时限的卡牌显示持续时间，例如「3d」或「24h」；只生效一次的卡牌显示「1x」。永久生效的卡牌没有角标。");
        var log = new InfoLog(Log, Path.Combine(Paths.BepInExRootPath, "info-pack.log"));

        log.Write($"===== Info Pack {Version} started");

        Features ReadFeatures() => new(baseCounts.Value, tradeStats.Value, alcoholQuality.Value, scrapOnly.Value, cookingCodex.Value, planningBadges.Value);

        var harmony = new Harmony(Guid);

        // Without the patch the page's messages never arrive, so a session would only re-post the install check forever.
        if (!PageMessages.Apply(harmony, log.Write)) return;

        var session = new InfoSession(ReadFeatures(), new WebPage(log.Write), new GameTableReader(log.Write), new GameTextReader(log.Write), log.Write);

        OptionalLive.Watch(() =>
        {
            var features = ReadFeatures();

            session.UseFeatures(features);
            log.Write(features.IsAnyOn ? $"features {features.ToJson()}" : "every feature is off, nothing is drawn");
        }, baseCounts, tradeStats, alcoholQuality, scrapOnly, cookingCodex, planningBadges);

        InfoTicker.Session = session;
        InfoTicker.Log = log.Write;
        AddComponent<InfoTicker>();
    }
}
