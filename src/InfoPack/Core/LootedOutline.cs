namespace InfoPack.Core;

/// <summary>
/// The search colour the game gives a container's outline on an explore map while Alt is held or the container is
/// hovered, numbered as the game numbers it (ExploreSearchStateManager.ExploreSearchState, which
/// AgentModel.Furniture_ShowSearchState turns into outline bits). Empty means searched with no items left in it.
/// </summary>
public enum SearchState
{
    None = 0,
    Unsearched = 1,
    HasItems = 2,
    Empty = 3,
}

public static class LootedOutline
{
    /// <summary>
    /// The state the game is asked to draw. Only a container searched down to nothing is shown as None, which the game
    /// draws with no search colour. Unsearched ones, ones with items left and any state the game might add later pass
    /// through unchanged and keep the game's colour, so nothing the player still needs stops standing out.
    /// </summary>
    public static int ShownState(int gameState) =>
        gameState == (int)SearchState.Empty ? (int)SearchState.None : gameState;
}
