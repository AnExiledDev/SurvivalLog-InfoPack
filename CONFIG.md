# Info Pack settings

Settings live in `BepInEx\config\com.dev.survivallog.infopack.cfg`, created the first time the game starts with the mod. Edit the file while the game is closed; changes apply at the next game start. Delete the file to get the defaults back.

## Features

| Setting | Default | What it does |
| --- | --- | --- |
| `BaseCounts` | `true` | Show how many of an item you have at home (bag, home storages and the workbench drawer) as an `At home: N` line in the backpack, trade and cooking tooltips. |
| `TradeStats` | `true` | Show what an item does to satiety, mood, energy, health and life in the trade window's tooltip for the trader's items. |
| `AlcoholQuality` | `true` | Show which brew tier a drink is in its tooltip (`Brew quality: 2 of 3`), and the brew panel's quality score with the score each better tier starts at. |
| `ScrapOnly` | `true` | Mark items that are only worth breaking down: an `Only useful as scrap` tooltip line, a `Scrap` chip on backpack slots and a `Scrap` tag in the pickup list. |
| `CookingCodex` | `true` | Add the description and stats of an ingredient, or of a recipe's dish, to the cooking window's tooltips. |
| `HideLootedHighlights` | `false` | New, off by default. On explore maps, stop the search colour from marking containers you have already searched and emptied. Unsearched containers and ones with items left keep their colour. |

Each piece works on its own, so turn off any you don't want and the rest keep working.

Set `BaseCounts` to `false` if you keep the ItemTotals mod installed, so tooltips don't show two count lines.

With every setting `false`, the mod writes one line to the log saying it has nothing to do, and stays out of the game.
