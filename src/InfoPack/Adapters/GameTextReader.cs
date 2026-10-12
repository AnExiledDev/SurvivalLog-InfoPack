using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using GameCore.HotUpdate;
using InfoPack.Core;

namespace InfoPack.Adapters;

/// <summary>
/// The game's localised text: the stat wording its item tips use and the item descriptions. The page draws with
/// textContent, so any rich-text tags the game's strings carry are stripped rather than shown.
/// </summary>
internal sealed class GameTextReader : IGameText
{
    private static readonly Regex RichTextTag = new("<[^>]*>", RegexOptions.Compiled);

    private readonly Action<string> _log;
    private bool _hasReportedFormatFailure;

    public GameTextReader(Action<string> log)
    {
        _log = log;
    }

    /// <summary>
    /// The game's GameKey.Text_SR_UI_ItemTips_8 to _17, in gain and loss pairs. A key the game cannot give falls back
    /// to English, so a stat is never shown without a name.
    /// </summary>
    public IReadOnlyList<StatFormat> StatFormats()
    {
        try
        {
            return new[]
            {
                Pair(() => GameKey.Text_SR_UI_ItemTips_8, () => GameKey.Text_SR_UI_ItemTips_9, "Satiety"),
                Pair(() => GameKey.Text_SR_UI_ItemTips_10, () => GameKey.Text_SR_UI_ItemTips_11, "Mood"),
                Pair(() => GameKey.Text_SR_UI_ItemTips_12, () => GameKey.Text_SR_UI_ItemTips_13, "Energy"),
                Pair(() => GameKey.Text_SR_UI_ItemTips_14, () => GameKey.Text_SR_UI_ItemTips_15, "Health"),
                Pair(() => GameKey.Text_SR_UI_ItemTips_16, () => GameKey.Text_SR_UI_ItemTips_17, "Life"),
            };
        }
        catch (Exception ex)
        {
            ReportFormatFailure(ex);

            return new[] { English("Satiety"), English("Mood"), English("Energy"), English("Health"), English("Life") };
        }
    }

    /// <summary>The language the game shows, or null before its settings exist. A plain field read on the game's side.</summary>
    public string? LanguageKey() => GameSingleton.Existing<SettingManager>()?.GetLanguageKey();

    public string? Description(int itemId)
    {
        var config = GameSingleton.Existing<ConfigManager>();
        var item = config?.Get_Config_Item(itemId);

        if (config == null || item == null) return null;

        return Clean(Translate(config, item.ItemDes1)) ?? Clean(item.ItemDes1_Local);
    }

    private StatFormat Pair(Func<string> gain, Func<string> loss, string name)
    {
        var english = English(name);

        return new StatFormat(Format(gain) ?? english.Gain, Format(loss) ?? english.Loss);
    }

    /// <summary>The game fills these with string.Format, so wording without a "{0}" is a missing key, not a translation.</summary>
    private string? Format(Func<string> read)
    {
        try
        {
            var text = Clean(read());

            return text != null && text.Contains("{0}", StringComparison.Ordinal) ? text : null;
        }
        catch (Exception ex)
        {
            ReportFormatFailure(ex);

            return null;
        }
    }

    private void ReportFormatFailure(Exception ex)
    {
        if (!_hasReportedFormatFailure) _log("could not read the game's stat wording, English is used (reported once): " + ex.Message);

        _hasReportedFormatFailure = true;
    }

    private static StatFormat English(string name) => new(name + " +{0}", name + " {0}");

    /// <summary>The game's lookup hands the key back when it has no text for it.</summary>
    private static string? Translate(ConfigManager config, string? key)
    {
        if (string.IsNullOrEmpty(key)) return null;

        var text = config.GetLocalTxt(key);

        return string.IsNullOrEmpty(text) || text == key ? null : text;
    }

    private static string? Clean(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var plain = RichTextTag.Replace(text, "").Trim();

        return plain.Length == 0 ? null : plain;
    }
}
