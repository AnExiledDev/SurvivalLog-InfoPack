namespace InfoPack.Core;

/// <summary>Whether the detail key and the breakdown key are held this frame.</summary>
public readonly record struct HeldKeys(bool Detail, bool Where)
{
    /// <summary>The argument of the page's setKeys call.</summary>
    public string ToJson() => "{\"detail\":" + Flag(Detail) + ",\"where\":" + Flag(Where) + "}";

    private static string Flag(bool isHeld) => isHeld ? "true" : "false";
}

/// <summary>
/// When the page hears about the keys. The page cannot see modifier keys itself, so it is told every time the pair
/// changes, and once more whenever its script is installed afresh, since a new script knows nothing.
/// </summary>
public sealed class KeySync
{
    private HeldKeys? _sent;

    /// <summary>True when the page does not hold this pair yet; from then on it is taken to hold it.</summary>
    public bool ShouldSend(HeldKeys keys)
    {
        if (_sent == keys) return false;

        _sent = keys;

        return true;
    }

    public void PageReloaded() => _sent = null;
}
