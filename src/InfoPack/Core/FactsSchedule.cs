namespace InfoPack.Core;

/// <summary>What the facts were built from: a loaded world, and the language its descriptions are in.</summary>
/// <param name="World">Identifies the loaded world; a new save gives a new one.</param>
/// <param name="Language">The game's language key, or null when it could not be read.</param>
public readonly record struct FactsKey(long World, string? Language);

/// <summary>
/// When the facts are looked at again and when they are built anew. The world and language are only checked every
/// interval, so an idle frame reads nothing from the game. The facts are rebuilt when either has changed since the
/// last build, so a save loaded in another language gets its descriptions in that language.
/// </summary>
public sealed class FactsSchedule
{
    private readonly float _intervalSeconds;
    private float _nextCheckAt;
    private FactsKey? _built;

    public FactsSchedule(float intervalSeconds)
    {
        _intervalSeconds = intervalSeconds;
    }

    public bool IsDue(float now) => now >= _nextCheckAt;

    public void Checked(float now) => _nextCheckAt = now + _intervalSeconds;

    public bool NeedsBuild(FactsKey current) => _built != current;

    /// <summary>Called before building, so a build that throws is not retried every check.</summary>
    public void Building(FactsKey key) => _built = key;

    /// <summary>A page reload often comes with a new save or language: look at once rather than at the next interval.</summary>
    public void PageReloaded() => _nextCheckAt = 0f;
}
