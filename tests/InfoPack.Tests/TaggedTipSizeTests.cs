using Jint;
using Xunit;

namespace InfoPack.Tests;

/// <summary>
/// The tooltip page.js draws for another mod's tagged row, run in Jint on a fake root page (TaggedFake.js). Exiled's
/// Toolkit can draw that mod's panel at the player's UI size, while the tooltip sits on the page's body outside it, so
/// the tooltip must take the panel's size itself and keep up with a new size while it shows.
/// </summary>
public class TaggedTipSizeTests
{
    [Fact]
    public void A_row_in_a_panel_drawn_at_150_percent_gets_its_tooltip_at_150_percent()
    {
        var page = new Page();
        page.Run("var p = panel(); sized(p, 1.5); var row = tagged('Fill all', p); hover(row, 100, 100); wait(600)");

        Assert.Equal(("block", "Fill all", "1.5"), (page.Text("ownTip().style.display"), page.Text("ownTip().children[0].textContent"), page.Text("ownTip().style.zoom")));
    }

    /// <summary>
    /// The tooltip's place is worked out on screen, 15 px from the cursor at the game's size and that times the zoom
    /// here, then written in the tooltip's own px: 100 + 30 on screen is 65 at zoom 2.
    /// </summary>
    [Fact]
    public void A_zoomed_tooltip_keeps_its_gap_from_the_cursor_on_screen()
    {
        var page = new Page();
        page.Run("var p = panel(); sized(p, 2); var row = tagged('Fill all', p); hover(row, 100, 100); wait(600)");

        Assert.Equal(("65px", "65px"), (page.Text("ownTip().style.left"), page.Text("ownTip().style.top")));
    }

    [Fact]
    public void A_new_size_while_the_tooltip_shows_is_taken_at_the_next_poll_and_placed_again()
    {
        var page = new Page();
        page.Run("var p = panel(); sized(p, 1.5); var row = tagged('Fill all', p); hover(row, 100, 100); wait(600)");

        page.Run("sized(p, 2); poll()");

        Assert.Equal(("2", "65px"), (page.Text("ownTip().style.zoom"), page.Text("ownTip().style.left")));
    }

    /// <summary>The game zooms some pages itself; the tooltip on that page's body has that zoom already.</summary>
    [Fact]
    public void A_row_outside_any_sized_panel_keeps_the_game_size_even_on_a_page_the_game_zoomed()
    {
        var page = new Page();
        page.Run("document.body.style.zoom = '0.8'; var row = tagged('Fill all'); hover(row, 100, 100); wait(600)");

        Assert.Equal(("block", ""), (page.Text("ownTip().style.display"), page.Text("ownTip().style.zoom")));
    }

    [Fact]
    public void Moving_from_a_sized_row_to_a_loose_one_draws_the_tooltip_at_the_game_size_again()
    {
        var page = new Page();
        page.Run("var p = panel(); sized(p, 2); var first = tagged('Fill all', p); var loose = tagged('Clear'); hover(first, 100, 100); wait(600)");

        page.Run("hover(loose, 100, 100); wait(600)");

        Assert.Equal(("Clear", ""), (page.Text("ownTip().children[0].textContent"), page.Text("ownTip().style.zoom")));
    }

    [Theory]
    [InlineData("delete window.ExiledUi")]
    [InlineData("window.ExiledUi = { version: 1, size: function () {} }")]
    public void Without_the_Toolkit_or_with_one_older_than_the_ui_size_the_tooltip_keeps_the_game_size(string toolkit)
    {
        var page = new Page();
        page.Run(toolkit + "; var p = panel(); sized(p, 2); var row = tagged('Fill all', p); hover(row, 100, 100); wait(600)");

        Assert.Equal(("block", ""), (page.Text("ownTip().style.display"), page.Text("ownTip().style.zoom")));
    }

    /// <summary>page.js on TaggedFake.js, with the tooltips kept on screen and the first poll run.</summary>
    private sealed class Page
    {
        private readonly Engine _engine = new();

        public Page()
        {
            _engine.Execute(Resource("InfoPack.Tests.TaggedFake.js"));
            _engine.Execute(Resource("InfoPack.page.js"));
            _engine.Execute("window.__slinfopack.setFeatures({ onScreen: true }); poll()");
        }

        public void Run(string statements) => _engine.Execute(statements);

        public string Text(string expression) => _engine.Evaluate($"String({expression})").AsString();

        private static string Resource(string name)
        {
            using var stream = typeof(TaggedTipSizeTests).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException(name + " is not embedded in the test assembly");
            using var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }
    }
}
