using System.Collections.Generic;
using System.Linq;
using InfoPack.Core;

namespace InfoPack.Tests;

/// <summary>A home whose storages and stacks a test lays out, read the way the game's item cache is.</summary>
internal sealed class FakeHome : IHomeItems
{
    private readonly List<long> _homeIds;
    private readonly List<(long ListedUnder, HeldStack Stack)> _stacks = new();

    public FakeHome(params long[] homeIds)
    {
        _homeIds = homeIds.ToList();
    }

    public FakeHome Holds(long storageId, int configId, int count)
    {
        _stacks.Add((storageId, new HeldStack(storageId, configId, count)));

        return this;
    }

    public FakeHome HoldsStale(long listedUnder, long ownerNow, int configId, int count)
    {
        _stacks.Add((listedUnder, new HeldStack(ownerNow, configId, count)));

        return this;
    }

    public IReadOnlyList<long> HomeStorageIds() => _homeIds;

    public IEnumerable<HeldStack> StacksIn(long storageId) =>
        _stacks.Where(entry => entry.ListedUnder == storageId).Select(entry => entry.Stack);
}
