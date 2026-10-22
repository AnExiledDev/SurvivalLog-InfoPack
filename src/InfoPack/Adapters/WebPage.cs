using System;
using System.Globalization;
using System.IO;
using GameCore.HotUpdate.ReduxUI;
using InfoPack.Core;

namespace InfoPack.Adapters;

/// <summary>
/// The mod's half that lives in the game's web UI: page.js, run in the root document the game's own pages sit in. It
/// adds the extra lines to the game's tooltips and panels. The web view can reload at any time and lose the script, so
/// it is checked for every couple of seconds and put back when the page says it is missing.
/// </summary>
internal sealed class WebPage
{
    private const float CheckSeconds = 2f;
    private const string ScriptResource = "InfoPack.page.js";
    private const string Handle = "window.__slinfopack";
    private const string NoScriptReply = "window.vuplex&&window.vuplex.postMessage('" + PageCommand.Prefix + "noscript')";

    private readonly Action<string> _log;
    private string? _script;
    private float _nextCheckAt;
    private bool _hasReportedRunFailure;

    public WebPage(Action<string> log)
    {
        _log = log;
    }

    public void KeepInstalled(float now)
    {
        if (now < _nextCheckAt) return;

        _nextCheckAt = now + CheckSeconds;
        Run(Handle + "||" + NoScriptReply);
    }

    public void Install()
    {
        if (Run(Script())) _log("page script sent to the web UI");
    }

    /// <summary>Which pieces to draw; the page draws nothing until it has these.</summary>
    public void Features(string json) => Call("setFeatures(" + json + ")");

    /// <summary>The facts that stay fixed for a loaded world.</summary>
    public void Facts(string json) => Call("setFacts(" + json + ")");

    /// <summary>The home counts by config id.</summary>
    public void Counts(string json) => Call("setCounts(" + json + ")");

    /// <summary>The answer to an instance lookup; 0 when the item was not found.</summary>
    public void Instance(long itemId, int configId) =>
        Call("setInstance(" + JsText.Literal(itemId.ToString(CultureInfo.InvariantCulture)) + "," + configId.ToString(CultureInfo.InvariantCulture) + ")");

    /// <summary>What the craving and the relief requests want, and the foods never tried.</summary>
    public void Wanted(string json) => Call("setWanted(" + json + ")");

    /// <summary>How fast the HUD stats drop and how long the food at home lasts.</summary>
    public void Stats(string json) => Call("setStats(" + json + ")");

    /// <summary>Which of the detail and breakdown keys are held.</summary>
    public void Keys(string json) => Call("setKeys(" + json + ")");

    /// <summary>The answer to a breakdown request: the home storages holding the item and how many each holds.</summary>
    public void Where(int configId, string rows) =>
        Call("setWhere(" + JsText.Literal(configId.ToString(CultureInfo.InvariantCulture)) + "," + rows + ")");

    /// <summary>Does nothing when the script is not there; the next check puts it back and it asks again.</summary>
    private void Call(string call) => Run(Handle + "&&" + Handle + "." + call);

    private bool Run(string javaScript)
    {
        var webView = GameSingleton.Existing<ReduxUISystem>()?.GetWebUILayer()?.canvasWebViewPrefab?.WebView;

        if (webView == null) return false;

        try
        {
            webView.ExecuteJavaScript(javaScript, null);

            return true;
        }
        catch (Exception ex)
        {
            if (!_hasReportedRunFailure) _log("could not run script in the web UI (reported once): " + ex.Message);

            _hasReportedRunFailure = true;

            return false;
        }
    }

    private string Script()
    {
        if (_script != null) return _script;

        using var stream = typeof(WebPage).Assembly.GetManifestResourceStream(ScriptResource) ?? throw new InvalidOperationException(ScriptResource + " is not embedded");
        using var reader = new StreamReader(stream);

        return _script = reader.ReadToEnd();
    }
}
