using System.Collections.Generic;

namespace InfoPack.Core;

/// <summary>One stack as the game's item cache lists it under a storage.</summary>
/// <param name="OwnerId">The owner the stack itself names. The cache can keep stale entries that point elsewhere.</param>
public readonly record struct HeldStack(long OwnerId, int ConfigId, int Count);

/// <summary>The port the count reads the game through: which storages are home, and what each one holds.</summary>
public interface IHomeItems
{
    /// <summary>The bag, the home storages and the workbench drawer, in any order; may name one twice.</summary>
    IReadOnlyList<long> HomeStorageIds();

    /// <summary>Adds the stacks listed under the storage to the list, which the caller clears and reuses.</summary>
    void StacksIn(long storageId, List<HeldStack> into);
}

/// <summary>
/// How many of each item are at home, by config id, taken again on every check into the same buffers. A storage named
/// twice is counted once, and a stack the cache lists under a storage it no longer belongs to is skipped (the game would
/// not let you take it from there either). The version only moves when the counts come out different, so an unchanged
/// home builds no message.
/// </summary>
public sealed class HomeTally
{
    private readonly HashSet<long> _seen = new();
    private readonly List<HeldStack> _stacks = new();
    private Dictionary<int, int> _counts = new();
    private Dictionary<int, int> _before = new();

    /// <summary>Goes up each time a count differs from the one before it.</summary>
    public long Version { get; private set; }

    /// <summary>The latest counts. Read only: the next count reuses it.</summary>
    public Dictionary<int, int> Counts => _counts;

    /// <summary>True when the counts differ from the ones taken before.</summary>
    public bool Count(IHomeItems home)
    {
        var last = _counts;

        _counts = _before;
        _before = last;
        _counts.Clear();
        _seen.Clear();

        var storageIds = home.HomeStorageIds();

        for (var s = 0; s < storageIds.Count; s++)
        {
            var storageId = storageIds[s];

            if (!_seen.Add(storageId)) continue;

            _stacks.Clear();
            home.StacksIn(storageId, _stacks);

            for (var i = 0; i < _stacks.Count; i++)
            {
                var stack = _stacks[i];

                if (stack.OwnerId != storageId || stack.ConfigId <= 0 || stack.Count <= 0) continue;

                _counts.TryGetValue(stack.ConfigId, out var sum);
                _counts[stack.ConfigId] = sum + stack.Count;
            }
        }

        _stacks.Clear();

        if (Same(_counts, _before)) return false;

        Version++;

        return true;
    }

    private static bool Same(Dictionary<int, int> now, Dictionary<int, int> before)
    {
        if (now.Count != before.Count) return false;

        foreach (var pair in now)
        {
            if (!before.TryGetValue(pair.Key, out var count) || count != pair.Value) return false;
        }

        return true;
    }
}
