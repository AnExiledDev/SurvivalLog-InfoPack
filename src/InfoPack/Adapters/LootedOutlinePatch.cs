using System;
using GameCore.HotUpdate.Battle.Show;
using HarmonyLib;
using InfoPack.Core;

namespace InfoPack.Adapters;

/// <summary>
/// Right after the game colours a container's outline by its search state, runs the game's own clear when the
/// container is searched and empty. Show side only: the search state, the plain outline all furniture gets under Alt
/// and the hover outline are left as the game draws them. Static because a Harmony postfix has nowhere else to look.
/// </summary>
internal static class LootedOutlinePatch
{
    private static Action<string> _log = _ => { };
    private static bool _hasReportedFailure;

    public static void Apply(Harmony harmony, Action<string> log)
    {
        _log = log;

        try
        {
            harmony.Patch(
                AccessTools.Method(typeof(AgentModel), nameof(AgentModel.Furniture_ShowSearchState), new[] { typeof(int) }),
                postfix: new HarmonyMethod(typeof(LootedOutlinePatch), nameof(ClearIfLooted)));
            log("hiding the search highlight on emptied containers");
        }
        catch (Exception ex)
        {
            log("could not hook the search highlight, emptied containers keep the game's colour: " + ex.Message);
        }
    }

    /// <summary>Never throws into the game: on any failure the outline stays as the game set it.</summary>
    private static void ClearIfLooted(AgentModel __instance, int state)
    {
        try
        {
            if (LootedOutline.ShouldClear(state)) __instance.Furniture_HideSearchState();
        }
        catch (Exception ex)
        {
            ReportOnce(ex);
        }
    }

    /// <summary>The outline handlers run on the main thread only, so a plain flag is enough.</summary>
    private static void ReportOnce(Exception ex)
    {
        if (_hasReportedFailure) return;

        _hasReportedFailure = true;
        _log("clearing an emptied container's search highlight failed, the game's colour stays (reported once): " + ex);
    }
}
