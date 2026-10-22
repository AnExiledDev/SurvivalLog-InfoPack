using System.Text.Json;
using InfoPack.Core;
using Xunit;

namespace InfoPack.Tests;

public class HeldKeysTests
{
    private static readonly HeldKeys Nothing = new(false, false);
    private static readonly HeldKeys Detail = new(true, false);
    private static readonly HeldKeys Both = new(true, true);

    [Fact]
    public void The_page_hears_the_keys_as_soon_as_it_is_ready_even_when_nothing_is_held()
    {
        var sync = new KeySync();

        Assert.True(sync.ShouldSend(Nothing));
    }

    [Fact]
    public void Holding_the_same_keys_frame_after_frame_sends_nothing_more()
    {
        var sync = new KeySync();

        sync.ShouldSend(Detail);

        Assert.False(sync.ShouldSend(Detail));
        Assert.False(sync.ShouldSend(Detail));
    }

    [Fact]
    public void Pressing_or_letting_go_of_either_key_tells_the_page()
    {
        var sync = new KeySync();

        sync.ShouldSend(Nothing);

        Assert.True(sync.ShouldSend(Detail));
        Assert.True(sync.ShouldSend(Both));
        Assert.True(sync.ShouldSend(Detail));
        Assert.True(sync.ShouldSend(Nothing));
    }

    [Fact]
    public void A_page_script_installed_afresh_is_told_the_keys_again()
    {
        var sync = new KeySync();

        sync.ShouldSend(Detail);
        sync.PageReloaded();

        Assert.True(sync.ShouldSend(Detail));
    }

    [Fact]
    public void The_page_reads_each_key_under_its_own_name()
    {
        var keys = JsonDocument.Parse(new HeldKeys(false, true).ToJson()).RootElement;

        Assert.False(keys.GetProperty("detail").GetBoolean());
        Assert.True(keys.GetProperty("where").GetBoolean());
    }

    [Theory]
    [InlineData("LeftShift", "Shift")]
    [InlineData("RightShift", "Shift")]
    [InlineData("LeftControl", "Ctrl")]
    [InlineData("RightControl", "Ctrl")]
    [InlineData("LeftAlt", "Alt")]
    [InlineData("RightAlt", "Alt")]
    public void The_hint_names_a_modifier_without_its_side_since_either_side_works(string keyCode, string shown)
    {
        Assert.Equal(shown, KeyNames.Display(keyCode));
    }

    [Theory]
    [InlineData("F")]
    [InlineData("Mouse3")]
    [InlineData("Tab")]
    public void The_hint_names_any_other_key_as_the_settings_do(string keyCode)
    {
        Assert.Equal(keyCode, KeyNames.Display(keyCode));
    }
}
