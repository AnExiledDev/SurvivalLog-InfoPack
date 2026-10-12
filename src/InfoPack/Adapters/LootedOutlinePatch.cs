using System;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;
using InfoPack.Core;

namespace InfoPack.Adapters;

/// <summary>
/// Right after the game looks up a container's search state on an explore map, answers "no state" for a container
/// searched down to nothing, so the game never asks for its search colour. Hooks ExploreSearchStateManager.TryGetState,
/// whose only callers are the Alt-held and hover handlers; with no state they skip the colour and, on hover exit, the
/// game's own clear finds nothing to clear. Show side only: the plain outline all furniture gets under Alt and the
/// hover outline are left as the game draws them. Static because a Harmony postfix has nowhere else to look.
/// </summary>
/// <remarks>
/// 0.2.0 hooked AgentModel.Furniture_ShowSearchState instead and the game crashed on expeditions inside that hook. The
/// cause is not confirmed. The likeliest one is that the method opens with short jumps the native hook has to copy
/// elsewhere; TryGetState opens with plain register saves. Off by default until it is confirmed in game.
/// </remarks>
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
                AccessTools.Method(
                    typeof(ExploreSearchStateManager),
                    nameof(ExploreSearchStateManager.TryGetState),
                    new[] { typeof(Furniture), typeof(ExploreSearchStateManager.ExploreSearchState).MakeByRefType() }),
                postfix: new HarmonyMethod(typeof(LootedOutlinePatch), nameof(HideIfLooted)));
            log("hiding the search highlight on emptied containers");
        }
        catch (Exception ex)
        {
            log("could not hook the search highlight, emptied containers keep the game's colour: " + ex.Message);
        }
    }

    /// <summary>
    /// Only reads the state the game just found and makes no call into the game. Never throws into the game: on any
    /// failure the game's answer stands.
    /// </summary>
    private static void HideIfLooted(ref bool __result, ref ExploreSearchStateManager.ExploreSearchState state)
    {
        try
        {
            if (__result && !LootedOutline.ShowsSearchColour((int)state)) __result = false;
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
