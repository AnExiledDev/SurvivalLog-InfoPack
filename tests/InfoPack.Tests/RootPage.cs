using Jint;

namespace InfoPack.Tests;

/// <summary>
/// page.js and Exiled's Toolkit's api.js, as the plugins ship them, on the fake root page in TaggedFake.js. The game
/// can run the two scripts in either order, so a test picks which goes first.
/// </summary>
internal sealed class RootPage
{
    private readonly Engine _engine = new();

    public RootPage(bool isToolkitFirst = false)
    {
        _engine.Execute(Resource("InfoPack.Tests.TaggedFake.js"));

        if (isToolkitFirst) LoadToolkit();

        _engine.Execute(Resource("InfoPack.page.js"));
    }

    /// <summary>page.js with the Toolkit after it, the tooltips kept on screen and the first poll run.</summary>
    public static RootPage WithTooltipsOnScreen()
    {
        var page = new RootPage();

        page.LoadToolkit();
        page.Run("window.__slinfopack.setFeatures({ onScreen: true }); poll()");

        return page;
    }

    public void LoadToolkit() => _engine.Execute(Resource("ExiledToolkit.api.js"));

    public void Run(string statements) => _engine.Execute(statements);

    public string Text(string expression) => _engine.Evaluate($"String({expression})").AsString();

    private static string Resource(string name)
    {
        using var stream = typeof(RootPage).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException(name + " is not embedded in the test assembly");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
