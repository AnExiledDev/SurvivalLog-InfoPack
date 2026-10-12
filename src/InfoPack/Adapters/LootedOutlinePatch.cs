using System;
using GameCore.HotUpdate.Battle.Show;
using HarmonyLib;
using InfoPack.Core;

namespace InfoPack.Adapters;

/// <summary>
/// Right before the game colours a container's outline by its search state, swaps searched-and-empty for no state, so
/// the game's own code draws no search colour. Show side only: the search state the game keeps, the plain outline all
/// furniture gets under Alt and the hover outline are left as the game draws them. Static because a Harmony prefix has
/// nowhere else to look.
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
                prefix: new HarmonyMethod(typeof(LootedOutlinePatch), nameof(ShowLootedAsPlain)));
            log("hiding the search highlight on emptied containers");
        }
        catch (Exception ex)
        {
            log("could not hook the search highlight, emptied containers keep the game's colour: " + ex.Message);
        }
    }

    /// <summary>
    /// Only rewrites the argument and never calls into the game: a call back into the game from inside this hook
    /// crashes it, and that crash cannot be caught. Never throws into the game: on any failure the state goes through
    /// as the game sent it.
    /// </summary>
    private static void ShowLootedAsPlain(ref int state)
    {
        try
        {
            state = LootedOutline.ShownState(state);
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
