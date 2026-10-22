# Changelog

## 0.6.0

Info Pack now tells you what the game wants from you, and how long your stats and food last:

- Craving: the tooltip of the food you are craving says `You're craving this`.
- Relief requests: an item a relief request still needs says how many more, like `Relief request needs 3 more`, and gets a small gold star on its backpack slot.
- Never tried: food and drink you have never had say `Never tried`.
- Robot modules and trap bait: a robot module's tooltip shows its level, and a bait item shows what kind of bait it is, under Used in.
- Stat bars: hovering a stat bar on the HUD shows how much it drops per game hour and about how long until it is empty. It shows after about half a game hour of play.
- Food forecast: hovering the satiety bar also shows about how many days the food at home lasts you.

Other changes:

- Items a relief request asks for are no longer marked as scrap.
- New Features settings: `Wanted`, `NeverTried`, `StatRates` and `FoodForecast`, all on by default.
- Made for Survival Log 1.1.19131.

## 0.5.0

Item tooltips work like a small wiki now. Below the game's own lines, Info Pack adds one tidy block split into sections, each with a short heading:

- Effects: what the item does to satiety, mood, energy, health and life, green for gains and red for losses.
- Freshness: how long until it spoils, how long it keeps when fresh and what it rots into.
- At home: how many you have at home. Hold Ctrl to see which storages hold them.
- Used in: the workbench recipes, dishes and brewing that use it, and how many you can make now with what you have at home.
- Growing: for seeds, the grow time, harvest window, light need, cold resistance, yield and seed chance.
- Hold Shift for more: value and weight, what it is made from, what it breaks down into, its burn value as fuel, where it can be found and its description.

Other changes:

- New Tooltip settings: when each section shows (always, only while Shift is held, or never), the order of the sections, the two keys, keeping tooltips on screen and spoil badges.
- Tall tooltips no longer run off the bottom of the window.
- Food that spoils within a day gets a small coloured corner on its slot.
- Made for Survival Log 1.1.19131.

## 0.4.1

- After a game update, a hook that can no longer be placed is logged as an error, so the Mod Manager can mark the mod as maybe needing an update.
- Made for Survival Log 1.1.19131.

## 0.4.0

- The Mods screen links this changelog.
- Settings changed on the Mod Manager's Mods screen take effect at once. Turning a feature off takes away what it drew on screen.
- The Mods screen shows the mod's name, description, category and links.
- Exiled's Toolkit 0.4.0 or newer is optional. With it, settings changed on the Mods screen take effect without a restart; without it, at the next game start.

## 0.3.2

The hide looted highlights setting is removed. The game's own grey outline for searched containers works better, so the mod no longer touches container outlines. If your config file still lists HideLootedHighlights, it does nothing and can be deleted.

## 0.3.1

First public release. Shows what the game knows but never puts on screen: how many of an item you have at home, a trader's food stats, brew quality, scrap-only loot, ingredient info while cooking and how long planning cards last. Emptied containers stop staying highlighted.
