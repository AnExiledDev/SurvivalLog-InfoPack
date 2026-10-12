using InfoPack.Core;
using Xunit;

namespace InfoPack.Tests;

public class CountRefreshTests
{
    [Fact]
    public void The_first_count_is_taken_at_once()
    {
        Assert.True(new CountRefresh(2f).IsDue(0f));
    }

    [Fact]
    public void Counts_are_not_taken_again_until_the_interval_has_passed()
    {
        var refresh = new CountRefresh(2f);

        refresh.Counted(10f);

        Assert.False(refresh.IsDue(11.9f));
        Assert.True(refresh.IsDue(12f));
    }

    [Fact]
    public void Counts_the_page_already_holds_are_not_sent_again()
    {
        var refresh = new CountRefresh(2f);

        Assert.True(refresh.ShouldSend("{\"101\":3}"));
        Assert.False(refresh.ShouldSend("{\"101\":3}"));
        Assert.True(refresh.ShouldSend("{\"101\":4}"));
    }

    [Fact]
    public void A_reloaded_page_gets_the_counts_at_once_even_when_they_did_not_change()
    {
        var refresh = new CountRefresh(2f);

        refresh.Counted(10f);
        refresh.ShouldSend("{\"101\":3}");
        refresh.PageReloaded();

        Assert.True(refresh.IsDue(10.5f));
        Assert.True(refresh.ShouldSend("{\"101\":3}"));
    }
}
