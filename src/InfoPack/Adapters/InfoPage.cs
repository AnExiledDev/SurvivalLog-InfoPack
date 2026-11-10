using System;
using System.Globalization;
using ExiledToolkit.Web;
using InfoPack.Core;

namespace InfoPack.Adapters;

/// <summary>
/// The mod's half that lives in the game's web UI: page.js, run in the root document the game's pages sit in. It
/// adds the extra lines to the game's tooltips and panels. The web view can reload at any time and lose the script, so
/// Exiled's Toolkit checks for it every couple of seconds and the session puts it back when the page says it is missing.
/// </summary>
internal sealed class InfoPage
{
    private const string ScriptResource = "InfoPack.page.js";
    private const string Global = "window.__slinfopack";

    private readonly WebPage _page;

    public InfoPage(Action<string> log)
    {
        _page = new WebPage(typeof(InfoPage).Assembly, ScriptResource, Global, PageCommand.Prefix + "noscript", log);
    }

    /// <summary>True when it checked this frame, which reads the game's web view.</summary>
    public bool KeepInstalled(float now) => _page.KeepInstalled(now);

    public void Install() => _page.Install();

    /// <summary>Which pieces to draw; the page draws nothing until it has these.</summary>
    public void Features(string json) => _page.Call("setFeatures(" + json + ")");

    /// <summary>The facts that stay fixed for a loaded world.</summary>
    public void Facts(string json) => _page.Call("setFacts(" + json + ")");

    /// <summary>The home counts by config id.</summary>
    public void Counts(string json) => _page.Call("setCounts(" + json + ")");

    /// <summary>The answer to an instance lookup; 0 when the item was not found.</summary>
    public void Instance(long itemId, int configId) =>
        _page.Call("setInstance(" + JsText.Literal(itemId.ToString(CultureInfo.InvariantCulture)) + "," + configId.ToString(CultureInfo.InvariantCulture) + ")");

    /// <summary>What the craving and the relief requests want, and the foods never tried.</summary>
    public void Wanted(string json) => _page.Call("setWanted(" + json + ")");

    /// <summary>How fast the HUD stats drop and how long the food at home lasts.</summary>
    public void Stats(string json) => _page.Call("setStats(" + json + ")");

    /// <summary>Which of the detail and breakdown keys are held.</summary>
    public void Keys(string json) => _page.Call("setKeys(" + json + ")");

    /// <summary>The answer to a breakdown request: the home storages holding the item and how many each holds.</summary>
    public void Where(int configId, string rows) =>
        _page.Call("setWhere(" + JsText.Literal(configId.ToString(CultureInfo.InvariantCulture)) + "," + rows + ")");
}
