using System.Collections.Generic;
using InfoPack.Core;
using Xunit;

namespace InfoPack.Tests;

public class SlicedJobTests
{
    private static IEnumerable<bool> Steps(List<int> ran, int count, int waitAfter = -1)
    {
        for (var i = 0; i < count; i++)
        {
            ran.Add(i);

            yield return i != waitAfter;
        }
    }

    [Fact]
    public void A_slice_with_time_to_spare_runs_the_whole_job()
    {
        var ran = new List<int>();
        var job = new SlicedJob(Steps(ran, 5));

        Assert.True(job.Advance(() => false));
        Assert.True(job.IsDone);
        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, ran);
    }

    [Fact]
    public void A_spent_slice_stops_after_one_step_and_the_job_still_finishes()
    {
        var ran = new List<int>();
        var job = new SlicedJob(Steps(ran, 3));
        var slices = 0;

        while (!job.Advance(() => true)) slices++;

        // One step a slice, and the slice that finds the end.
        Assert.Equal(3, slices);
        Assert.Equal(new[] { 0, 1, 2 }, ran);
    }

    [Fact]
    public void The_slice_is_checked_after_every_step()
    {
        var ran = new List<int>();
        var job = new SlicedJob(Steps(ran, 10));
        var checks = 0;

        Assert.False(job.Advance(() => ++checks >= 4));
        Assert.Equal(new[] { 0, 1, 2, 3 }, ran);
    }

    [Fact]
    public void A_step_that_waits_ends_the_slice_even_with_time_left()
    {
        var ran = new List<int>();
        var job = new SlicedJob(Steps(ran, 4, waitAfter: 1));

        Assert.False(job.Advance(() => false));
        Assert.Equal(new[] { 0, 1 }, ran);

        Assert.True(job.Advance(() => false));
        Assert.Equal(new[] { 0, 1, 2, 3 }, ran);
    }

    [Fact]
    public void A_finished_job_runs_nothing_more()
    {
        var ran = new List<int>();
        var job = new SlicedJob(Steps(ran, 2));

        job.Advance(() => false);

        Assert.True(job.Advance(() => false));
        Assert.Equal(2, ran.Count);
    }
}
