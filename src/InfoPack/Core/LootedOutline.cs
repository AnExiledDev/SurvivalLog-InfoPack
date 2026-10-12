namespace InfoPack.Core;

/// <summary>
/// The search colour the game gives a container's outline on an explore map while Alt is held or the container is
/// hovered, numbered as the game numbers it (ExploreSearchStateManager.ExploreSearchState, which
/// ExploreSearchStateManager.TryGetState reports and AgentModel.Furniture_ShowSearchState turns into outline bits).
/// Empty means searched with no items left in it.
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
    /// Only a container searched down to nothing loses its search colour. Unsearched ones, ones with items left, None
    /// and any state the game might add later are left to the game, so nothing the player still needs stops standing out.
    /// </summary>
    public static bool ShowsSearchColour(int gameState) => gameState != (int)SearchState.Empty;
}
