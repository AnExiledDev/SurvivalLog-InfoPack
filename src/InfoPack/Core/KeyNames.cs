namespace InfoPack.Core;

/// <summary>
/// The name a tooltip hint gives a key, from the key's name in Unity's KeyCode. Either side of Shift, Ctrl or Alt
/// works the same in game, so both sides get the one name the player knows them by.
/// </summary>
public static class KeyNames
{
    public static string Display(string keyCodeName) => keyCodeName switch
    {
        "LeftShift" or "RightShift" => "Shift",
        "LeftControl" or "RightControl" => "Ctrl",
        "LeftAlt" or "RightAlt" => "Alt",
        _ => keyCodeName,
    };
}
