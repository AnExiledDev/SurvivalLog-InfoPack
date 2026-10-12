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

    IEnumerable<HeldStack> StacksIn(long storageId);
}

public static class HomeCounts
{
    /// <summary>
    /// How many of each item are at home, by config id. A storage named twice is counted once, and a stack the cache
    /// lists under a storage it no longer belongs to is skipped (the game would not let you take it from there either).
    /// </summary>
    public static SortedDictionary<int, int> Count(IHomeItems home)
    {
        var counts = new SortedDictionary<int, int>();
        var seen = new HashSet<long>();

        foreach (var storageId in home.HomeStorageIds())
        {
            if (!seen.Add(storageId)) continue;

            foreach (var stack in home.StacksIn(storageId))
            {
                if (stack.OwnerId != storageId || stack.ConfigId <= 0 || stack.Count <= 0) continue;

                counts.TryGetValue(stack.ConfigId, out var sum);
                counts[stack.ConfigId] = sum + stack.Count;
            }
        }

        return counts;
    }
}
