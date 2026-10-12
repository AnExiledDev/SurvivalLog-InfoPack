# Info Pack

A BepInEx 6 (IL2CPP) plugin for Survival Log that shows information the game knows but never puts on screen: how many of an item you have at home, what a trader's food does to you, which brew tier an alcohol is, which items are only worth scrapping, and what an ingredient or dish does while you cook. It only shows things. It never changes an item, a stat or a save.

## What it does

Each piece has its own toggle (see Config) and draws inside the game's own windows, in the game's own tooltip style.

- **Base counts.** Item tooltips in the backpack, the trade window and the cooking window get a line `At home: N`: how many of that item are in your bag, in every storage on your home map and its home group (hidden ones included), and in the workbench drawer, added together: the same pool the game's crafting counts from. The number is refreshed every two seconds while a game is loaded.
- **Trade stats.** In the trade window, the tooltip of an item on the trader's shelf gets a line naming what it does to satiety, mood, energy, health and life, in the game's own wording (for example `Satiety +12, Energy +3`). Items that change no stat get no line.
- **Alcohol quality.** The tooltip of a brew product says where it sits among what its barrel makes, as `Brew quality: 2 of 3` (1 is the lowest). While a brew is set up but not started, the brew panel shows the batch's quality score under its quality bar, with the score each better tier starts at: `Quality 0.62: Normal from 0.40, Strong from 0.80`, with the tier names in the game's language.
- **Scrap only.** Items that are only worth breaking down get a line `Only useful as scrap` in their tooltips, a small `Scrap` chip on their backpack slots, and a `Scrap` tag in the pickup list. An item is marked only when the game can shred it, no workbench or cooking recipe uses it, it is a material, electrical part, daily good or uncategorised item, it has no other use (not food, not usable, not fuel, not brewable, not a seed, trap or furniture), and it is worth no more in trade than what shredding it gives back. The rule leans towards leaving an item unmarked.
- **Cooking codex.** In the cooking window, hovering an ingredient adds its description and its stat line to the game's tooltip, followed by the lines above. Hovering a recipe adds the same for the dish it makes (its normal result, or the next result the recipe names when it has no normal one).

The pages keep their additions across reloads: the script puts itself back whenever the game's web view reloads.

## Controls

None. Everything shows up in tooltips and windows the game already has; there are no buttons and no hotkeys.

## Config

`BepInEx/config/com.dev.survivallog.infopack.cfg`:

| Setting | Default | What it does |
| --- | --- | --- |
| `Features/BaseCounts` | `true` | The `At home: N` line in the backpack, trade and cooking tooltips. |
| `Features/TradeStats` | `true` | The stat line in the trade window's tooltip for the trader's items. |
| `Features/AlcoholQuality` | `true` | The `Brew quality: R of M` tooltip line and the quality readout in the brew panel. |
| `Features/ScrapOnly` | `true` | The `Only useful as scrap` tooltip line, the `Scrap` chip on backpack slots and the `Scrap` tag in the pickup list. |
| `Features/CookingCodex` | `true` | Descriptions and stat lines in the cooking window's ingredient and recipe tooltips. |

With every setting off the plugin logs that it has nothing to do and stays out of the game entirely.

## Log

`BepInEx/info-pack.log`. It gets a start line naming the version and which pieces are on, a line when the facts are built for a loaded world or a new language (item count, size, language and how many milliseconds it took), and one line per distinct failure: a game table that could not be read or stat wording that fell back to English (each reported once), and a page piece that threw (once per page load).

## Known limits

- Ingredient lines under a recipe in the cooking window are the game's own and get no counts.
- The scrap rule does not know about wishes, aid demands, robot modules or trap bait, so an item only those ask for can still be marked. Anything the game burns as fuel is treated as useful and never marked.
- Items on the trader's side that the game's item cache does not hold get no lines in the trade tooltip. Items you sell are looked up by their instance id, which can take a moment the first time.
- The game places its tooltips for a fixed size. A tooltip made taller by these lines can reach past the bottom of the screen near the lower edge.
- The brew panel readout shows the quality while a brew is being set up. Once the barrel is brewing it is hidden.
- Hiding the highlight on searched containers was planned and left out: the game's search-state code could not be followed with enough confidence to hook it safely.
- Text is English apart from the stat wording, the item descriptions and the brew tier names, which come from the game in your language.

## Install

Copy `InfoPack.dll` into `BepInEx/plugins`. Needs BepInEx 6 (IL2CPP build).

## Build and test

```sh
dotnet build src/InfoPack -c Release -p:GameDir="D:\path\to\Survival Log"
dotnet test tests/InfoPack.Tests
```

The build references BepInEx and the game's interop assemblies under `GameDir`, so BepInEx must be installed and the game launched once. The tests compile only the pure `Core` code and need nothing from the game.

## License

MIT, see [LICENSE](LICENSE).
