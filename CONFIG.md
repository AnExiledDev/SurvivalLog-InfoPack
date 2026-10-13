# Info Pack settings

Settings live in `BepInEx\config\com.dev.survivallog.infopack.cfg`, created the first time the game starts with the mod. Edit the file while the game is closed; changes apply at the next game start. Delete the file to get the defaults back.

## Features

| Setting | Default | What it does |
| --- | --- | --- |
| `BaseCounts` | `true` | Item tooltips in the backpack, trade and cooking windows get an `At home: N` line counting that item in your backpack, every storage at home and the workbench drawer. Turn it off if you also use the ItemTotals mod, so tooltips don't show two counts. |
| `TradeStats` | `true` | The tooltip of an item on the trader's shelf in the trade window shows what it does to satiety, mood, energy, health and life. |
| `AlcoholQuality` | `true` | A brewed drink's tooltip shows its tier, like `Brew quality: 2 of 3` (1 is the lowest). While you set up a brew, the brew panel also shows the batch's quality score and the score each better tier starts at. |
| `ScrapOnly` | `true` | Items only worth shredding get an `Only useful as scrap` line in their tooltip, a `Scrap` chip on their backpack slot and a `Scrap` tag in the pickup list. |
| `CookingCodex` | `true` | Hovering an ingredient in the cooking window adds its description and stats to the tooltip. Hovering a recipe does the same for the dish it makes. |
| `PlanningBadges` | `true` | Cards on the Survival Planning screen get a small badge on their picture. A timed card shows how long it lasts, like `3d` or `24h`. A card that takes effect once shows `1x`. Cards that last for good get no badge. |
| `HideLootedHighlights` | `false` | Off by default. When on, containers on explore maps that you have searched and emptied lose their search colour when you hold Alt or point at them, while unsearched containers and ones with items left keep it. |

Each piece works on its own, so turn off any you don't want and the rest keep working.

Set `BaseCounts` to `false` if you keep the ItemTotals mod installed, so tooltips don't show two count lines.

With every setting `false`, the mod writes one line to the log saying it has nothing to do, and stays out of the game.
