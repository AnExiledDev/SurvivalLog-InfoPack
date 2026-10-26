using System;
using System.Text.RegularExpressions;
using GameCore.HotUpdate;
using InfoPack.Core;

namespace InfoPack.Adapters;

/// <summary>
/// The game's localised text: item names and descriptions, and the names of furniture and places. The page draws with
/// textContent, so any rich-text tags the game's strings carry are stripped rather than shown. Rows are looked up in
/// the tables themselves: the game's Get_Config_X getters log an error for every id they lack.
/// </summary>
internal sealed class GameTextReader : IGameText
{
    private static readonly Regex RichTextTag = new("<[^>]*>", RegexOptions.Compiled);

    private ConfigManager? _config;

    /// <summary>The language the game shows, or null before its settings exist. A plain field read on the game's side.</summary>
    public string? LanguageKey() => GameSingleton.Existing<SettingManager>()?.GetLanguageKey();

    public bool IsChinese()
    {
        try
        {
            return GameSingleton.Existing<SettingManager>()?.IsChinese() == true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public string? LocalText(string key)
    {
        var config = Config();

        return config == null ? null : Clean(Translate(config, key));
    }

    public string? Description(int itemId)
    {
        var config = Config();
        var table = config?._Config_Item_Dict;

        if (config == null || table == null || !table.TryGetValue(itemId, out var item) || item == null) return null;

        return Clean(Translate(config, item.ItemDes1)) ?? Clean(item.ItemDes1_Local);
    }

    public string? ItemName(int itemId)
    {
        var config = Config();
        var table = config?._Config_Item_Dict;

        if (config == null || table == null || !table.TryGetValue(itemId, out var item) || item == null) return null;

        return Clean(Translate(config, item.ItemName)) ?? Clean(item.ItemName_Local);
    }

    public string? FurnitureName(int furnitureId)
    {
        var config = Config();
        var table = config?._Config_Furniture_Dict;

        if (config == null || table == null || !table.TryGetValue(furnitureId, out var furniture) || furniture == null) return null;

        return Clean(Translate(config, furniture.Name)) ?? Clean(furniture.Name_Local);
    }

    public string? PlaceName(int placeId)
    {
        var config = Config();
        var table = config?._Config_MapPoint_Dict;

        if (config == null || table == null || !table.TryGetValue(placeId, out var place) || place == null) return null;

        return Clean(Translate(config, place.Name)) ?? Clean(place.Name_Local);
    }

    /// <summary>
    /// The game's config manager, kept while the game has the same one: the facts look up thousands of names in a row,
    /// and each fresh read of the singleton makes a new interop object.
    /// </summary>
    private ConfigManager? Config()
    {
        var pointer = GameSingleton.Pointer<ConfigManager>();

        if (pointer == IntPtr.Zero) return _config = null;

        if (_config == null || _config.Pointer != pointer) _config = GameSingleton.Existing<ConfigManager>();

        return _config;
    }

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
