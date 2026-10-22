using InfoPack.Core;
using UnityEngine;

namespace InfoPack.Adapters;

/// <summary>
/// The detail and breakdown keys, read from Unity's input each frame. The web view drops modifier keys from the mouse
/// events it passes on, so the page cannot tell Shift is down; the plugin reads it here and tells the page.
/// </summary>
internal sealed class TipKeys
{
    private KeyCode _detail;
    private KeyCode _where;

    public TipKeys(KeyCode detail, KeyCode where)
    {
        Use(detail, where);
    }

    public void Use(KeyCode detail, KeyCode where)
    {
        _detail = detail;
        _where = where;
    }

    public HeldKeys Read() => new(IsHeld(_detail), IsHeld(_where));

    /// <summary>The name the tooltip hint shows for a key: "Shift" for either Shift key, and so on.</summary>
    public static string Display(KeyCode key) => KeyNames.Display(key.ToString());

    /// <summary>Either side of Shift, Ctrl or Alt counts, as the hint names them without a side.</summary>
    private static bool IsHeld(KeyCode key) => key != KeyCode.None && (Input.GetKey(key) || Input.GetKey(OtherSide(key)));

    private static KeyCode OtherSide(KeyCode key) => key switch
    {
        KeyCode.LeftShift => KeyCode.RightShift,
        KeyCode.RightShift => KeyCode.LeftShift,
        KeyCode.LeftControl => KeyCode.RightControl,
        KeyCode.RightControl => KeyCode.LeftControl,
        KeyCode.LeftAlt => KeyCode.RightAlt,
        KeyCode.RightAlt => KeyCode.LeftAlt,
        _ => key,
    };
}
