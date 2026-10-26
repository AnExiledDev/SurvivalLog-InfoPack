namespace InfoPack.Core;

/// <summary>
/// When the home counts are taken again and when they are sent to the page. The counts are cached for a fixed
/// interval, so a number is never more than one interval old and the storages are not walked every frame. A new page
/// script asks for a count at once. The page only gets the counts when they differ from what it already holds; a
/// version stamp of what the message is built from says so before the message is built at all.
/// </summary>
public sealed class CountRefresh
{
    private readonly float _intervalSeconds;
    private float _nextCountAt;
    private string? _sent;
    private long? _sentStamp;

    public CountRefresh(float intervalSeconds)
    {
        _intervalSeconds = intervalSeconds;
    }

    public bool IsDue(float now) => now >= _nextCountAt;

    public void Counted(float now) => _nextCountAt = now + _intervalSeconds;

    /// <summary>True when the page already holds the message built from this stamp, so there is nothing to build.</summary>
    public bool Holds(long stamp) => _sentStamp == stamp;

    /// <summary>True when the page does not hold this message yet; from then on it is taken to hold it.</summary>
    public bool ShouldSend(string message)
    {
        if (message == _sent) return false;

        _sent = message;

        return true;
    }

    /// <summary>As <see cref="ShouldSend(string)"/>, and the message is remembered as built from this stamp.</summary>
    public bool ShouldSend(string message, long stamp)
    {
        _sentStamp = stamp;

        return ShouldSend(message);
    }

    /// <summary>What the message is built from changed in a way no stamp shows (a setting): build it at the next look.</summary>
    public void Forget() => _sentStamp = null;

    /// <summary>The page script was installed afresh and holds nothing: count now and send whatever comes out.</summary>
    public void PageReloaded()
    {
        _sent = null;
        _sentStamp = null;
        _nextCountAt = 0f;
    }
}
