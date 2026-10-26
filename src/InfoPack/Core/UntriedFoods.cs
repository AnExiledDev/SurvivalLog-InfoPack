using System;
using System.Collections.Generic;

namespace InfoPack.Core;

/// <summary>
/// The food codex items the player has never eaten, in ascending id order. Eating a food unlocks its codex entry and
/// an entry never locks again, so the foods are only asked about again when the codex's count of unlocked foods moved,
/// and a food found eaten is dropped for good. A check where the count stands still asks the game nothing.
/// </summary>
public sealed class UntriedFoods
{
    private readonly List<int> _untried = new();
    private IReadOnlyList<int>? _foods;
    private int? _unlockedCount;

    /// <summary>Goes up whenever the list changes, so a caller can tell nothing new was found.</summary>
    public long Version { get; private set; }

    /// <summary>The foods not eaten yet, ascending. Read only.</summary>
    public IReadOnlyList<int> Items => _untried;

    /// <summary>
    /// Brings the list up to date. A new food list (new facts) starts over from it. isUnlocked is the game's own
    /// question, asked only for foods still on the list and only when unlockedCount moved.
    /// </summary>
    public void Check(IReadOnlyList<int> foods, int unlockedCount, Func<int, bool> isUnlocked)
    {
        if (!ReferenceEquals(foods, _foods)) StartOver(foods);

        if (_unlockedCount == unlockedCount) return;

        for (var i = _untried.Count - 1; i >= 0; i--)
        {
            if (!isUnlocked(_untried[i])) continue;

            _untried.RemoveAt(i);
            Version++;
        }

        // Only a check that got through every food counts, so one that threw is done again in full.
        _unlockedCount = unlockedCount;
    }

    /// <summary>A new world: what was found belongs to the old one.</summary>
    public void Reset()
    {
        _foods = null;
        _unlockedCount = null;
        _untried.Clear();
        Version++;
    }

    private void StartOver(IReadOnlyList<int> foods)
    {
        var sorted = new SortedSet<int>();

        for (var i = 0; i < foods.Count; i++) sorted.Add(foods[i]);

        _foods = foods;
        _unlockedCount = null;
        _untried.Clear();
        _untried.AddRange(sorted);
        Version++;
    }
}
