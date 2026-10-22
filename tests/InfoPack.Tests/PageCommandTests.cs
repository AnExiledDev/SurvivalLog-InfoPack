using System.Text.Json;
using InfoPack.Core;
using Xunit;

namespace InfoPack.Tests;

public class PageCommandTests
{
    [Fact]
    public void The_script_messages_parse_to_their_commands()
    {
        Assert.Equal(new PageCommand.Ready(), PageCommand.Parse("slmod|infopack|ready"));
        Assert.Equal(new PageCommand.NoScript(), PageCommand.Parse("slmod|infopack|noscript"));
    }

    [Theory]
    [InlineData("slmod|infopack|inst|42", 42L)]
    [InlineData("slmod|infopack|inst|9007199254740991", 9007199254740991L)]
    [InlineData("slmod|infopack|inst|-7", -7L)]
    public void An_instance_lookup_carries_the_item_id_the_trade_window_hovered(string message, long itemId)
    {
        Assert.Equal(new PageCommand.Instance(itemId), PageCommand.Parse(message));
    }

    [Theory]
    [InlineData("slmod|infopack|where|15501", 15501)]
    [InlineData("slmod|infopack|where|1", 1)]
    [InlineData("slmod|infopack|where|2147483647", int.MaxValue)]
    public void A_breakdown_request_carries_the_config_id_of_the_hovered_item(string message, int configId)
    {
        Assert.Equal(new PageCommand.Where(configId), PageCommand.Parse(message));
    }

    [Theory]
    [InlineData("slmod|infopack|where")]
    [InlineData("slmod|infopack|where|")]
    [InlineData("slmod|infopack|where|0")]
    [InlineData("slmod|infopack|where|-3")]
    [InlineData("slmod|infopack|where|+3")]
    [InlineData("slmod|infopack|where| 3")]
    [InlineData("slmod|infopack|where|3.0")]
    [InlineData("slmod|infopack|where|1e3")]
    [InlineData("slmod|infopack|where|2147483648")]
    [InlineData("slmod|infopack|where|abc")]
    [InlineData("slmod|infopack|where|3|4")]
    public void A_breakdown_request_without_a_real_item_id_is_never_answered(string message)
    {
        Assert.Equal(new PageCommand.Unknown(message), PageCommand.Parse(message));
    }

    [Theory]
    [InlineData("slmod|infopack|inst")]
    [InlineData("slmod|infopack|inst|")]
    [InlineData("slmod|infopack|inst|12abc")]
    [InlineData("slmod|infopack|inst|1.5")]
    [InlineData("slmod|infopack|inst|1|2")]
    [InlineData("slmod|infopack|ready|now")]
    [InlineData("slmod|infopack|diag|backpack tooltip: TypeError")]
    [InlineData("slmod|infopack|")]
    public void A_malformed_or_unknown_message_is_never_acted_on(string message)
    {
        Assert.Equal(new PageCommand.Unknown(message), PageCommand.Parse(message));
    }

    [Theory]
    [InlineData("slmod|unpackall|ready")]
    [InlineData("slmod|infopac|ready")]
    [InlineData("")]
    [InlineData(null)]
    public void Only_messages_with_our_prefix_are_claimed_so_other_mods_and_the_game_keep_theirs(string? message)
    {
        Assert.False(PageCommand.IsOurs(message));
    }

    public static TheoryData<string> AwkwardTexts() => new()
    {
        "Satiety +5, Mood -2",
        "say \"hi\" \\ bye",
        "line\nbreak\r\tand " + (char)0x2028 + " " + (char)0x2029,
        "</script><script>alert(1)</script>",
        (char)0x98fd + "+{0}",
        "",
    };

    [Theory]
    [MemberData(nameof(AwkwardTexts))]
    public void Any_text_reaches_the_page_unchanged_and_cannot_break_out_of_its_string(string text)
    {
        var literal = JsText.Literal(text);

        Assert.Equal(text, JsonSerializer.Deserialize<string>(literal));
        Assert.DoesNotContain("<", literal);
    }
}
