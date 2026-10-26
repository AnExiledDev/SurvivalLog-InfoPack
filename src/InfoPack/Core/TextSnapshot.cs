using System.Collections.Generic;

namespace InfoPack.Core;

/// <summary>
/// The game text the facts use, copied once so the facts can be built away from the game's thread: the description
/// and name of every item, the name of every furniture kind that holds loot and of every place furniture is set down
/// at, the text of every bait kind, and whether the game shows Chinese. Filled a step at a time, one lookup each, so
/// a <see cref="SlicedJob"/> can spread the lookups over frames; a fill cut short carries on where it stopped. Text the
/// copy does not hold reads as missing, as the game's would.
/// </summary>
public sealed class TextSnapshot : IGameText
{
    private readonly Dictionary<int, string?> _descriptions = new();
    private readonly Dictionary<int, string?> _itemNames = new();
    private readonly Dictionary<int, string?> _furnitureNames = new();
    private readonly Dictionary<int, string?> _placeNames = new();
    private readonly Dictionary<string, string?> _localTexts = new();
    private bool _isChinese;

    /// <summary>True once every lookup the tables need has been copied.</summary>
    public bool IsFilled { get; private set; }

    /// <summary>The lookups the facts make from these tables, a step each, asking source for what is not copied yet.</summary>
    public IEnumerable<bool> Fill(GameTables tables, IGameText source)
    {
        foreach (var item in tables.Items)
        {
            if (item.Id <= 0 || _descriptions.ContainsKey(item.Id)) continue;

            _itemNames[item.Id] = source.ItemName(item.Id);
            _descriptions[item.Id] = source.Description(item.Id);

            yield return true;
        }

        foreach (var furniture in tables.LootFurniture)
        {
            if (_furnitureNames.ContainsKey(furniture.Id)) continue;

            _furnitureNames[furniture.Id] = source.FurnitureName(furniture.Id);

            yield return true;
        }

        foreach (var placement in tables.Placements)
        {
            if (placement.PlaceId <= 0 || _placeNames.ContainsKey(placement.PlaceId)) continue;

            _placeNames[placement.PlaceId] = source.PlaceName(placement.PlaceId);

            yield return true;
        }

        foreach (var key in tables.TrapBait.Values)
        {
            if (_localTexts.ContainsKey(key)) continue;

            _localTexts[key] = source.LocalText(key);

            yield return true;
        }

        _isChinese = source.IsChinese();
        IsFilled = true;
    }

    public string? Description(int itemId) => _descriptions.TryGetValue(itemId, out var text) ? text : null;

    public string? ItemName(int itemId) => _itemNames.TryGetValue(itemId, out var text) ? text : null;

    public string? FurnitureName(int furnitureId) => _furnitureNames.TryGetValue(furnitureId, out var text) ? text : null;

    public string? PlaceName(int placeId) => _placeNames.TryGetValue(placeId, out var text) ? text : null;

    public string? LocalText(string key) => _localTexts.TryGetValue(key, out var text) ? text : null;

    public bool IsChinese() => _isChinese;
}

/// <summary>
/// The text copy kept between builds. It is kept only while the key and the item table stay the same (a settings change
/// builds the facts again from the same text); a new world, language or player copies the game's text afresh, as the
/// build read the game's text afresh before.
/// </summary>
public sealed class TextCopies
{
    private TextSnapshot? _copy;
    private FactsKey _key;
    private IReadOnlyList<ItemRow>? _items;

    public TextSnapshot For(FactsKey key, GameTables tables)
    {
        if (_copy == null || _key != key || !ReferenceEquals(_items, tables.Items))
        {
            _copy = new TextSnapshot();
            _key = key;
            _items = tables.Items;
        }

        return _copy;
    }
}
