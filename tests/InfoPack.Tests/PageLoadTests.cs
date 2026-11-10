using Xunit;

namespace InfoPack.Tests;

/// <summary>
/// page.js talks to the plugin and adds its stylesheet through Exiled's Toolkit's page API. The game can run api.js
/// before or after page.js, and the plugin sends nothing until it hears "ready", so the page has to say it exactly once
/// in either order.
/// </summary>
public class PageLoadTests
{
    [Fact]
    public void The_page_says_ready_once_when_the_Toolkit_loads_after_it()
    {
        var page = new RootPage();
        var before = page.Text("sent.length");

        page.LoadToolkit();

        Assert.Equal(("0", "1"), (before, page.Text("readyCount()")));
    }

    [Fact]
    public void The_page_says_ready_once_when_the_Toolkit_loaded_first()
    {
        var page = new RootPage(isToolkitFirst: true);

        Assert.Equal("1", page.Text("readyCount()"));
    }

    /// <summary>The style goes in with the first tooltip drawn and is not added again on the next draws.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_drawn_tooltip_puts_the_style_in_the_page_once(bool isToolkitFirst)
    {
        var page = new RootPage(isToolkitFirst);

        if (!isToolkitFirst) page.LoadToolkit();

        page.Run("window.__slinfopack.setFeatures({ onScreen: true }); poll(); var row = tagged('Fill all'); hover(row, 100, 100); wait(600); poll(); wait(600)");

        Assert.Equal(("1", "true"), (page.Text("slipStyles().length"), page.Text("slipStyles()[0].textContent.length > 0")));
    }
}
