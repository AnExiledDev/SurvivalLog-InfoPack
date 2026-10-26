using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
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
    private const string Version = "0.8.15";

    public override void Load()
    {
        if (!OptionalLive.IsToolkitInstalled) Log.LogInfo(OptionalLive.MissingNote);

        var baseCounts = Config.Bind("Features", "BaseCounts", true,
            "When on (the default), item tooltips in the backpack, storage, trade and cooking windows get an 'At home' section with how many of that item are in your backpack, every storage at home and the workbench drawer. Turn it off if you also use the ItemTotals mod, so tooltips don't show two counts.\n" +
            "开启时（默认），背包、储物、交易和烹饪界面的物品提示会多一栏「家中」，显示这件物品在背包、家里所有储物家具和工作台抽屉里一共有多少。如果同时装了 ItemTotals 模组，请关闭此项，免得提示里出现两行数量。");
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
            "When on (the default), hovering an ingredient in the cooking window adds its description and stats to the tooltip. Hovering a recipe does the same for the dish it makes. Turning it off leaves the cooking window's tooltips as the game draws them, with no Info Pack block.\n" +
            "开启时（默认），在烹饪界面悬停食材，提示里会加上它的描述和属性；悬停菜谱，则加上做出的菜肴的描述和属性。关闭后，烹饪界面的提示恢复游戏原样，不再显示 Info Pack 的内容。");
        var planningBadges = Config.Bind("Features", "PlanningBadges", true,
            "When on (the default), cards on the Survival Planning screen get a small badge on their picture. A timed card shows how long it lasts, like '3d' or '24h'. A card that takes effect once shows '1x'. Cards that last for good get no badge.\n" +
            "开启时（默认），生存规划界面的卡牌图片上会显示一个小角标。有时限的卡牌显示持续时间，例如「3d」或「24h」；只生效一次的卡牌显示「1x」。永久生效的卡牌没有角标。");
        var wanted = Config.Bind("Features", "Wanted", true,
            "When on (the default), an item's tooltip says when it is the food you are craving, or how many more of it a relief request still needs. Items a relief request needs also get a star on their backpack slot.\n" +
            "开启时（默认），如果物品正是你想吃的食物，或者救助委托还需要它，提示里会写明，并显示还差几个。救助委托需要的物品在背包格子上还会显示一颗星。");
        var neverTried = Config.Bind("Features", "NeverTried", true,
            "When on (the default), food and drink you have never had get a 'Never tried' line in their tooltip.\n" +
            "开启时（默认），从没吃过的食物和饮品，提示里会多一行「从没尝过」。");
        var statRates = Config.Bind("Features", "StatRates", true,
            "When on (the default), hovering a stat bar on the HUD shows how much it drops per game hour and about how long until it is empty. The rate shows after about half a game hour of play.\n" +
            "开启时（默认），悬停界面上的状态条，会显示它每个游戏小时下降多少，以及大约多久后耗尽。游戏内过去约半小时后才会显示。");
        var foodForecast = Config.Bind("Features", "FoodForecast", true,
            "When on (the default), hovering the satiety bar on the HUD shows about how many days the food at home lasts you. It counts the satiety of every food item at home against how fast you get hungry, and leaves out spoiling and cooking.\n" +
            "开启时（默认），悬停界面上的饱腹条，会显示家中食物大约够你吃几天。按家中所有食物的饱腹值和你饿的速度估算，不计腐坏和烹饪。");
        var featureSettings = new ConfigEntryBase[] { baseCounts, tradeStats, alcoholQuality, scrapOnly, cookingCodex, planningBadges, wanted, neverTried, statRates, foodForecast };
        var tooltip = new TooltipConfig(Config);
        var log = new InfoLog(Log, Path.Combine(Paths.BepInExRootPath, "info-pack.log"));

        log.Write($"===== Info Pack {Version} started");

        Features ReadFeatures() => new(baseCounts.Value, tradeStats.Value, alcoholQuality.Value, scrapOnly.Value, cookingCodex.Value, planningBadges.Value)
        {
            SpoilBadges = tooltip.SpoilBadges.Value,
            KeepOnScreen = tooltip.KeepOnScreen.Value,
            Tip = tooltip.Layout(),
            Wanted = wanted.Value,
            NeverTried = neverTried.Value,
            StatRates = statRates.Value,
            FoodForecast = foodForecast.Value,
        };

        var harmony = new Harmony(Guid);

        // Without the patch the page's messages never arrive, so a session would only re-post the install check forever.
        if (!PageMessages.Apply(harmony, log.Write)) return;

        var keys = new TipKeys(tooltip.DetailKey.Value, tooltip.WhereKey.Value);
        var session = new InfoSession(ReadFeatures(), keys, new WebPage(log.Write), new GameTableReader(log.Write), new GameTextReader(), log.Write);

        OptionalLive.Watch(() =>
        {
            var features = ReadFeatures();

            keys.Use(tooltip.DetailKey.Value, tooltip.WhereKey.Value);
            session.UseFeatures(features);
            log.Write(features.IsAnyOn ? $"features {features.ToJson()}" : "every feature is off, nothing is drawn");
        }, featureSettings.Concat(tooltip.Entries()).ToArray());

        InfoTicker.Session = session;
        InfoTicker.Log = log.Write;
        AddComponent<InfoTicker>();
    }
}
