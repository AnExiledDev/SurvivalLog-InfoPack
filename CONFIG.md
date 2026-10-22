# Info Pack settings

Settings live in `BepInEx\config\com.dev.survivallog.infopack.cfg`, created the first time the game starts with the mod. Change them in game on the Mod Manager's Mods screen, where they take effect at once, or edit the file while the game is closed. Delete the file to get the defaults back. Without Exiled's Toolkit installed, changes made on the Mods screen take effect at the next game start instead.

## Features

| Setting | Default | What it does |
| --- | --- | --- |
| `BaseCounts` | `true` | Item tooltips in the backpack, trade and cooking windows get an `At home: N` line counting that item in your backpack, every storage at home and the workbench drawer. Turn it off if you also use the ItemTotals mod, so tooltips don't show two counts. |
| `TradeStats` | `true` | The tooltip of an item on the trader's shelf in the trade window shows what it does to satiety, mood, energy, health and life. |
| `AlcoholQuality` | `true` | A brewed drink's tooltip shows its tier, like `Brew quality: 2 of 3` (1 is the lowest). While you set up a brew, the brew panel also shows the batch's quality score and the score each better tier starts at. |
| `ScrapOnly` | `true` | Items only worth shredding get an `Only useful as scrap` line in their tooltip, a `Scrap` chip on their backpack slot and a `Scrap` tag in the pickup list. |
| `CookingCodex` | `true` | Hovering an ingredient in the cooking window adds its description and stats to the tooltip. Hovering a recipe does the same for the dish it makes. |
| `PlanningBadges` | `true` | Cards on the Survival Planning screen get a small badge on their picture. A timed card shows how long it lasts, like `3d` or `24h`. A card that takes effect once shows `1x`. Cards that last for good get no badge. |

## Tooltip

| Setting | Default | What it does |
| --- | --- | --- |
| `DetailKey` | `LeftShift` | Hold this key over an item to see the sections set to `Detail`, and every row of long lists. The tooltip shows a hint like `Shift: more` when there is more to see. Either side of Shift, Ctrl or Alt works. |
| `WhereKey` | `LeftControl` | Hold this key over an item to see which storages at home hold it and how many each one has, under the home count. Needs `BaseCounts`. Either side of Shift, Ctrl or Alt works. |
| `KeepOnScreen` | `true` | A tooltip that would run past an edge of the window, such as a tall one near the bottom, is moved back inside it. |
| `SpoilBadges` | `true` | Food in backpack and storage slots that spoils within a day gets a small coloured corner. |
| `Order` | `Effects, Fresh, Notes, Home, UsedIn, Grow, Value, MadeFrom, Breaks, Fuel, FoundIn, About` | The order of the sections in the Info Pack part of item tooltips, as section names separated by commas. A section left out keeps its usual place after the ones named; an unknown or repeated name is ignored. |

One setting per section says when it shows: `Always`, `Detail` (only while `DetailKey` is held) or `Off`. A section with nothing to say about an item is never drawn.

| Setting | Default | Section |
| --- | --- | --- |
| `Effects` | `Always` | Effects: what the item does to satiety, mood, energy, health and life. |
| `Fresh` | `Always` | Freshness: time left before the item spoils, how long it keeps when fresh and what it rots into. |
| `Notes` | `Always` | Info Pack's short notes, like `Only useful as scrap` and a brewed drink's quality. |
| `Home` | `Always` | At home: how many you have at home, with the per-storage list while `WhereKey` is held. Needs `BaseCounts`. |
| `UsedIn` | `Always` | Used in: workbench recipes, dishes and brewing that use the item. |
| `Grow` | `Always` | Growing, for seeds: grow time, harvest window, light need, cold resistance, yield and seed chance. |
| `Value` | `Detail` | Value: trade value, weight, trade value per kg and satiety per kg. |
| `MadeFrom` | `Detail` | Made from: the recipes, plants and shredding that give the item. |
| `Breaks` | `Detail` | Breaks down into: what shredding, cutting or rotting turns the item into. |
| `Fuel` | `Detail` | Fuel: the item's burn value as fuel. |
| `FoundIn` | `Detail` | Found in: the furniture the item can turn up in while searching, and where that furniture stands. |
| `About` | `Detail` | About: the item's description. |

On the Mod Manager's Mods screen, `Order` and the per-section settings are under the advanced settings.

Each piece works on its own, so turn off any you don't want and the rest keep working.

Set `BaseCounts` to `false` if you keep the ItemTotals mod installed, so tooltips don't show two count lines.

With every feature `false`, every section `Off` and `KeepOnScreen` and `SpoilBadges` `false`, the mod draws nothing and the log says every feature is off.
