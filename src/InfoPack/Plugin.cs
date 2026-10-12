using System.IO;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using InfoPack.Adapters;
using InfoPack.Core;

namespace InfoPack;

[BepInPlugin(Guid, "Info Pack", Version)]
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.infopack";
    private const string Version = "0.2.1";

    public override void Load()
    {
        var features = new Features(
            Config.Bind("Features", "BaseCounts", true, "Show how many of an item you have at home (bag, home storages and the workbench drawer) in the backpack, trade and cooking tooltips.").Value,
            Config.Bind("Features", "TradeStats", true, "Show what an item does to satiety, mood, energy, health and life in the trade window's tooltip for the trader's items.").Value,
            Config.Bind("Features", "AlcoholQuality", true, "Show which brew tier an alcohol is in its tooltip, and the brew panel's quality score with the tier thresholds.").Value,
            Config.Bind("Features", "ScrapOnly", true, "Mark items that are only worth breaking down: a tooltip line, a Scrap chip on backpack slots and a tag in the pickup list.").Value,
            Config.Bind("Features", "CookingCodex", true, "Add the description and stats of an ingredient, or of a recipe's dish, to the cooking window's tooltips.").Value,
            Config.Bind("Features", "HideLootedHighlights", false, "New, off by default. On explore maps, stop the search colour from marking containers you have already searched and emptied. Unsearched containers and ones with items left keep their colour.").Value);
        var log = new InfoLog(Log, Path.Combine(Paths.BepInExRootPath, "info-pack.log"));

        log.Write($"===== Info Pack {Version} started, features {features.ToJson()}, hide looted highlights {features.HideLootedHighlights}");

        if (!features.IsAnyOn)
        {
            log.Write("every feature is off, nothing to do");

            return;
        }

        var harmony = new Harmony(Guid);

        if (features.HideLootedHighlights) LootedOutlinePatch.Apply(harmony, log.Write);

        if (!features.IsAnyPageOn) return;

        // Without the patch the page's messages never arrive, so a session would only re-post the install check forever.
        if (!PageMessages.Apply(harmony, log.Write)) return;

        InfoTicker.Session = new InfoSession(features, new WebPage(log.Write), new GameTableReader(log.Write), new GameTextReader(log.Write), log.Write);
        InfoTicker.Log = log.Write;
        AddComponent<InfoTicker>();
    }
}
