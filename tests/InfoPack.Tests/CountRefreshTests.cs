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

    [Fact]
    public void A_stamp_the_page_already_holds_needs_no_message_built()
    {
        var refresh = new CountRefresh(2f);

        Assert.False(refresh.Holds(1));

        refresh.ShouldSend("{\"101\":3}", 1);

        Assert.True(refresh.Holds(1));
        Assert.False(refresh.Holds(2));
    }

    [Fact]
    public void A_new_stamp_with_the_same_text_is_held_but_not_sent_again()
    {
        var refresh = new CountRefresh(2f);

        refresh.ShouldSend("{\"101\":3}", 1);

        Assert.False(refresh.ShouldSend("{\"101\":3}", 2));
        Assert.True(refresh.Holds(2));
    }

    [Fact]
    public void A_setting_change_builds_the_message_again_even_for_the_same_stamp()
    {
        var refresh = new CountRefresh(2f);

        refresh.ShouldSend("{\"101\":3}", 1);
        refresh.Forget();

        Assert.False(refresh.Holds(1));
        Assert.True(refresh.ShouldSend("{\"101\":3,\"x\":1}", 1));
    }

    [Fact]
    public void A_reloaded_page_holds_no_stamp_so_the_message_is_built_and_sent()
    {
        var refresh = new CountRefresh(2f);

        refresh.ShouldSend("{\"101\":3}", 1);
        refresh.PageReloaded();

        Assert.False(refresh.Holds(1));
        Assert.True(refresh.ShouldSend("{\"101\":3}", 1));
    }
}
