using System;
using System.Globalization;

namespace InfoPack.Core;

/// <summary>
/// What the page script tells the plugin. It sends "slmod|infopack|" followed by a verb; anything else parses to
/// Unknown so it can be logged rather than acted on (the script's "diag|..." notes land there on purpose).
/// </summary>
public abstract record PageCommand
{
    public const string Prefix = "slmod|infopack|";

    private PageCommand()
    {
    }

    /// <summary>The page script just installed itself and holds no data yet.</summary>
    public sealed record Ready : PageCommand;

    /// <summary>The page has no script, usually because the web view reloaded.</summary>
    public sealed record NoScript : PageCommand;

    /// <summary>The trade window hovered an item it only knows by instance id; the page wants that item's config id.</summary>
    public sealed record Instance(long ItemId) : PageCommand;

    /// <summary>The breakdown key is held over an item; the page wants to know which home storages hold it.</summary>
    public sealed record Where(int ConfigId) : PageCommand;

    public sealed record Unknown(string Text) : PageCommand;

    public static bool IsOurs(string? message) => message is not null && message.StartsWith(Prefix, StringComparison.Ordinal);

    /// <summary>Parses a whole message, prefix included.</summary>
    public static PageCommand Parse(string message)
    {
        if (!IsOurs(message)) return new Unknown(message);

        var parts = message.Substring(Prefix.Length).Split('|');

        return (parts[0], parts.Length) switch
        {
            ("ready", 1) => new Ready(),
            ("noscript", 1) => new NoScript(),
            ("inst", 2) when IsWholeNumber(parts[1], out var id) => new Instance(id),
            ("where", 2) when IsConfigId(parts[1], out var configId) => new Where(configId),
            _ => new Unknown(message),
        };
    }

    /// <summary>Item config ids are positive, written as plain digits.</summary>
    private static bool IsConfigId(string text, out int value) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value) && value > 0;

    private static bool IsWholeNumber(string text, out long value) =>
        long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
}
