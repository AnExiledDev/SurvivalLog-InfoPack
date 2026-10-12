namespace InfoPack.Core;

/// <summary>
/// When the home counts are taken again and when they are sent to the page. The counts are cached for a fixed
/// interval, so a number is never more than one interval old and the storages are not walked every frame. A new page
/// script asks for a count at once. The page only gets the counts when they differ from what it already holds.
/// </summary>
public sealed class CountRefresh
{
    private readonly float _intervalSeconds;
    private float _nextCountAt;
    private string? _sent;

    public CountRefresh(float intervalSeconds)
    {
        _intervalSeconds = intervalSeconds;
    }

    public bool IsDue(float now) => now >= _nextCountAt;

    public void Counted(float now) => _nextCountAt = now + _intervalSeconds;

    /// <summary>True when the page does not hold these counts yet; from then on it is taken to hold them.</summary>
    public bool ShouldSend(string counts)
    {
        if (counts == _sent) return false;

        _sent = counts;

        return true;
    }

    /// <summary>The page script was installed afresh and holds nothing: count now and send whatever comes out.</summary>
    public void PageReloaded()
    {
        _sent = null;
        _nextCountAt = 0f;
    }
}
