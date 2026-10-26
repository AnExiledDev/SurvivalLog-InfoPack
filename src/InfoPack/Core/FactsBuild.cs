using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InfoPack.Core;

/// <summary>What the live pieces are matched against, taken from the tables when the facts are built.</summary>
public sealed record LiveLists(IReadOnlyList<int> ItemIds, IReadOnlyList<int> FoodCodex, IReadOnlyDictionary<int, float> Satiety)
{
    public static LiveLists From(IReadOnlyList<ItemRow> items) => new(
        items.Select(item => item.Id).Distinct().ToList(),
        items.Where(item => item.InFoodCodex).Select(item => item.Id).Distinct().ToList(),
        FoodForecast.SatietyById(items));
}

/// <summary>The facts message and the lists the live pieces use, built together.</summary>
public sealed record BuiltFacts(string Json, LiveLists Lists);

/// <summary>
/// Building the facts without holding up a frame. The game text is copied a lookup per step on the game's thread (only
/// what the copy does not hold yet), then the facts, which touch nothing of the game's, are built on a worker while
/// the steps wait a slice at a time. A failure on the worker is thrown from the step that finds it.
/// </summary>
public static class FactsBuild
{
    /// <summary>Hands work to a worker thread.</summary>
    public static readonly Func<Func<BuiltFacts>, Task<BuiltFacts>> OnWorker = work => Task.Run(work);

    /// <param name="run">Where the build runs: <see cref="OnWorker"/> in the game.</param>
    /// <param name="done">Gets the facts at the last step, on the thread that runs the steps.</param>
    public static IEnumerable<bool> Steps(
        GameTables tables,
        TextSnapshot text,
        IGameText source,
        Features features,
        Func<Func<BuiltFacts>, Task<BuiltFacts>> run,
        Action<BuiltFacts> done)
    {
        if (!text.IsFilled)
        {
            foreach (var step in text.Fill(tables, source)) yield return step;
        }

        var build = run(() => new BuiltFacts(PageFacts.Build(tables, text, features), LiveLists.From(tables.Items)));

        while (!build.IsCompleted) yield return false;

        done(build.GetAwaiter().GetResult());
    }
}
